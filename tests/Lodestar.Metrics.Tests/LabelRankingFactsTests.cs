using Lodestar.Metrics.Internal;
using Xunit;

namespace Lodestar.Metrics.Tests;

public sealed class LabelRankingFactsTests
{
    [Fact]
    public void The_best_score_ranks_first_and_a_tied_group_takes_its_worst_rank()
    {
        double[] scores = [0.75, 0.5, 1.0];
        int[] ranks = new int[3];
        LabelRanking.MaxRank(scores, ranks);
        Assert.Equal([2, 3, 1], ranks);

        double[] tied = [0.5, 0.5, 0.5];
        LabelRanking.MaxRank(tied, ranks);
        Assert.Equal([3, 3, 3], ranks);
    }

    [Fact]
    public void The_refusals_are_sklearns_with_its_sentences()
    {
        bool[] truth = [true, false];
        double[] scores = [0.7, 0.2];

        // A single label column: refused here, accepted by LabelRankingAveragePrecision.
        ArgumentException single = Assert.Throws<ArgumentException>(
            () => LabelRanking.Validate([true], [0.7], 1, default, singleLabelAllowed: false));
        Assert.Contains("binary format is not supported", single.Message, StringComparison.Ordinal);

        // ...and accepted when the caller allows it.
        LabelRanking.Validate([true], [0.7], 1, default, singleLabelAllowed: true);

        Assert.Throws<ArgumentException>(
            () => LabelRanking.Validate(truth, [0.7], 2, default, singleLabelAllowed: false));
        Assert.Throws<ArgumentException>(
            () => LabelRanking.Validate([], [], 2, default, singleLabelAllowed: false));
        Assert.Throws<ArgumentException>(
            () => LabelRanking.Validate(truth, scores, 2, [1.0, 2.0], singleLabelAllowed: false));
    }
}
