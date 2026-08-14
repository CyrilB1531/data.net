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

        // Defensive, and unreachable today: matthews_corrcoef hard-codes 0.0 for
        // its undefined case, so a future fixture cannot fail on NaN != NaN.
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

    /// <summary>
    /// scikit-learn's <c>matthews_corrcoef</c> takes no <c>labels</c> argument, so
    /// <c>Score(cm)</c> computes over exactly the classes the matrix holds —
    /// following <see cref="BalancedAccuracy"/> and <see cref="CohenKappa"/>, not
    /// <see cref="Precision"/>/<see cref="Recall"/>, which read the extended
    /// column sums on purpose (<see cref="Prf"/>'s own remarks say why). The same
    /// 7-sample, 3-class fixture the two tests above use scores 0.59375
    /// unrestricted; restricting to labels [1, 2] drops every sample touching
    /// label 0, leaving a diagonal matrix, so the restricted correlation is 1.0.
    /// </summary>
    [Fact]
    public void A_restricted_label_set_reads_over_the_matrix_it_holds()
    {
        int[] yTrue = [0, 0, 1, 1, 2, 2, 2];
        int[] yPred = [0, 1, 1, 1, 2, 0, 2];

        ConfusionMatrix restricted = ConfusionMatrix.Compute(yTrue, yPred, labels: [1, 2]);

        Assert.Equal(1.0, MatthewsCorrelation.Score(restricted), 12);
    }

    [Fact]
    public void A_view_holding_no_weight_is_undefined_the_same_way_kappa_is()
    {
        // Same input CohenKappaTests's A_view_holding_no_weight_* fixtures pin:
        // labels=[0] leaves no weight, pinning the two metrics to agree.
        int[] yTrue = [0, 0];
        int[] yPred = [1, 1];

        Assert.Equal(0.0, MatthewsCorrelation.Score(yTrue, yPred, labels: [0]));
        Assert.True(double.IsNaN(MatthewsCorrelation.Score(yTrue, yPred, ZeroDivision.NaN, labels: [0])));
        Assert.Throws<UndefinedMetricException>(
            () => MatthewsCorrelation.Score(yTrue, yPred, ZeroDivision.Throw, labels: [0]));
    }
}
