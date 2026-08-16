using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

public sealed class MeanSquaredErrorTests
{
    [Theory]
    [MemberData(nameof(RegressionCorpus.Indices), MemberType = typeof(RegressionCorpus))]
    public void Matches_sklearn_mean_squared_error(int index)
    {
        JsonElement c = RegressionCorpus.Cases[index];
        double[] yTrue = RegressionCorpus.Doubles(c, "y_true");
        double[] yPred = RegressionCorpus.Doubles(c, "y_pred");
        int k = RegressionCorpus.OutputCount(c);
        double[] sw = RegressionCorpus.OptionalDoubles(c, "sample_weight");
        string who = RegressionCorpus.Describe(c);

        RegressionCorpus.AssertClose(
            RegressionCorpus.Value(c, "mse|uniform"),
            MeanSquaredError.Score(yTrue, yPred, k, sw),
            $"{who} mse|uniform");
        RegressionCorpus.AssertClose(
            RegressionCorpus.Values(c, "mse|raw"),
            MeanSquaredError.PerOutput(yTrue, yPred, k, sw),
            $"{who} mse|raw");

        if (RegressionCorpus.Has(c, "mse|weights"))
        {
            RegressionCorpus.AssertClose(
                RegressionCorpus.Value(c, "mse|weights"),
                MeanSquaredError.Score(yTrue, yPred, k, sw, RegressionCorpus.OutputWeights(c)),
                $"{who} mse|weights");
        }
    }

    [Theory]
    [MemberData(nameof(RegressionCorpus.Indices), MemberType = typeof(RegressionCorpus))]
    public void Matches_sklearn_root_mean_squared_error(int index)
    {
        JsonElement c = RegressionCorpus.Cases[index];
        double[] yTrue = RegressionCorpus.Doubles(c, "y_true");
        double[] yPred = RegressionCorpus.Doubles(c, "y_pred");
        int k = RegressionCorpus.OutputCount(c);
        double[] sw = RegressionCorpus.OptionalDoubles(c, "sample_weight");
        string who = RegressionCorpus.Describe(c);

        RegressionCorpus.AssertClose(
            RegressionCorpus.Value(c, "rmse|uniform"),
            RootMeanSquaredError.Score(yTrue, yPred, k, sw),
            $"{who} rmse|uniform");
        RegressionCorpus.AssertClose(
            RegressionCorpus.Values(c, "rmse|raw"),
            RootMeanSquaredError.PerOutput(yTrue, yPred, k, sw),
            $"{who} rmse|raw");

        if (RegressionCorpus.Has(c, "rmse|weights"))
        {
            RegressionCorpus.AssertClose(
                RegressionCorpus.Value(c, "rmse|weights"),
                RootMeanSquaredError.Score(yTrue, yPred, k, sw, RegressionCorpus.OutputWeights(c)),
                $"{who} rmse|weights");
        }
    }

    [Fact]
    public void Reproduces_the_hand_measured_single_output_values()
    {
        double[] yTrue = [3.0, -0.5, 2.0, 7.0];
        double[] yPred = [2.5, 0.0, 2.0, 8.0];

        Assert.Equal(0.375, MeanSquaredError.Score(yTrue, yPred), 12);
        Assert.Equal(0.6123724356957945, RootMeanSquaredError.Score(yTrue, yPred), 12);
        Assert.Equal(0.475, MeanSquaredError.Score(yTrue, yPred, 1, [1.0, 2.0, 3.0, 4.0]), 12);
    }

    [Fact]
    public void Reproduces_the_hand_measured_multioutput_values()
    {
        // Row-major: sample 0's two outputs, then sample 1's, then sample 2's.
        double[] yTrue = [0.5, 1.0, -1.0, 1.0, 7.0, -6.0];
        double[] yPred = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];

        RegressionCorpus.AssertClose(
            [0.4166666666666667, 1.0], MeanSquaredError.PerOutput(yTrue, yPred, 2), "mse per-output");
        Assert.Equal(0.7083333333333334, MeanSquaredError.Score(yTrue, yPred, 2), 12);
        Assert.Equal(0.825, MeanSquaredError.Score(yTrue, yPred, 2, default, [0.3, 0.7]), 12);
        RegressionCorpus.AssertClose(
            [0.6454972243679028, 1.0], RootMeanSquaredError.PerOutput(yTrue, yPred, 2), "rmse per-output");
    }

    [Fact]
    public void The_root_is_taken_per_output_not_on_the_reduced_value()
    {
        // sqrt(mean(x)) != mean(sqrt(x)) for unequal outputs; scikit-learn's
        // multioutput RMSE takes the root first (verified in task-2-report.md).
        double[] yTrue = [0.5, 1.0, -1.0, 1.0, 7.0, -6.0];
        double[] yPred = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];

        Assert.Equal(0.8227486121839513, RootMeanSquaredError.Score(yTrue, yPred, 2), 12);
        Assert.NotEqual(
            Math.Sqrt(MeanSquaredError.Score(yTrue, yPred, 2)),
            RootMeanSquaredError.Score(yTrue, yPred, 2),
            12);
    }

    [Fact]
    public void PerOutput_at_one_output_is_the_scalar_score()
    {
        double[] yTrue = [3.0, -0.5, 2.0, 7.0];
        double[] yPred = [2.5, 0.0, 2.0, 8.0];

        Assert.Equal(MeanSquaredError.Score(yTrue, yPred), Assert.Single(MeanSquaredError.PerOutput(yTrue, yPred)));
    }
}
