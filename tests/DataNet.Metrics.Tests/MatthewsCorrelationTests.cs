using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class MatthewsCorrelationTests
{
    [Fact]
    public void Matches_sklearn_matthews_corrcoef()
    {
        foreach (JsonElement c in MetricsCorpus.Cases)
        {
            if (!c.TryGetProperty("matthews", out JsonElement expected))
            {
                continue;
            }

            double actual = MatthewsCorrelation.Score(
                MetricsCorpus.Ints(c, "y_true"),
                MetricsCorpus.Ints(c, "y_pred"),
                sampleWeight: MetricsCorpus.OptionalDoubles(c, "sample_weight"));
            double want = OracleLoader.Number(expected);

            if (double.IsNaN(want))
            {
                Assert.True(double.IsNaN(actual), $"{MetricsCorpus.Describe(c)}: expected NaN, got {actual}");
                continue;
            }

            Assert.True(Math.Abs(want - actual) < MetricsCorpus.Tolerance,
                $"{MetricsCorpus.Describe(c)}: expected {want}, got {actual}");
        }
    }

    [Fact]
    public void A_single_label_throughout_is_zero_by_default()
    {
        // The denominator collapses. scikit-learn hard-codes 0.0 and warns; this
        // package makes the choice explicit instead, defaulting to the same value.
        int[] y = [1, 1, 1];

        Assert.Equal(0.0, MatthewsCorrelation.Score(y, y));
    }

    [Fact]
    public void A_single_label_throughout_can_be_made_to_throw()
    {
        int[] y = [1, 1, 1];

        Assert.Throws<UndefinedMetricException>(
            () => MatthewsCorrelation.Score(y, y, ZeroDivision.Throw));
    }

    [Fact]
    public void Reads_the_same_number_from_the_labels_and_from_the_matrix()
    {
        int[] yTrue = [0, 0, 1, 1, 2, 2, 2];
        int[] yPred = [0, 1, 1, 1, 2, 0, 2];

        double fromLabels = MatthewsCorrelation.Score(yTrue, yPred);
        double fromMatrix = MatthewsCorrelation.Score(ConfusionMatrix.Compute(yTrue, yPred));

        Assert.Equal(fromMatrix, fromLabels);
    }

    [Fact]
    public void Is_a_correlation_so_perfect_agreement_is_one()
    {
        int[] yTrue = [0, 1, 2, 0, 1, 2];

        Assert.Equal(1.0, MatthewsCorrelation.Score(yTrue, yTrue), 12);
    }

    [Fact]
    public void A_restricted_label_set_reads_over_the_matrix_it_holds()
    {
        // scikit-learn's matthews_corrcoef takes no labels argument, so it can
        // never produce this number: Score(cm) computes over exactly the
        // classes the matrix holds, the same as Precision and Recall do. This
        // is the same 7-sample, 3-class fixture the two tests above use, whose
        // unrestricted correlation over all three classes is 0.59375.
        // Restricting to labels [1, 2] drops every sample that touches label
        // 0 (rows 0-1 of the pair below), and what is left of the matrix is
        // diagonal - perfect agreement, so the restricted correlation is 1.0,
        // not 0.59375.
        int[] yTrue = [0, 0, 1, 1, 2, 2, 2];
        int[] yPred = [0, 1, 1, 1, 2, 0, 2];

        ConfusionMatrix restricted = ConfusionMatrix.Compute(yTrue, yPred, labels: [1, 2]);

        Assert.Equal(1.0, MatthewsCorrelation.Score(restricted), 12);
    }
}
