using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>
/// NDCG and DCG against the frozen corpus, whose fixtures are chosen to separate the
/// two tie treatments rather than merely to exercise them.
/// </summary>
public sealed class RankingTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("ranking.json");

    private static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("cases").EnumerateArray()];

    public static TheoryData<int> Indices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            data.Add(i);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void Matches_sklearn_on_every_case(int index)
    {
        JsonElement c = Cases[index];
        double[] yTrue = MetricsCorpus.Doubles(c, "y_true");
        double[] yScore = MetricsCorpus.Doubles(c, "y_score");
        int n = yTrue.Length;

        Assert.Equal(c.GetProperty("dcg").GetDouble(),
                     Dcg.Score(yTrue, yScore, n), MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("dcg_ignore_ties").GetDouble(),
                     Dcg.Score(yTrue, yScore, n, ignoreTies: true), MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("dcg_log_e").GetDouble(),
                     Dcg.Score(yTrue, yScore, n, logBase: Math.E), MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("dcg_at_2").GetDouble(),
                     Dcg.Score(yTrue, yScore, n, k: 2), MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("ndcg").GetDouble(),
                     Ndcg.Score(yTrue, yScore, n), MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("ndcg_ignore_ties").GetDouble(),
                     Ndcg.Score(yTrue, yScore, n, ignoreTies: true), MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("ndcg_at_2").GetDouble(),
                     Ndcg.Score(yTrue, yScore, n, k: 2), MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("ndcg_at_99").GetDouble(),
                     Ndcg.Score(yTrue, yScore, n, k: 99), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void The_corpus_separates_the_two_tie_treatments()
    {
        // Without this the suite could pass with ties ignored altogether: five of the
        // eight fixtures give the same number either way, and three do not.
        int separating = 0;
        foreach (JsonElement c in Cases)
        {
            if (Math.Abs(c.GetProperty("ndcg").GetDouble() -
                         c.GetProperty("ndcg_ignore_ties").GetDouble()) > MetricsCorpus.Tolerance)
            {
                separating++;
            }
        }

        Assert.Equal(3, separating);
    }

    [Fact]
    public void A_k_past_the_label_count_scores_the_whole_row()
    {
        // Two corpus columns say this, and only as two numbers that happen to agree.
        // Asserted here as the equality it is, so a k that started truncating fails.
        double[] yTrue = [3.0, 2.0, 1.0, 0.0];
        double[] yScore = [0.1, 0.4, 0.5, 0.9];

        Assert.Equal(Ndcg.Score(yTrue, yScore, 4), Ndcg.Score(yTrue, yScore, 4, k: 99));
        Assert.Equal(Dcg.Score(yTrue, yScore, 4), Dcg.Score(yTrue, yScore, 4, k: 99));
    }

    [Fact]
    public void A_row_with_nothing_relevant_scores_zero_rather_than_dividing_by_zero()
    {
        double[] yTrue = [0.0, 0.0, 0.0, 0.0];
        double[] yScore = [0.9, 0.5, 0.4, 0.1];

        Assert.Equal(0.0, Ndcg.Score(yTrue, yScore, 4));
        Assert.Equal(0.0, Dcg.Score(yTrue, yScore, 4));
    }

    [Fact]
    public void Several_rows_are_averaged_and_the_corpus_holds_only_one()
    {
        // Every fixture is a single query, so nothing above reaches the division by the
        // row count. The two rows here are the corpus' perfect and reversed ones.
        double[] yTrue = [3.0, 2.0, 1.0, 0.0, 3.0, 2.0, 1.0, 0.0];
        double[] yScore = [0.9, 0.5, 0.4, 0.1, 0.1, 0.4, 0.5, 0.9];

        Assert.Equal((1.0 + 0.6138273133441086) / 2.0,
                     Ndcg.Score(yTrue, yScore, 4), MetricsCorpus.Tolerance);
        Assert.Equal((4.761859507142915 + 2.922959427791636) / 2.0,
                     Dcg.Score(yTrue, yScore, 4), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void A_row_of_one_document_is_refused_in_sklearns_own_sentence()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => Ndcg.Score([1.0], [0.5], 1));

        Assert.Contains(
            "Computing NDCG is only meaningful when there is more than 1 document.",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Relevance_and_scores_of_different_lengths_are_refused()
    {
        Assert.Throws<ArgumentException>(() => Ndcg.Score([1.0, 0.0], [0.5, 0.4, 0.3], 2));
        Assert.Throws<ArgumentException>(() => Dcg.Score([1.0, 0.0], [0.5, 0.4, 0.3], 2));
    }

    [Fact]
    public void A_length_that_is_not_a_whole_number_of_rows_is_refused()
    {
        Assert.Throws<ArgumentException>(() => Ndcg.Score([1.0, 0.0, 1.0], [0.5, 0.4, 0.3], 2));
    }

    [Fact]
    public void Ties_rank_by_descending_index_past_the_sort_that_stops_being_stable()
    {
        // Array.Sort is an introsort: stable below 16 elements, arbitrary above. Every
        // corpus fixture is 4 or 6 wide, so only a row this size can see the difference.
        const int n = 30;
        double[] relevance = new double[n];
        double[] tied = new double[n];
        double[] ranked = new double[n];
        for (int i = 0; i < n; i++)
        {
            relevance[i] = i;
            tied[i] = 0.5;

            // Scores that make the intended order explicit: index 29 first, index 0 last.
            ranked[i] = i;
        }

        Assert.Equal(Dcg.Score(relevance, ranked, n, ignoreTies: true),
                     Dcg.Score(relevance, tied, n, ignoreTies: true), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void A_negative_relevance_is_refused_by_ndcg_and_accepted_by_dcg()
    {
        // ndcg_score refuses it and dcg_score does not, so the ratio cannot go negative
        // where the page promises [0, 1]; unguarded this row scored -1.
        double[] yTrue = [1.0, -1.0];
        double[] yScore = [0.1, 0.9];

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => Ndcg.Score(yTrue, yScore, 2));
        Assert.Contains(
            "ndcg_score should not be used on negative y_true values.",
            error.Message,
            StringComparison.Ordinal);

        Assert.True(Dcg.Score(yTrue, yScore, 2) < 0.0);
    }

    [Fact]
    public void A_k_below_one_is_refused_rather_than_zeroing_every_discount()
    {
        double[] yTrue = [3.0, 2.0, 1.0, 0.0];
        double[] yScore = [0.9, 0.5, 0.4, 0.1];

        Assert.Throws<ArgumentOutOfRangeException>(() => Ndcg.Score(yTrue, yScore, 4, k: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Dcg.Score(yTrue, yScore, 4, k: -1));
    }

    [Fact]
    public void A_logBase_outside_scikit_learns_range_is_refused_rather_than_returning_NaN()
    {
        double[] yTrue = [3.0, 2.0, 1.0, 0.0];
        double[] yScore = [0.9, 0.5, 0.4, 0.1];

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Dcg.Score(yTrue, yScore, 4, logBase: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Dcg.Score(yTrue, yScore, 4, logBase: -2.0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Dcg.Score(yTrue, yScore, 4, logBase: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Dcg.Score(yTrue, yScore, 4, logBase: double.PositiveInfinity));

        // The interval is open at both ends but wide: a base below 1 is accepted, and
        // takes the score negative. Measured, -4.761859507142915 at logBase 0.5.
        Assert.Equal(-4.761859507142915,
            Dcg.Score(yTrue, yScore, 4, logBase: 0.5), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void A_logBase_of_one_is_accepted_and_scores_zero_as_the_reference_does()
    {
        // Inside scikit-learn's (0, inf), so not a refusal: log(1) is 0, every discount
        // is 0, and dcg_score returns 0.0 there too — measured, with a RuntimeWarning.
        double[] yTrue = [3.0, 2.0, 1.0, 0.0];
        double[] yScore = [0.9, 0.5, 0.4, 0.1];

        Assert.Equal(0.0, Dcg.Score(yTrue, yScore, 4, logBase: 1.0), MetricsCorpus.Tolerance);
    }
}
