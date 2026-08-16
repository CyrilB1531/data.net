using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>
/// The definition of mean reciprocal rank, clause by clause. There is no corpus behind
/// this metric — decision 0036 — so these tests are what pins it: a change to any of the
/// three choices below fails here rather than drifting into a release.
/// </summary>
public sealed class ReciprocalRankTests
{
    [Fact]
    public void The_first_relevant_document_is_what_counts_not_the_last()
    {
        // Two relevant documents, at ranks 1 and 3. The definition takes the first.
        double[] relevance = [1.0, 0.0, 1.0, 0.0];
        double[] scores = [0.9, 0.5, 0.4, 0.1];

        Assert.Equal(1.0, ReciprocalRank.Score(relevance, scores, 4), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void The_rank_is_reciprocal_not_linear()
    {
        double[] relevance = [0.0, 0.0, 1.0, 0.0];
        double[] scores = [0.9, 0.5, 0.4, 0.1];

        Assert.Equal(1.0 / 3.0, ReciprocalRank.Score(relevance, scores, 4), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void A_query_with_no_relevant_document_contributes_zero_rather_than_being_dropped()
    {
        // One query answered perfectly, one with nothing relevant at all. Dropping the
        // second would score 1.0; counting it as zero scores 0.5, which is the choice.
        double[] relevance = [1.0, 0.0, 0.0, 0.0, 0.0, 0.0];
        double[] scores = [0.9, 0.5, 0.1, 0.9, 0.5, 0.1];

        Assert.Equal(0.5, ReciprocalRank.Score(relevance, scores, 3), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Relevance_is_a_judgement_so_any_non_zero_value_counts()
    {
        double[] graded = [0.0, 3.0, 0.0];
        double[] binary = [0.0, 1.0, 0.0];
        double[] scores = [0.9, 0.5, 0.1];

        Assert.Equal(ReciprocalRank.Score(binary, scores, 3),
                     ReciprocalRank.Score(graded, scores, 3), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void A_row_of_one_document_is_refused_like_every_other_ranking_metric()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => ReciprocalRank.Score([1.0], [0.5], 1));

        Assert.Contains("more than 1 document", error.Message, StringComparison.Ordinal);
    }
}
