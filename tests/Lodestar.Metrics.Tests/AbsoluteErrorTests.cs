using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

public sealed class AbsoluteErrorTests
{
    [Theory]
    [MemberData(nameof(RegressionCorpus.Indices), MemberType = typeof(RegressionCorpus))]
    public void Matches_sklearn_mean_absolute_error(int index)
    {
        JsonElement c = RegressionCorpus.Cases[index];
        double[] yTrue = RegressionCorpus.Doubles(c, "y_true");
        double[] yPred = RegressionCorpus.Doubles(c, "y_pred");
        int k = RegressionCorpus.OutputCount(c);
        double[] sw = RegressionCorpus.OptionalDoubles(c, "sample_weight");
        string who = RegressionCorpus.Describe(c);

        RegressionCorpus.AssertClose(
            RegressionCorpus.Value(c, "mae|uniform"),
            MeanAbsoluteError.Score(yTrue, yPred, k, sw), $"{who} mae|uniform");
        RegressionCorpus.AssertClose(
            RegressionCorpus.Values(c, "mae|raw"),
            MeanAbsoluteError.PerOutput(yTrue, yPred, k, sw), $"{who} mae|raw");

        if (RegressionCorpus.Has(c, "mae|weights"))
        {
            RegressionCorpus.AssertClose(
                RegressionCorpus.Value(c, "mae|weights"),
                MeanAbsoluteError.Score(yTrue, yPred, k, sw, RegressionCorpus.OutputWeights(c)),
                $"{who} mae|weights");
        }
    }

    [Theory]
    [MemberData(nameof(RegressionCorpus.Indices), MemberType = typeof(RegressionCorpus))]
    public void Matches_sklearn_median_absolute_error(int index)
    {
        JsonElement c = RegressionCorpus.Cases[index];
        double[] yTrue = RegressionCorpus.Doubles(c, "y_true");
        double[] yPred = RegressionCorpus.Doubles(c, "y_pred");
        int k = RegressionCorpus.OutputCount(c);
        double[] sw = RegressionCorpus.OptionalDoubles(c, "sample_weight");
        string who = RegressionCorpus.Describe(c);

        RegressionCorpus.AssertClose(
            RegressionCorpus.Value(c, "median_ae|uniform"),
            MedianAbsoluteError.Score(yTrue, yPred, k, sw), $"{who} median_ae|uniform");
        RegressionCorpus.AssertClose(
            RegressionCorpus.Values(c, "median_ae|raw"),
            MedianAbsoluteError.PerOutput(yTrue, yPred, k, sw), $"{who} median_ae|raw");

        if (RegressionCorpus.Has(c, "median_ae|weights"))
        {
            RegressionCorpus.AssertClose(
                RegressionCorpus.Value(c, "median_ae|weights"),
                MedianAbsoluteError.Score(yTrue, yPred, k, sw, RegressionCorpus.OutputWeights(c)),
                $"{who} median_ae|weights");
        }
    }

