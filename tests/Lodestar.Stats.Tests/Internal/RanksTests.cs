using Lodestar.Stats.Internal;
using Xunit;

namespace Lodestar.Stats.Tests.Internal;

/// <summary>
/// Mid-ranks and the tie-correction term. Three families share these, so a bug
/// here would show up as three unrelated corpora disagreeing at once.
/// </summary>
public sealed class RanksTests
{
    [Fact]
    public void Average_ranks_a_strictly_increasing_sample_one_to_n()
    {
        double[] ranks = Ranks.Average([10.0, 20.0, 30.0, 40.0]);

        Assert.Equal([1.0, 2.0, 3.0, 4.0], ranks);
    }

    [Fact]
    public void Average_ranks_are_positional_not_sorted()
    {
        // The result is indexed by the input's own order, not by sorted order:
        // a caller sums the ranks belonging to one of two interleaved samples.
        double[] ranks = Ranks.Average([30.0, 10.0, 20.0]);

        Assert.Equal([3.0, 1.0, 2.0], ranks);
    }

    [Fact]
    public void Average_splits_a_tie_group_at_its_midpoint()
    {
        // Two values tied for ranks 2 and 3 both take 2.5.
        double[] ranks = Ranks.Average([1.0, 5.0, 5.0, 9.0]);

        Assert.Equal([1.0, 2.5, 2.5, 4.0], ranks);
    }

    [Fact]
    public void Average_handles_a_tie_group_of_three_and_one_of_two()
    {
        double[] ranks = Ranks.Average([7.0, 7.0, 7.0, 2.0, 2.0, 9.0]);

        // The two 2.0s take ranks 1 and 2 -> 1.5; the three 7.0s take 3, 4, 5 -> 4.
        Assert.Equal([4.0, 4.0, 4.0, 1.5, 1.5, 6.0], ranks);
    }

    [Fact]
    public void Average_ranks_sum_to_n_times_n_plus_one_over_two_whatever_the_ties()
    {
        double[] ranks = Ranks.Average([3.0, 3.0, 3.0, 3.0, 1.0]);

        Assert.Equal(15.0, ranks.Sum(), 1e-12);
    }

    [Theory]
    [InlineData(new[] { 1.0, 2.0, 3.0 }, 0.0)]
    // One group of two: 2^3 - 2 = 6.
    [InlineData(new[] { 1.0, 5.0, 5.0, 9.0 }, 6.0)]
    // A group of three (24) and a group of two (6).
    [InlineData(new[] { 7.0, 7.0, 7.0, 2.0, 2.0, 9.0 }, 30.0)]
    public void TieCorrection_sums_t_cubed_minus_t_over_the_groups(double[] values, double expected)
    {
        Assert.Equal(expected, Ranks.TieCorrection(values), 1e-12);
    }

    [Fact]
    public void HasTies_answers_the_question_the_exact_branch_asks()
    {
        Assert.False(Ranks.HasTies([1.0, 2.0, 3.0]));
        Assert.True(Ranks.HasTies([1.0, 2.0, 2.0]));
    }

    [Fact]
    public void Average_refuses_an_empty_sample()
    {
        Assert.Throws<ArgumentException>(() => Ranks.Average([]));
    }
}
