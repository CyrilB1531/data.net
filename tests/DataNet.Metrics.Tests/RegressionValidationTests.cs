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
        // scikit-learn has two distinct messages, not one; a single "input is not
        // finite" would still throw and still pass a type-only assertion.
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

    [Fact]
    public void A_sample_weight_that_is_zero_throughout_is_refused()
    {
        // Measured: scikit-learn 1.9.0 raises this on mse, median_ae, r2 and
        // explained_variance alike; unguarded, mse gives NaN and median_ae a number.
        double[] yTrue = [1.0, 2.0, 3.0];
        double[] yPred = [1.0, 2.0, 4.0];

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => MeanSquaredError.Score(yTrue, yPred, 1, [0.0, 0.0, 0.0]));

        Assert.Contains(
            "Sample weights must contain at least one non-zero number.",
            error.Message,
            StringComparison.Ordinal);

        Assert.Throws<ArgumentException>(
            () => MedianAbsoluteError.Score(yTrue, yPred, 1, [0.0, 0.0, 0.0]));
        Assert.Throws<ArgumentException>(() => R2.Score(yTrue, yPred, 1, [0.0, 0.0, 0.0]));
    }

    [Fact]
    public void A_sample_weight_that_merely_sums_to_zero_is_not_this_check_s_business()
    {
        // The rule is "every weight is zero", not "the weights sum to zero" —
        // scikit-learn scores an all-negative weight happily.
        double[] yTrue = [1.0, 2.0, 3.0];
        double[] yPred = [1.0, 2.0, 4.0];

        Assert.Equal(0.5, MeanSquaredError.Score(yTrue, yPred, 1, [-1.0, -2.0, -3.0]), 12);

        // And a single subnormal weight is a non-zero number, which scikit-learn
        // accepts and scores.
        Assert.Equal(1.0, MeanSquaredError.Score(yTrue, yPred, 1, [0.0, 0.0, 1e-320]), 12);
    }

    [Fact]
    public void Output_weights_that_sum_to_zero_are_refused_with_numpys_words()
    {
        // numpy.average's own rule is the sum: [1, -1] is refused though not all
        // zero, and [-1, -1] is accepted though every weight is negative.
        double[] yTrue = [1.0, 2.0, 3.0, 4.0, 5.0, 7.0];
        double[] yPred = [1.0, 3.0, 2.0, 4.0, 5.0, 6.0];

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => MeanSquaredError.Score(yTrue, yPred, 2, outputWeights: [0.0, 0.0]));

        Assert.Contains(
            "Weights sum to zero, can't be normalized.", error.Message, StringComparison.Ordinal);

        Assert.Throws<ArgumentException>(
            () => MeanSquaredError.Score(yTrue, yPred, 2, outputWeights: [1.0, -1.0]));

        Assert.Equal(0.5, MeanSquaredError.Score(yTrue, yPred, 2, outputWeights: [-1.0, -1.0]), 12);
    }
}
