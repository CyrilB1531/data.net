using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

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
    // Residuals [0, 2, 4, 10] against a zero prediction, one row per measured
    // weighting. The uniform rows are the ones a "value at the 50% point"
    // implementation gets wrong, and only because the count is even.
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

    [Theory]
    // A shorter, three-value fixture, and the two-value fixture where the
    // weighted percentile collapses to whichever endpoint holds the majority
    // weight — or the plain average when the two are tied.
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
        // Invariant 1 of the spec: the weighted path and the unweighted path must
        // not disagree where they have no reason to, including on the average of
        // the two middle values that an even count produces.
        double[] residuals = [0.0, 2.0, 4.0, 10.0];
        double[] zeros = [0.0, 0.0, 0.0, 0.0];

        Assert.Equal(
            MedianAbsoluteError.Score(residuals, zeros),
            MedianAbsoluteError.Score(residuals, zeros, 1, [1.0, 1.0, 1.0, 1.0]),
            12);
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
