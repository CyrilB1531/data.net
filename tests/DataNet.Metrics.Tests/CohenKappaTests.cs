using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class CohenKappaTests
{
    private static readonly int[] YTrue = [0, 0, 1, 1, 2, 2, 2];
    private static readonly int[] YPred = [0, 1, 1, 1, 2, 0, 2];

    /// <summary>One row per corpus case per weighting, so a failure names both.</summary>
    public static TheoryData<int, string, KappaWeighting> Rows()
    {
        var data = new TheoryData<int, string, KappaWeighting>();
        for (int i = 0; i < MetricsCorpus.Cases.Count; i++)
        {
            data.Add(i, "kappa", KappaWeighting.None);
            data.Add(i, "kappa_linear", KappaWeighting.Linear);
            data.Add(i, "kappa_quadratic", KappaWeighting.Quadratic);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Rows))]
    public void Matches_sklearn_cohen_kappa_score(int index, string key, KappaWeighting weighting)
    {
        JsonElement c = MetricsCorpus.Cases[index];

        double actual = CohenKappa.Score(
            MetricsCorpus.Ints(c, "y_true"),
            MetricsCorpus.Ints(c, "y_pred"),
            weighting,
            sampleWeight: MetricsCorpus.OptionalDoubles(c, "sample_weight"));
        double want = OracleLoader.Number(c.GetProperty(key));

        // Four corpus fixtures really are nan here — cohen_kappa_score's own
        // replace_undefined_by default — and NaN is never within Tolerance of
        // itself, so the comparison has to branch.
        if (double.IsNaN(want))
        {
            Assert.True(double.IsNaN(actual), $"{MetricsCorpus.Describe(c)} {key}: expected NaN, got {actual}");
            return;
        }

        Assert.True(Math.Abs(want - actual) < MetricsCorpus.Tolerance,
            $"{MetricsCorpus.Describe(c)} {key}: expected {want}, got {actual}");
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
        // Score(cm) reads over exactly the classes the matrix holds, whatever
        // scikit-learn's own labels= would do with the same inputs - a property
        // of this package's matrix-consuming overload, not a gap in the
        // reference. This is the same 7-sample, 3-class fixture
        // Matches_sklearn_cohen_kappa_score's "kappa" row covers, whose
        // unrestricted kappa over all three classes is 0.575757575758.
        // Restricting to labels [1, 2] drops every sample that touches label 0
        // (indices 0, 1 and 5 below), and what is left of the matrix is
        // diagonal - perfect agreement, so the restricted kappa is 1.0, not
        // 0.575757575758 - which happens to be exactly what
        // cohen_kappa_score(y_true, y_pred, labels=[1, 2]) also returns.
        ConfusionMatrix restricted = ConfusionMatrix.Compute(YTrue, YPred, labels: [1, 2]);

        Assert.Equal(1.0, CohenKappa.Score(restricted), 12);
    }

    [Theory]
    [InlineData(ZeroDivision.Zero, 0.0)]
    [InlineData(ZeroDivision.One, 1.0)]
    [InlineData(ZeroDivision.NaN, double.NaN)]
    public void A_view_holding_no_weight_reads_the_zero_division_policy(ZeroDivision zeroDivision, double want)
    {
        // labels=[0] keeps one class, and every sample was predicted as 1, so the
        // 1x1 view holds 0.0 — nothing to correct for chance. The expected
        // agreement is then 0.0 / 0.0 cell by cell, which is NaN, which is never
        // == 0.0: without a guard on the total the expected == 0.0 test never
        // fires and the method returns NaN for every policy, silently discarding
        // this argument. scikit-learn tests the same sum before building its
        // expected matrix and returns replace_undefined_by.
        int[] yTrue = [0, 0];
        int[] yPred = [1, 1];

        double actual = CohenKappa.Score(yTrue, yPred, zeroDivision: zeroDivision, labels: [0]);

        if (double.IsNaN(want))
        {
            Assert.True(double.IsNaN(actual), $"expected NaN, got {actual}");
        }
        else
        {
            Assert.Equal(want, actual);
        }
    }

    [Fact]
    public void A_view_holding_no_weight_can_be_made_to_throw()
    {
        int[] yTrue = [0, 0];
        int[] yPred = [1, 1];

        Assert.Throws<UndefinedMetricException>(
            () => CohenKappa.Score(yTrue, yPred, zeroDivision: ZeroDivision.Throw, labels: [0]));
    }

    [Fact]
    public void An_all_zero_sample_weight_reaches_the_same_guard()
    {
        // The other way to a view with no weight, and the one that needs no
        // labels= at all: a perfectly ordinary two-class target whose every
        // weight is zero. Kappa is as undefined here as above.
        int[] yTrue = [0, 1];
        int[] yPred = [0, 1];
        double[] weights = [0.0, 0.0];

        Assert.Equal(0.0, CohenKappa.Score(yTrue, yPred, zeroDivision: ZeroDivision.Zero, sampleWeight: weights));
        Assert.True(double.IsNaN(CohenKappa.Score(yTrue, yPred, sampleWeight: weights)));
        Assert.Throws<UndefinedMetricException>(
            () => CohenKappa.Score(yTrue, yPred, zeroDivision: ZeroDivision.Throw, sampleWeight: weights));
    }

    [Fact]
    public void An_out_of_range_weighting_is_refused_even_on_an_undefined_input()
    {
        // The weighting is validated before the undefined-total shortcut, so the
        // argument error wins over the policy — scikit-learn validates its
        // parameters before it looks at the data too.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CohenKappa.Score([0, 0], [1, 1], (KappaWeighting)99, labels: [0]));
    }
}
