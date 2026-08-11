using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class PercentageAndMaxErrorTests
{
    [Theory]
    [MemberData(nameof(RegressionCorpus.Indices), MemberType = typeof(RegressionCorpus))]
    public void Matches_sklearn_mean_absolute_percentage_error(int index)
    {
        JsonElement c = RegressionCorpus.Cases[index];
        double[] yTrue = RegressionCorpus.Doubles(c, "y_true");
        double[] yPred = RegressionCorpus.Doubles(c, "y_pred");
        int k = RegressionCorpus.OutputCount(c);
        double[] sw = RegressionCorpus.OptionalDoubles(c, "sample_weight");
        string who = RegressionCorpus.Describe(c);

        RegressionCorpus.AssertClose(
            RegressionCorpus.Value(c, "mape|uniform"),
            MeanAbsolutePercentageError.Score(yTrue, yPred, k, sw), $"{who} mape|uniform");
        RegressionCorpus.AssertClose(
            RegressionCorpus.Values(c, "mape|raw"),
            MeanAbsolutePercentageError.PerOutput(yTrue, yPred, k, sw), $"{who} mape|raw");

        if (RegressionCorpus.Has(c, "mape|weights"))
        {
            RegressionCorpus.AssertClose(
                RegressionCorpus.Value(c, "mape|weights"),
                MeanAbsolutePercentageError.Score(yTrue, yPred, k, sw, RegressionCorpus.OutputWeights(c)),
                $"{who} mape|weights");
        }
    }

    [Theory]
    [MemberData(nameof(RegressionCorpus.Indices), MemberType = typeof(RegressionCorpus))]
    public void Matches_sklearn_max_error(int index)
    {
        JsonElement c = RegressionCorpus.Cases[index];
        if (!RegressionCorpus.Has(c, "max_error|uniform"))
        {
            // Only the unweighted single-output cases carry it: max_error accepts
            // neither sample_weight nor multioutput, and refuses 2-D input.
            return;
        }

        RegressionCorpus.AssertClose(
            RegressionCorpus.Value(c, "max_error|uniform"),
            MaxError.Score(RegressionCorpus.Doubles(c, "y_true"), RegressionCorpus.Doubles(c, "y_pred")),
            $"{RegressionCorpus.Describe(c)} max_error");
    }

    [Fact]
    public void Max_error_theory_actually_ran_on_at_least_one_case()
    {
        // Guards against the theory above silently passing by early-returning on
        // every case: max_error|uniform is expected on the unweighted
        // single-output fixtures, so at least one must carry it.
        Assert.Contains(RegressionCorpus.Cases, c => RegressionCorpus.Has(c, "max_error|uniform"));
    }

    [Fact]
    public void The_clamp_uses_machine_epsilon_not_double_Epsilon()
    {
        // 1 / 2**-52 exactly. With double.Epsilon the answer would be about
        // 2e323 — both are "a large finite number", and only this pins which.
        Assert.Equal(4503599627370496.0, MeanAbsolutePercentageError.Score([0.0], [1.0]), 12);
    }

    [Fact]
    public void Reproduces_the_hand_measured_value()
    {
        double[] yTrue = [3.0, -0.5, 2.0, 7.0];
        double[] yPred = [2.5, 0.0, 2.0, 8.0];

        Assert.Equal(0.3273809523809524, MeanAbsolutePercentageError.Score(yTrue, yPred), 12);
        Assert.Equal(1.0, MaxError.Score(yTrue, yPred), 12);
    }

    [Fact]
    public void MaxError_is_the_largest_residual_in_either_direction()
    {
        Assert.Equal(4.0, MaxError.Score([1.0, 10.0], [5.0, 9.0]), 12);
    }
}
