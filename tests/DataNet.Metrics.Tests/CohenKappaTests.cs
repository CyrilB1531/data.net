using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class CohenKappaTests
{
    private static readonly int[] YTrue = [0, 0, 1, 1, 2, 2, 2];
    private static readonly int[] YPred = [0, 1, 1, 1, 2, 0, 2];

    [Theory]
    [InlineData("kappa", KappaWeighting.None)]
    [InlineData("kappa_linear", KappaWeighting.Linear)]
    [InlineData("kappa_quadratic", KappaWeighting.Quadratic)]
    public void Matches_sklearn_cohen_kappa_score(string key, KappaWeighting weighting)
    {
        foreach (JsonElement c in MetricsCorpus.Cases)
        {
            if (!c.TryGetProperty(key, out JsonElement expected))
            {
                continue;
            }

            double actual = CohenKappa.Score(
                MetricsCorpus.Ints(c, "y_true"),
                MetricsCorpus.Ints(c, "y_pred"),
                weighting,
                sampleWeight: MetricsCorpus.OptionalDoubles(c, "sample_weight"));
            double want = OracleLoader.Number(expected);

            if (double.IsNaN(want))
            {
                Assert.True(double.IsNaN(actual), $"{MetricsCorpus.Describe(c)} {key}: expected NaN, got {actual}");
                continue;
            }

            Assert.True(Math.Abs(want - actual) < MetricsCorpus.Tolerance,
                $"{MetricsCorpus.Describe(c)} {key}: expected {want}, got {actual}");
        }
    }

    [Fact]
    public void Unweighted_kappa_is_invariant_under_any_permutation_of_the_labels()
    {
        double reference = CohenKappa.Score(YTrue, YPred, labels: [0, 1, 2]);

        foreach (int[] order in new[] { new[] { 2, 1, 0 }, new[] { 1, 0, 2 }, new[] { 2, 0, 1 } })
        {
            Assert.Equal(reference, CohenKappa.Score(YTrue, YPred, labels: order), 12);
        }
    }

    [Theory]
    [InlineData(KappaWeighting.Linear)]
    [InlineData(KappaWeighting.Quadratic)]
    public void Weighted_kappa_survives_a_reversal_and_not_another_permutation(KappaWeighting weighting)
    {
        // The weighting is a distance between class indices, so reversing them
        // preserves every abs(i - j) and any other permutation does not. This is
        // why the matrix overload's result depends on cm.Labels order, and the
        // only test that would notice if the weighting were built from label
        // values instead of positions.
        double ascending = CohenKappa.Score(YTrue, YPred, weighting, labels: [0, 1, 2]);
        double reversed = CohenKappa.Score(YTrue, YPred, weighting, labels: [2, 1, 0]);
        double shuffled = CohenKappa.Score(YTrue, YPred, weighting, labels: [1, 0, 2]);

        Assert.Equal(ascending, reversed, 12);
        Assert.NotEqual(ascending, shuffled, 12);
    }

    [Fact]
    public void A_single_label_throughout_is_NaN_by_default()
    {
        // scikit-learn's own default for this case, and the reason ZeroDivision
        // defaults to NaN here while it defaults to Zero on Matthews correlation.
        int[] y = [1, 1, 1];

        Assert.True(double.IsNaN(CohenKappa.Score(y, y)));
    }

    [Fact]
    public void A_single_label_throughout_can_be_made_to_throw()
    {
        int[] y = [1, 1, 1];

        Assert.Throws<UndefinedMetricException>(
            () => CohenKappa.Score(y, y, zeroDivision: ZeroDivision.Throw));
    }

    [Fact]
    public void Perfect_agreement_is_one()
    {
        int[] y = [1, 1, 2];

        Assert.Equal(1.0, CohenKappa.Score(y, y), 12);
    }

    [Fact]
    public void An_out_of_range_weighting_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CohenKappa.Score(YTrue, YPred, (KappaWeighting)99));
    }

    [Fact]
    public void A_restricted_label_set_reads_over_the_matrix_it_holds()
    {
        // scikit-learn's cohen_kappa_score takes no labels argument, so it can
        // never produce this number: Score(cm) computes over exactly the
        // classes the matrix holds. This is the same 7-sample, 3-class fixture
        // Matches_sklearn_cohen_kappa_score's "kappa" row covers, whose
        // unrestricted kappa over all three classes is 0.575757575758.
        // Restricting to labels [1, 2] drops every sample that touches label 0
        // (indices 0, 1 and 5 below), and what is left of the matrix is
        // diagonal - perfect agreement, so the restricted kappa is 1.0, not
        // 0.575757575758.
        ConfusionMatrix restricted = ConfusionMatrix.Compute(YTrue, YPred, labels: [1, 2]);

        Assert.Equal(1.0, CohenKappa.Score(restricted), 12);
    }
}
