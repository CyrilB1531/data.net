using Lodestar.Stats.Internal;
using Xunit;

namespace Lodestar.Stats.Tests.Internal;

/// <summary>
/// The two exact null distributions, checked against counts a reader can verify
/// by hand and against the total each must sum to.
/// </summary>
public sealed class RankDistributionsTests
{
    [Fact]
    public void MannWhitney_counts_sum_to_the_number_of_arrangements()
    {
        // Choosing 4 of 9 positions: C(9,4) = 126 arrangements, and every one of
        // them lands on exactly one U value.
        double[] counts = RankDistributions.MannWhitneyCounts(4, 5);

        Assert.Equal(21, counts.Length);          // U ranges over [0, 20].
        Assert.Equal(126.0, counts.Sum(), 1e-9);
    }

    [Fact]
    public void MannWhitney_counts_are_symmetric_about_the_midpoint()
    {
        double[] counts = RankDistributions.MannWhitneyCounts(3, 5);

        for (int u = 0; u < counts.Length; u++)
        {
            Assert.Equal(counts[u], counts[counts.Length - 1 - u], 1e-9);
        }
    }

    [Fact]
    public void MannWhitney_two_by_two_is_the_hand_computable_case()
    {
        // n = m = 2: U in [0, 4] with counts 1, 1, 2, 1, 1 -- six arrangements.
        double[] counts = RankDistributions.MannWhitneyCounts(2, 2);

        Assert.Equal([1.0, 1.0, 2.0, 1.0, 1.0], counts);
    }

    [Fact]
    public void SignedRank_counts_sum_to_two_to_the_n()
    {
        double[] counts = RankDistributions.SignedRankCounts(6);

        Assert.Equal(22, counts.Length);          // W ranges over [0, 21].
        Assert.Equal(64.0, counts.Sum(), 1e-9);
    }

    [Fact]
    public void SignedRank_three_is_the_hand_computable_case()
    {
        // Ranks 1, 2, 3: the subset sums are 0,1,2,3,3,4,5,6 -- so W = 3 twice.
        double[] counts = RankDistributions.SignedRankCounts(3);

        Assert.Equal([1.0, 1.0, 1.0, 2.0, 1.0, 1.0, 1.0], counts);
    }

    [Fact]
    public void SignedRank_counts_are_symmetric_about_the_midpoint()
    {
        double[] counts = RankDistributions.SignedRankCounts(7);

        for (int w = 0; w < counts.Length; w++)
        {
            Assert.Equal(counts[w], counts[counts.Length - 1 - w], 1e-9);
        }
    }

    [Fact]
    public void SignedRank_of_zero_is_the_single_empty_assignment()
    {
        Assert.Equal([1.0], RankDistributions.SignedRankCounts(0));
    }

    [Fact]
    public void Both_refuse_a_negative_size()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RankDistributions.MannWhitneyCounts(-1, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => RankDistributions.SignedRankCounts(-1));
    }
}