    [Fact]
    public void Reproduces_the_hand_measured_values()
    {
        double[] yTrue = [3.0, -0.5, 2.0, 7.0];
        double[] yPred = [2.5, 0.0, 2.0, 8.0];

        Assert.Equal(0.5, MeanAbsoluteError.Score(yTrue, yPred), 12);
        Assert.Equal(0.55, MeanAbsoluteError.Score(yTrue, yPred, 1, [1.0, 2.0, 3.0, 4.0]), 12);
        Assert.Equal(0.5, MedianAbsoluteError.Score(yTrue, yPred), 12);
        Assert.Equal(0.5, MedianAbsoluteError.Score(yTrue, yPred, 1, [1.0, 2.0, 3.0, 4.0]), 12);

        double[] mtTrue = [0.5, 1.0, -1.0, 1.0, 7.0, -6.0];
        double[] mtPred = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];
        Assert.Equal([0.5, 1.0], MeanAbsoluteError.PerOutput(mtTrue, mtPred, 2));
    }

    [Theory]
    // Residuals [0, 2, 4, 10], one row per measured weighting: the uniform rows
    // are what a naive "value at the 50% point" median gets wrong, since the count is even.
    [InlineData(new double[] { 1, 1, 1, 1 }, 3.0)]
    [InlineData(new double[] { 2, 2, 2, 2 }, 3.0)]
    [InlineData(new double[] { 0.5, 0.5, 0.5, 0.5 }, 3.0)]
    [InlineData(new double[] { 1, 1, 1, 2 }, 4.0)]
    [InlineData(new double[] { 2, 1, 1, 1 }, 2.0)]
    [InlineData(new double[] { 1, 2, 1, 1 }, 2.0)]
    [InlineData(new double[] { 1, 1, 2, 1 }, 4.0)]
    [InlineData(new double[] { 1, 3, 3, 1 }, 3.0)]
    [InlineData(new double[] { 0, 0, 1, 1 }, 7.0)]
    [InlineData(new double[] { 1, 1, 1, 7 }, 10.0)]
    [InlineData(new double[] { 7, 1, 1, 1 }, 0.0)]
    public void The_weighted_median_is_an_averaged_percentile(double[] weights, double expected)
    {
        double[] residuals = [0.0, 2.0, 4.0, 10.0];
        double[] zeros = [0.0, 0.0, 0.0, 0.0];

        Assert.Equal(expected, MedianAbsoluteError.Score(residuals, zeros, 1, weights), 12);
    }

    /// <summary>
    /// The uniform weights above (1, 2, 0.5, 7) are all exact in binary, so none
    /// exercises scikit-learn's epsilon tolerance on the cumulative-weight
    /// comparison. 0.1 is not exact: at n = 6, 8, 10 the rounding error pushes
    /// the cumulative sum past half by less than one epsilon, and scikit-learn
    /// still averages there. The 0.7 rows overshoot by more than an epsilon, so
    /// scikit-learn does not average — they fail an implementation that reads
    /// "uniform weight" as an unconditional average. Rows that passed before
    /// the tolerance landed stay as the regression guard.
    /// </summary>
    [Theory]
    [InlineData(0.1, 4, 1.5)]
    [InlineData(0.1, 6, 2.5)]
    [InlineData(0.1, 8, 3.5)]
    [InlineData(0.1, 10, 4.5)]
    [InlineData(0.1, 12, 5.5)]
    [InlineData(0.7, 6, 3.0)]
    [InlineData(0.7, 8, 4.0)]
    [InlineData(0.7, 10, 5.0)]
    [InlineData(1.0 / 3.0, 6, 2.5)]
    [InlineData(1.0 / 3.0, 8, 3.5)]
    [InlineData(1.0 / 3.0, 10, 5.0)]
    public void A_fractional_uniform_weight_averages_within_one_machine_epsilon(
        double weight, int count, double expected)
    {
        double[] residuals = new double[count];
        double[] zeros = new double[count];
        double[] weights = new double[count];
        for (int i = 0; i < count; i++)
        {
            residuals[i] = i;
            weights[i] = weight;
        }

        Assert.Equal(expected, MedianAbsoluteError.Score(residuals, zeros, 1, weights), 12);
    }

    [Theory]
    // A three-value fixture, plus the two-value one where the weighted percentile
    // collapses to the majority-weight endpoint, or averages when the weights tie.
    [InlineData(new double[] { 1, 1, 1 }, new double[] { 0, 2, 4 }, 2.0)]
    [InlineData(new double[] { 1, 3 }, new double[] { 0, 10 }, 10.0)]
    [InlineData(new double[] { 3, 1 }, new double[] { 0, 10 }, 0.0)]
    [InlineData(new double[] { 1, 1 }, new double[] { 0, 10 }, 5.0)]
    public void Shorter_fixtures_reproduce_the_measured_values(double[] weights, double[] residuals, double expected)
    {
        double[] zeros = new double[residuals.Length];

        Assert.Equal(expected, MedianAbsoluteError.Score(residuals, zeros, 1, weights), 12);
    }

    [Fact]
    public void Uniform_weights_give_the_unweighted_median()
    {
        // Invariant 1 of the spec: the weighted and unweighted paths must agree,
        // including on the even-count average of the two middle values.
        double[] residuals = [0.0, 2.0, 4.0, 10.0];
        double[] zeros = [0.0, 0.0, 0.0, 0.0];

        Assert.Equal(
            MedianAbsoluteError.Score(residuals, zeros),
            MedianAbsoluteError.Score(residuals, zeros, 1, [1.0, 1.0, 1.0, 1.0]),
            12);
    }

    /// <summary>
    /// The invariant above holds for a weight of 1, not for uniform weights in
    /// general — more than scikit-learn promises. Measured against scikit-learn
    /// 1.9.0: residuals 0…9 under a uniform weight of 0.7 give 5.0 weighted and
    /// 4.5 unweighted, because the cumulative weight there overshoots half the
    /// total by more than a machine epsilon and the averaging branch does not
    /// fire. Reproducing that disagreement is parity; removing it would not be.
    /// </summary>
    [Fact]
    public void A_uniform_weight_is_not_a_promise_of_the_unweighted_median()
    {
        double[] residuals = [0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0];
        double[] zeros = new double[10];
        double[] weights = [0.7, 0.7, 0.7, 0.7, 0.7, 0.7, 0.7, 0.7, 0.7, 0.7];

        Assert.Equal(4.5, MedianAbsoluteError.Score(residuals, zeros), 12);
        Assert.Equal(5.0, MedianAbsoluteError.Score(residuals, zeros, 1, weights), 12);
    }

    [Fact]
    public void An_odd_count_needs_no_averaging()
    {
        double[] residuals = [0.0, 2.0, 4.0];
        double[] zeros = [0.0, 0.0, 0.0];

        Assert.Equal(2.0, MedianAbsoluteError.Score(residuals, zeros), 12);
        Assert.Equal(2.0, MedianAbsoluteError.Score(residuals, zeros, 1, [1.0, 1.0, 1.0]), 12);
    }
}
