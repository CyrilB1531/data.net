using Xunit;

namespace Lodestar.Conformal.Tests;

/// <summary>
/// The edges no oracle can carry: MAPIE raises where these return, so what they assert is
/// decision 0070 rather than a frozen value.
/// </summary>
public sealed class SplitConformalEdgeTests
{
    // k = ceil(10 * 0.95) = 10 > 9: the level asks for a score the set does not hold.
    private static readonly double[] NineScores = [0.2, 0.1, 0.4, 0.3, 0.5, 0.1, 0.4, 0.3, 0.1];

    [Fact]
    public void A_calibration_set_too_small_for_the_level_yields_an_infinite_quantile() =>
        Assert.Equal(double.PositiveInfinity, SplitConformal.Quantile(NineScores, 0.05));

    [Fact]
    public void An_infinite_quantile_yields_the_whole_line()
    {
        (double Lower, double Upper) interval =
            SplitConformal.Interval(4.0, double.PositiveInfinity);

        Assert.Equal(double.NegativeInfinity, interval.Lower);
        Assert.Equal(double.PositiveInfinity, interval.Upper);
    }

    [Fact]
    public void An_infinite_quantile_yields_the_full_label_set() =>
        Assert.Equal([true, true, true],
                     SplitConformal.PredictionSet([0.4, 0.35, 0.25], double.PositiveInfinity));

    [Fact]
    public void The_ceiling_does_not_round_an_exact_integer_up()
    {
        // (n + 1)(1 - alpha) = 20 * 0.9 = 18 exactly, so k is 18: the 18th smallest, not the
        // 19th, which is what numpy's method="higher" reads at this level (decision 0070).
        double[] scores = [.. Enumerable.Range(1, 19).Select(value => (double)value)];

        Assert.Equal(18.0, SplitConformal.Quantile(scores, 0.1));
    }

    [Fact]
    public void One_calibration_score_is_enough_at_a_level_it_can_answer() =>
        Assert.Equal(7.0, SplitConformal.Quantile([7.0], 0.5));

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    public void A_level_outside_the_open_unit_interval_is_refused(double alpha) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SplitConformal.Quantile(NineScores, alpha));

    [Fact]
    public void An_empty_calibration_set_is_refused() =>
        Assert.Throws<ArgumentException>(() => SplitConformal.Quantile([], 0.1));

    [Fact]
    public void Residuals_refuse_spans_of_different_lengths() =>
        Assert.Throws<ArgumentException>(
            () => SplitConformal.AbsoluteResiduals([1.0, 2.0], [1.0]));

    [Fact]
    public void Residuals_are_the_absolute_difference() =>
        Assert.Equal([1.0, 0.5, 0.0],
                     SplitConformal.AbsoluteResiduals([1.0, 2.0, 3.0], [2.0, 1.5, 3.0]));

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void A_quantile_that_is_not_a_score_is_refused(double quantile)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SplitConformal.Interval(1.0, quantile));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SplitConformal.PredictionSet([0.5, 0.5], quantile));
    }

    [Fact]
    public void A_class_count_that_is_not_positive_is_refused() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SplitConformal.LeastAmbiguousScores([0.5, 0.5], [0], 0));

    [Fact]
    public void A_probability_block_that_does_not_fit_the_labels_is_refused() =>
        Assert.Throws<ArgumentException>(
            () => SplitConformal.LeastAmbiguousScores([0.5, 0.5, 0.5], [0, 1], 2));

    [Fact]
    public void A_label_outside_the_class_range_is_refused() =>
        Assert.Throws<ArgumentException>(
            () => SplitConformal.LeastAmbiguousScores([0.5, 0.5], [2], 2));

    [Fact]
    public void A_class_exactly_on_the_threshold_is_included() =>
        // LAC includes the boundary: p >= 1 - q, not p > 1 - q.
        Assert.Equal([true, true], SplitConformal.PredictionSet([0.25, 0.75], 0.75));

    [Fact]
    public void A_zero_quantile_keeps_only_a_certain_class() =>
        Assert.Equal([false, true], SplitConformal.PredictionSet([0.0, 1.0], 0.0));
}
