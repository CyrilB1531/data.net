using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class PrfValidationTests
{
    // Class 2 is never predicted, so its precision divides by zero.
    private static readonly int[] YTrue = [0, 0, 1, 1, 2, 2];
    private static readonly int[] YPred = [0, 1, 1, 1, 0, 1];

    [Fact]
    public void Zero_division_zero_matches_sklearn_s_default_value()
    {
        double[] perClass = Precision.PerClass(YTrue, YPred);
        Assert.Equal(0.0, perClass[2], MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Zero_division_one_returns_one()
    {
        double[] perClass = Precision.PerClass(YTrue, YPred, ZeroDivision.One);
        Assert.Equal(1.0, perClass[2], MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Zero_division_nan_returns_nan()
    {
        double[] perClass = Precision.PerClass(YTrue, YPred, ZeroDivision.NaN);
        Assert.True(double.IsNaN(perClass[2]));
    }

    [Fact]
    public void Zero_division_throw_raises_instead_of_returning_a_silent_zero()
    {
        Assert.Throws<UndefinedMetricException>(
            () => Precision.PerClass(YTrue, YPred, ZeroDivision.Throw));
    }

    [Fact]
    public void Binary_averaging_rejects_a_three_class_target()
    {
        Assert.Throws<ArgumentException>(
            () => Precision.Score(YTrue, YPred, Averaging.Binary));
    }

    [Fact]
    public void Binary_averaging_rejects_a_pos_label_outside_the_target()
    {
        int[] yTrue = [0, 1, 1, 0];
        int[] yPred = [0, 1, 0, 0];
        Assert.Throws<ArgumentException>(
            () => Precision.Score(yTrue, yPred, Averaging.Binary, posLabel: 9));
    }

    [Fact]
    public void FBeta_rejects_a_negative_beta()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FBeta.Score(YTrue, YPred, -1.0, Averaging.Macro));
    }

    [Fact]
    public void FBeta_PerClass_rejects_a_negative_beta()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FBeta.PerClass(YTrue, YPred, -1.0));
    }

    [Fact]
    public void Micro_averaged_f1_equals_accuracy_when_no_label_is_excluded()
    {
        double micro = F1.Score(YTrue, YPred, Averaging.Micro);
        Assert.Equal(Accuracy.Score(YTrue, YPred), micro, MetricsCorpus.Tolerance);
    }
}
