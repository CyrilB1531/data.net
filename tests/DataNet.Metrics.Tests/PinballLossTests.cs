using System.Globalization;
using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class PinballLossTests
{
    [Theory]
    [MemberData(nameof(RegressionCorpus.Indices), MemberType = typeof(RegressionCorpus))]
    public void Matches_sklearn_mean_pinball_loss(int index)
    {
        JsonElement c = RegressionCorpus.Cases[index];
        double[] yTrue = RegressionCorpus.Doubles(c, "y_true");
        double[] yPred = RegressionCorpus.Doubles(c, "y_pred");
        int k = RegressionCorpus.OutputCount(c);
        double[] sw = RegressionCorpus.OptionalDoubles(c, "sample_weight");
        string who = RegressionCorpus.Describe(c);

        foreach (double alpha in new[] { 0.5, 0.9 })
        {
            string tag = $"pinball{alpha.ToString("0.#", CultureInfo.InvariantCulture)}";
            RegressionCorpus.AssertClose(
                RegressionCorpus.Value(c, $"{tag}|uniform"),
                PinballLoss.Score(yTrue, yPred, alpha, k, sw), $"{who} {tag}|uniform");
            RegressionCorpus.AssertClose(
                RegressionCorpus.Values(c, $"{tag}|raw"),
                PinballLoss.PerOutput(yTrue, yPred, alpha, k, sw), $"{who} {tag}|raw");

            if (RegressionCorpus.Has(c, $"{tag}|weights"))
            {
                RegressionCorpus.AssertClose(
                    RegressionCorpus.Value(c, $"{tag}|weights"),
                    PinballLoss.Score(yTrue, yPred, alpha, k, sw, RegressionCorpus.OutputWeights(c)),
                    $"{who} {tag}|weights");
            }
        }
    }

    [Fact]
    public void Weights_theory_actually_ran_on_at_least_one_case()
    {
        // Guards against the "pinball{a}|weights" branch above never firing: only
        // the multi-output fixtures carry it.
        Assert.Contains(RegressionCorpus.Cases, c =>
            RegressionCorpus.Has(c, "pinball0.5|weights") || RegressionCorpus.Has(c, "pinball0.9|weights"));
    }

    [Fact]
    public void At_one_half_it_is_half_the_mean_absolute_error()
    {
        // Invariant 6 of the spec, and the one test that would fail if alpha were
        // wired into the wrong side of the max. Measured: both are 0.25.
        double[] yTrue = [3.0, -0.5, 2.0, 7.0];
        double[] yPred = [2.5, 0.0, 2.0, 8.0];

        Assert.Equal(0.25, PinballLoss.Score(yTrue, yPred), 12);
        Assert.Equal(MeanAbsoluteError.Score(yTrue, yPred) / 2.0, PinballLoss.Score(yTrue, yPred), 12);
    }

    [Fact]
    public void A_high_quantile_punishes_undershooting_more_than_overshooting()
    {
        // alpha = 0.9 asks for a prediction the truth exceeds only 10% of the
        // time, so being too low costs nine times as much as being too high.
        Assert.Equal(0.9, PinballLoss.Score([1.0], [0.0], 0.9), 12);
        Assert.Equal(0.1, PinballLoss.Score([0.0], [1.0], 0.9), 12);
    }

    [Fact]
    public void Reproduces_the_hand_measured_values()
    {
        double[] yTrue = [3.0, -0.5, 2.0, 7.0];
        double[] yPred = [2.5, 0.0, 2.0, 8.0];

        Assert.Equal(0.15, PinballLoss.Score(yTrue, yPred, 0.9), 12);
        Assert.Equal(0.275, PinballLoss.Score(yTrue, yPred, 0.5, 1, [1.0, 2.0, 3.0, 4.0]), 12);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void An_alpha_outside_the_unit_interval_is_refused(double alpha)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PinballLoss.Score([1.0, 2.0], [1.0, 2.0], alpha));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void The_endpoints_are_accepted(double alpha)
    {
        // scikit-learn's range is closed, so 0 and 1 are legal quantiles.
        Assert.Equal(0.0, PinballLoss.Score([1.0, 2.0], [1.0, 2.0], alpha), 12);
    }
}
