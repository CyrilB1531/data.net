using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class RegressionValidationTests
{
    [Fact]
    public void A_NaN_input_is_refused_with_scikit_learns_own_words()
    {
        double[] yTrue = [1.0, double.NaN];
        double[] yPred = [1.0, 1.0];

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => MeanSquaredError.Score(yTrue, yPred));

        Assert.Contains("Input contains NaN.", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_infinite_input_is_refused_with_the_other_message()
    {
        // scikit-learn has two distinct messages here, not one. Collapsing them
        // into a single "input is not finite" would still throw, and would still
        // pass a test that only asserted the type.
        double[] yTrue = [1.0, double.PositiveInfinity];
        double[] yPred = [1.0, 1.0];

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => MeanSquaredError.Score(yTrue, yPred));

        Assert.Contains("Input contains infinity", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_length_that_does_not_divide_by_the_output_count_is_refused()
    {
        double[] yTrue = [1.0, 2.0, 3.0, 4.0, 5.0];
        double[] yPred = [1.0, 2.0, 3.0, 4.0, 5.0];

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => MeanSquaredError.Score(yTrue, yPred, outputCount: 2));

        Assert.Contains("5", error.Message, StringComparison.Ordinal);
        Assert.Contains("2", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_output_count_below_one_is_refused()
    {
        double[] y = [1.0, 2.0];

        Assert.Throws<ArgumentOutOfRangeException>(() => MeanSquaredError.Score(y, y, outputCount: 0));
    }

    [Fact]
    public void Output_weights_that_do_not_match_the_output_count_are_refused()
    {
        double[] yTrue = [1.0, 2.0, 3.0, 4.0];
        double[] yPred = [1.0, 2.0, 3.0, 4.0];
        double[] outputWeights = [0.5, 0.3, 0.2];

        Assert.Throws<ArgumentException>(
            () => MeanSquaredError.Score(yTrue, yPred, 2, outputWeights: outputWeights));
    }

    [Fact]
    public void Disagreeing_lengths_and_empty_input_are_refused()
    {
        Assert.Throws<ArgumentException>(() => MeanSquaredError.Score([1.0, 2.0], [1.0]));
        Assert.Throws<ArgumentException>(() => MeanSquaredError.Score([], []));
    }

    [Fact]
    public void A_sample_weight_of_the_wrong_length_is_refused()
    {
        double[] yTrue = [1.0, 2.0, 3.0, 4.0];
        double[] yPred = [1.0, 2.0, 3.0, 4.0];

        // Two samples of two outputs each: the weight is per sample, not per value.
        Assert.Throws<ArgumentException>(
            () => MeanSquaredError.Score(yTrue, yPred, 2, sampleWeight: [1.0, 2.0, 3.0, 4.0]));
    }
}
