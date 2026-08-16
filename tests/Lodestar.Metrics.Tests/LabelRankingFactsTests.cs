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

    [Fact]
    public void Weighted_throws_numpys_own_sentence_on_a_zero_weight_sum()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => LabelRanking.Weighted([1.0, 2.0, 3.0], [0.0, 0.0, 0.0]));
        Assert.Contains(
            "Weights sum to zero, can't be normalized.", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Weighted_is_the_plain_mean_unweighted_and_the_weighted_mean_otherwise()
    {
        double[] values = [1.0, 2.0, 3.0];
        Assert.Equal(2.0, LabelRanking.Weighted(values, default));
        Assert.Equal(2.0, LabelRanking.Weighted(values, [1.0, 0.0, 1.0]));
        Assert.Equal(3.0, LabelRanking.Weighted(values, [0.0, 0.0, 1.0]));
    }

    [Fact]
    public void RelevantCount_counts_the_true_entries_of_a_row()
    {
        Assert.Equal(2, LabelRanking.RelevantCount([true, false, true, false]));
        Assert.Equal(0, LabelRanking.RelevantCount([false, false, false]));
    }

    [Fact]
    public void A_row_with_nothing_relevant_covers_zero_labels_not_all_of_them()
    {
        bool[] truth = [false, false, false, true, false, false];
        double[] scores = [0.7, 0.2, 0.1, 0.7, 0.2, 0.1];

        // The scoring row covers 1 label, the empty row covers 0: the mean is 0.5.
        // Treating the empty row as fully covered would give 2.0 and look reasonable.
        Assert.Equal(0.5, CoverageError.Score(truth, scores, 3), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void A_tie_between_a_relevant_and_an_irrelevant_label_counts_as_an_error()
    {
        // Two relevant, one irrelevant, every score equal: both pairs are wrong, so 1.
        Assert.Equal(1.0,
            LabelRankingLoss.Score([true, true, false], [0.5, 0.5, 0.5], 3),
            MetricsCorpus.Tolerance);

        // The same row with the irrelevant label scored strictly lower: nothing is wrong.
        Assert.Equal(0.0,
            LabelRankingLoss.Score([true, true, false], [0.5, 0.5, 0.1], 3),
            MetricsCorpus.Tolerance);
    }
}
