using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

public sealed class R2Tests
{
    [Theory]
    [MemberData(nameof(RegressionCorpus.Indices), MemberType = typeof(RegressionCorpus))]
    public void Matches_sklearn_r2_score(int index)
    {
        JsonElement c = RegressionCorpus.Cases[index];
        double[] yTrue = RegressionCorpus.Doubles(c, "y_true");
        double[] yPred = RegressionCorpus.Doubles(c, "y_pred");
        int k = RegressionCorpus.OutputCount(c);
        double[] sw = RegressionCorpus.OptionalDoubles(c, "sample_weight");
        string who = RegressionCorpus.Describe(c);

        foreach (bool forceFinite in new[] { true, false })
        {
            string flag = forceFinite ? "force_finite" : "raw_infinity";
            RegressionCorpus.AssertClose(
                RegressionCorpus.Value(c, $"r2|uniform|{flag}"),
                R2.Score(yTrue, yPred, k, sw, default, forceFinite),
                $"{who} r2|uniform|{flag}");
            RegressionCorpus.AssertClose(
                RegressionCorpus.Values(c, $"r2|raw|{flag}"),
                R2.PerOutput(yTrue, yPred, k, sw, forceFinite),
                $"{who} r2|raw|{flag}");
            RegressionCorpus.AssertClose(
                RegressionCorpus.Value(c, $"r2|variance_weighted|{flag}"),
                R2.VarianceWeighted(yTrue, yPred, k, sw, forceFinite),
                $"{who} r2|variance_weighted|{flag}");

            // Only the wider fixtures carry an explicit multioutput=[…] value,
            // because the generator has no output weights to freeze at one output.
            if (RegressionCorpus.Has(c, $"r2|weights|{flag}"))
            {
                RegressionCorpus.AssertClose(
                    RegressionCorpus.Value(c, $"r2|weights|{flag}"),
                    R2.Score(yTrue, yPred, k, sw, RegressionCorpus.OutputWeights(c), forceFinite),
                    $"{who} r2|weights|{flag}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(RegressionCorpus.Indices), MemberType = typeof(RegressionCorpus))]
    public void Matches_sklearn_explained_variance_score(int index)
    {
        JsonElement c = RegressionCorpus.Cases[index];
        double[] yTrue = RegressionCorpus.Doubles(c, "y_true");
        double[] yPred = RegressionCorpus.Doubles(c, "y_pred");
        int k = RegressionCorpus.OutputCount(c);
        double[] sw = RegressionCorpus.OptionalDoubles(c, "sample_weight");
        string who = RegressionCorpus.Describe(c);

        foreach (bool forceFinite in new[] { true, false })
        {
            string flag = forceFinite ? "force_finite" : "raw_infinity";
            RegressionCorpus.AssertClose(
                RegressionCorpus.Value(c, $"ev|uniform|{flag}"),
                ExplainedVariance.Score(yTrue, yPred, k, sw, default, forceFinite),
                $"{who} ev|uniform|{flag}");
            RegressionCorpus.AssertClose(
                RegressionCorpus.Values(c, $"ev|raw|{flag}"),
                ExplainedVariance.PerOutput(yTrue, yPred, k, sw, forceFinite),
                $"{who} ev|raw|{flag}");
            RegressionCorpus.AssertClose(
                RegressionCorpus.Value(c, $"ev|variance_weighted|{flag}"),
                ExplainedVariance.VarianceWeighted(yTrue, yPred, k, sw, forceFinite),
                $"{who} ev|variance_weighted|{flag}");

            if (RegressionCorpus.Has(c, $"ev|weights|{flag}"))
            {
                RegressionCorpus.AssertClose(
                    RegressionCorpus.Value(c, $"ev|weights|{flag}"),
                    ExplainedVariance.Score(yTrue, yPred, k, sw, RegressionCorpus.OutputWeights(c), forceFinite),
                    $"{who} ev|weights|{flag}");
            }
        }
    }

    [Fact]
    public void Fewer_than_two_samples_is_zeroDivisions_case_and_not_forceFinites()
    {
        // nan under either setting of forceFinite. Routing this through
        // forceFinite would return -inf, and every ordinary fixture would agree.
        Assert.True(double.IsNaN(R2.Score([2.0], [5.0])));
        Assert.True(double.IsNaN(R2.Score([2.0], [5.0], forceFinite: false)));
        Assert.True(double.IsNaN(R2.Score([2.0], [2.0])));

        Assert.Equal(0.0, R2.Score([2.0], [5.0], zeroDivision: ZeroDivision.Zero), 12);
        Assert.Throws<UndefinedMetricException>(
            () => R2.Score([2.0], [5.0], zeroDivision: ZeroDivision.Throw));
    }

    [Fact]
    public void Zero_variance_over_two_samples_is_forceFinites_case_and_not_zeroDivisions()
    {
        Assert.Equal(1.0, R2.Score([2.0, 2.0], [2.0, 2.0]), 12);
        Assert.Equal(0.0, R2.Score([2.0, 2.0], [2.0, 3.0]), 12);
        Assert.True(double.IsNaN(R2.Score([2.0, 2.0], [2.0, 2.0], forceFinite: false)));
        Assert.True(double.IsNegativeInfinity(R2.Score([2.0, 2.0], [2.0, 3.0], forceFinite: false)));

        // zeroDivision does not reach it: Throw does not throw here.
        Assert.Equal(0.0, R2.Score([2.0, 2.0], [2.0, 3.0], zeroDivision: ZeroDivision.Throw), 12);
    }

    [Fact]
    public void Explained_variance_is_one_on_a_single_wrong_sample()
    {
        // Its own definition, not a rounding accident: one residual has zero
        // variance. This is why ExplainedVariance takes no zeroDivision.
        Assert.Equal(1.0, ExplainedVariance.Score([3.0], [5.0]), 12);

        // Unclamped, the same case is nan, not -inf: the "perfect" side of the
        // zero-variance branch, numerator and denominator both vanished.
        Assert.True(double.IsNaN(ExplainedVariance.Score([3.0], [5.0], forceFinite: false)));
    }

    [Fact]
    public void Reproduces_the_hand_measured_values()
    {
        double[] yTrue = [3.0, -0.5, 2.0, 7.0];
        double[] yPred = [2.5, 0.0, 2.0, 8.0];

        Assert.Equal(0.9486081370449679, R2.Score(yTrue, yPred), 12);
        Assert.Equal(0.9571734475374732, ExplainedVariance.Score(yTrue, yPred), 12);
        Assert.Equal(0.9459613196814562, R2.Score(yTrue, yPred, 1, [1.0, 2.0, 3.0, 4.0]), 12);

        double[] mtTrue = [0.5, 1.0, -1.0, 1.0, 7.0, -6.0];
        double[] mtPred = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];
        RegressionCorpus.AssertClose(
            [0.9654377880184332, 0.9081632653061225], R2.PerOutput(mtTrue, mtPred, 2), "r2 per-output");
        RegressionCorpus.AssertClose(
            [0.967741935483871, 1.0], ExplainedVariance.PerOutput(mtTrue, mtPred, 2), "ev per-output");
        Assert.Equal(0.9368005266622779, R2.Score(mtTrue, mtPred, 2), 12);
        Assert.Equal(0.9382566585956417, R2.VarianceWeighted(mtTrue, mtPred, 2), 12);
        Assert.Equal(0.9838709677419355, ExplainedVariance.Score(mtTrue, mtPred, 2), 12);
        Assert.Equal(0.9830508474576269, ExplainedVariance.VarianceWeighted(mtTrue, mtPred, 2), 12);
    }

    [Fact]
    public void Uniform_averaging_is_the_mean_of_the_per_output_scores()
    {
        // Invariant 5 of the spec. It also proves variance_weighted is doing
        // something else: on this fixture the two differ in the third decimal.
        double[] yTrue = [0.5, 1.0, -1.0, 1.0, 7.0, -6.0];
        double[] yPred = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];

        double[] perOutput = R2.PerOutput(yTrue, yPred, 2);
        Assert.Equal((perOutput[0] + perOutput[1]) / 2.0, R2.Score(yTrue, yPred, 2), 12);
        Assert.NotEqual(R2.Score(yTrue, yPred, 2), R2.VarianceWeighted(yTrue, yPred, 2), 12);
    }

    [Fact]
    public void Explained_variance_beats_r2_on_a_biased_prediction()
    {
        // Explained variance subtracts the mean residual before squaring, so a
        // constant bias explains all the variance and scores 1; R² pays for it.
        double[] yTrue = [1.0, 2.0, 3.0, 4.0];
        double[] biased = [3.0, 4.0, 5.0, 6.0];

        Assert.Equal(1.0, ExplainedVariance.Score(yTrue, biased), 12);
        Assert.Equal(-2.2, R2.Score(yTrue, biased), 12);
    }
}
