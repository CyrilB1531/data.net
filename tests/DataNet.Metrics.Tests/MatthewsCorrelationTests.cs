using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class MatthewsCorrelationTests
{
    [Theory]
    [MemberData(nameof(MetricsCorpus.Indices), MemberType = typeof(MetricsCorpus))]
    public void Matches_sklearn_matthews_corrcoef(int index)
    {
        JsonElement c = MetricsCorpus.Cases[index];

        double actual = MatthewsCorrelation.Score(
            MetricsCorpus.Ints(c, "y_true"),
            MetricsCorpus.Ints(c, "y_pred"),
            sampleWeight: MetricsCorpus.OptionalDoubles(c, "sample_weight"));
        double want = OracleLoader.Number(c.GetProperty("matthews"));

        // Defensive, and unreachable on today's corpus: matthews_corrcoef
        // hard-codes 0.0 for its undefined case rather than returning nan, so no
        // fixture here is non-finite. Kept so this test reads the same as the
        // balanced-accuracy and kappa ones next door, where the branch does fire,
        // and so a future fixture cannot fail on NaN != NaN instead of on value.
        if (double.IsNaN(want))
        {
            Assert.True(double.IsNaN(actual), $"{MetricsCorpus.Describe(c)}: expected NaN, got {actual}");
            return;
        }

        Assert.True(Math.Abs(want - actual) < MetricsCorpus.Tolerance,
            $"{MetricsCorpus.Describe(c)}: expected {want}, got {actual}");
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
        // never produce this number: Score(cm) computes over exactly the classes
        // the matrix holds. That is the opposite of what Precision and Recall do
        // with the same matrix — those read ConfusionMatrix.TrueSum and the
        // extended column sums on purpose, so a sample whose predicted label fell
        // outside the label set still counts in a denominator there (Prf.Support's
        // own remarks say why). BalancedAccuracy and CohenKappa follow this rule,
        // not that one. This is the same 7-sample, 3-class fixture the two tests
        // above use, whose
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

    [Fact]
    public void A_view_holding_no_weight_is_undefined_the_same_way_kappa_is()
    {
        // The input CohenKappaTests.A_view_holding_no_weight_* pins: labels=[0]
        // keeps one class and every sample was predicted as 1, so the view holds
        // no weight. Matthews reaches its denominator == 0.0 guard on its own
        // arithmetic, and always did — this is here so the two metrics are pinned
        // to agree that the input is undefined rather than one of them quietly
        // returning a number.
        int[] yTrue = [0, 0];
        int[] yPred = [1, 1];

        Assert.Equal(0.0, MatthewsCorrelation.Score(yTrue, yPred, labels: [0]));
        Assert.True(double.IsNaN(MatthewsCorrelation.Score(yTrue, yPred, ZeroDivision.NaN, labels: [0])));
        Assert.Throws<UndefinedMetricException>(
            () => MatthewsCorrelation.Score(yTrue, yPred, ZeroDivision.Throw, labels: [0]));
    }
}
