using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>What the two table tests refuse, and where Yates does and does not apply.</summary>
public sealed class TableTestEdgeTests
{
    [Fact]
    public void GoodnessOfFit_refuses_expectations_that_do_not_sum_to_the_observations()
    {
        Assert.Throws<ArgumentException>(
            () => ChiSquare.GoodnessOfFit([10.0, 10.0], [5.0, 6.0]));
    }

    [Fact]
    public void GoodnessOfFit_refuses_a_zero_expectation()
    {
        Assert.Throws<ArgumentException>(
            () => ChiSquare.GoodnessOfFit([10.0, 10.0], [20.0, 0.0]));
    }

    [Fact]
    public void GoodnessOfFit_refuses_mismatched_lengths_and_a_single_category()
    {
        Assert.Throws<ArgumentException>(
            () => ChiSquare.GoodnessOfFit([10.0, 10.0], [20.0]));
        Assert.Throws<ArgumentException>(() => ChiSquare.GoodnessOfFit([10.0]));
    }

    [Fact]
    public void Yates_applies_to_a_two_by_two_and_to_nothing_else()
    {
        double[][] twoByTwo = [[10.0, 20.0], [30.0, 40.0]];
        double[][] threeByTwo = [[10.0, 20.0], [30.0, 40.0], [15.0, 5.0]];

        Assert.NotEqual(
            ChiSquare.Contingency(twoByTwo, Continuity.Applied).Statistic,
            ChiSquare.Contingency(twoByTwo, Continuity.None).Statistic);

        // Above 2x2 the correction is not defined, so asking for it changes nothing.
        Assert.Equal(
            ChiSquare.Contingency(threeByTwo, Continuity.Applied).Statistic,
            ChiSquare.Contingency(threeByTwo, Continuity.None).Statistic,
            1e-15);
    }

    [Fact]
    public void Yates_clamps_at_zero_rather_than_overshooting_negative()
    {
        // long-comment: pins two scipy-measured numbers rather than one, and the
        // corpus gap that makes doing so necessary is part of what a reviewer needs.
        // No stats_chisquare.json fixture reaches this: every 2x2 case there deviates
        // from independence by more than half a count. Here all four cells sit under
        // 0.5 away, so an unclamped deviation - 0.5 would go negative and, squared,
        // add a spurious positive term -- measured against scipy 1.18.0,
        // correction=True gives exactly 0.0, not the 0.023242630385487566
        // correction=False gives.
        double[][] table = [[10.0, 10.0], [10.0, 11.0]];

        Chi2ContingencyResult corrected = ChiSquare.Contingency(table, Continuity.Applied);
        Chi2ContingencyResult uncorrected = ChiSquare.Contingency(table, Continuity.None);

        Assert.Equal(0.0, corrected.Statistic, 1e-15);
        StatsOracleAsserts.Statistic(0.023242630385487566, uncorrected.Statistic, "no clamp");
    }

    [Fact]
    public void Contingency_refuses_a_ragged_table_and_a_zero_marginal()
    {
        double[][] ragged = [[1.0, 2.0], [3.0]];
        double[][] emptyRow = [[0.0, 0.0], [3.0, 4.0]];

        Assert.Throws<ArgumentException>(() => ChiSquare.Contingency(ragged));
        Assert.Throws<ArgumentException>(() => ChiSquare.Contingency(emptyRow));
    }

    [Fact]
    public void FisherExact_refuses_a_table_that_is_not_two_by_two()
    {
        Assert.Throws<ArgumentException>(() => FisherExact.Test([[1, 2, 3], [4, 5, 6]]));
        Assert.Throws<ArgumentException>(() => FisherExact.Test([[1, 2]]));
    }

    [Fact]
    public void FisherExact_refuses_a_negative_count()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FisherExact.Test([[1, -2], [3, 4]]));
    }

    [Fact]
    public void FisherExact_odds_ratio_is_infinite_when_a_diagonal_is_zero()
    {
        TestResult result = FisherExact.Test([[5, 0], [0, 5]]);

        Assert.True(double.IsPositiveInfinity(result.Statistic));
    }

    [Fact]
    public void FisherExact_refuses_a_table_too_large_to_enumerate()
    {
        // The k-loop is O(total); a table this large would enumerate for however
        // long that takes rather than fail fast, well below where int addition overflows.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FisherExact.Test([[600_000_000, 1], [1, 1]]));
    }
}
