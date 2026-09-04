using Lodestar.Embeddings.Search;
using Xunit;

namespace Lodestar.Embeddings.Tests.Search;

public sealed class MmrTests
{
    private static readonly float[] Query = [1f, 0f, 0f];

    // sim to query: 1.0, 0.8, 0.6, 0.0
    private static readonly float[][] Candidates =
    [
        [1.00f, 0.00f, 0.00f],
        [0.80f, 0.60f, 0.00f],
        [0.60f, 0.00f, 0.80f],
        [0.00f, 1.00f, 0.00f],
    ];

    [Fact]
    public void Pure_relevance_takes_them_in_query_order()
    {
        Assert.Equal([0, 1, 2], Mmr.Select(Query, Candidates, count: 3, lambda: 1.0));
    }

    [Fact]
    public void Pure_diversity_takes_the_orthogonal_one_second()
    {
        // The order is the selection's, not a re-sort by relevance: candidate 3 is
        // orthogonal to candidate 0 and so is the least redundant available.
        Assert.Equal([0, 3, 2], Mmr.Select(Query, Candidates, count: 3, lambda: 0.0));
    }

    [Fact]
    public void The_first_pick_is_always_the_most_relevant()
    {
        foreach (double lambda in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 })
        {
            Assert.Equal(0, Mmr.Select(Query, Candidates, count: 1, lambda)[0]);
        }
    }

    [Fact]
    public void Asking_for_more_than_there_are_returns_them_all_once()
    {
        int[] chosen = Mmr.Select(Query, Candidates, count: 99);

        Assert.Equal(4, chosen.Length);
        Assert.Equal(4, chosen.Distinct().Count());
    }

    [Fact]
    public void Zero_selects_nothing()
    {
        Assert.Empty(Mmr.Select(Query, Candidates, count: 0));
    }

    [Fact]
    public void A_zero_vector_has_no_cosine_and_is_refused()
    {
        float[][] withZero = [[1f, 0f, 0f], [0f, 0f, 0f]];

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => Mmr.Select(Query, withZero, count: 2));

        Assert.Contains("index 1", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_zero_vector_query_has_no_cosine_and_is_refused()
    {
        float[] zeroQuery = [0f, 0f, 0f];

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => Mmr.Select(zeroQuery, Candidates, count: 2));

        Assert.Contains("query", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("index", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_zero_vector_query_is_refused_even_when_nothing_would_be_selected()
    {
        // Guards against the query check regressing back behind the count == 0
        // short-circuit, which would let this call return [] instead of throwing.
        float[] zeroQuery = [0f, 0f, 0f];

        Assert.Throws<ArgumentException>(() => Mmr.Select(zeroQuery, Candidates, count: 0));
    }

    [Fact]
    public void A_nan_candidate_has_no_cosine_and_is_refused()
    {
        float[][] withNaN = [[1f, 0f, 0f], [float.NaN, 0f, 0f]];

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => Mmr.Select(Query, withNaN, count: 2));

        Assert.Contains("index 1", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_overflowing_candidate_has_no_cosine_and_is_refused()
    {
        float[][] withOverflow = [[1f, 0f, 0f], [2e38f, 2e38f, 0f]];

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => Mmr.Select(Query, withOverflow, count: 2));

        Assert.Contains("index 1", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_overflowing_query_has_no_cosine_and_is_refused()
    {
        float[] overflowingQuery = [2e38f, 2e38f, 0f];

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => Mmr.Select(overflowingQuery, Candidates, count: 2));

        Assert.Contains("query", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("index", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_candidate_of_the_wrong_width_is_refused()
    {
        float[][] ragged = [[1f, 0f, 0f], [1f, 0f]];

        Assert.Throws<ArgumentException>(() => Mmr.Select(Query, ragged, count: 2));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void A_lambda_outside_the_unit_interval_is_refused(double lambda)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Mmr.Select(Query, Candidates, count: 2, lambda));
    }

    [Fact]
    public void A_negative_count_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Mmr.Select(Query, Candidates, count: -1));
    }

    [Fact]
    public void Null_candidates_are_refused()
    {
        Assert.Throws<ArgumentNullException>(() => Mmr.Select(Query, null!, count: 1));
    }

    [Fact]
    public void A_negative_redundancy_is_not_floored_at_zero()
    {
        // keybert/_mmr.py:48 takes the raw max, unfloored: candidate 1 (cosine -1 to 0)
        // beats orthogonal candidate 2 (cosine 0) here; a floored version clamps and picks 2.
        float[] query = [1f, 0f];
        float[][] candidates = [[1f, 0f], [-1f, 0f], [0f, 1f]];

        Assert.Equal([0, 1], Mmr.Select(query, candidates, count: 2, lambda: 0.5));
    }
}
