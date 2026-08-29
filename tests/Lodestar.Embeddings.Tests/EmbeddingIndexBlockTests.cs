using Lodestar.Embeddings.Search;
using Xunit;

namespace Lodestar.Embeddings.Tests;

public sealed class EmbeddingIndexBlockTests
{
    /// <summary>Scores are compared to four places; the index stores float, not double.</summary>
    private const int Places = 4;

    [Fact]
    public void FromBlock_scores_exactly_as_an_index_built_by_Add()
    {
        float[] block = [3f, 4f, 0f, 1f, 2f, 2f];

        var added = new EmbeddingIndex(dimension: 2);
        for (int item = 0; item < 3; item++)
        {
            added.Add(block.AsSpan(item * 2, 2));
        }

        EmbeddingIndex bulk = EmbeddingIndex.FromBlock(block, 2, BlockNormalization.Normalize);

        // The index exposes no vector accessor, so equal scores for a query is what "the
        // same bits" is observable as; both paths call NormalizeStored, making this exact.
        Assert.Equal(added.Search([1f, 1f], 3), bulk.Search([1f, 1f], 3));
    }

    [Fact]
    public void AlreadyNormalized_stores_the_block_untouched()
    {
        // A block that is not normalized, taken as though it were. The query is normalized
        // to (0.6, 0.8) and the stored vector is not, so the score is |(3,4)| = 5.
        EmbeddingIndex index = EmbeddingIndex.FromBlock(
            [3f, 4f], 2, BlockNormalization.AlreadyNormalized);

        Assert.Equal(5f, index.Search([3f, 4f], 1)[0].Score, Places);
    }

    [Fact]
    public void Off_normalizes_neither_the_block_nor_the_query()
    {
        EmbeddingIndex index = EmbeddingIndex.FromBlock([3f, 4f], 2, BlockNormalization.Off);

        Assert.Equal(25f, index.Search([3f, 4f], 1)[0].Score, Places);
    }

    [Fact]
    public void FromBlock_copies_so_the_caller_can_reuse_its_buffer()
    {
        float[] block = [1f, 0f];
        EmbeddingIndex index = EmbeddingIndex.FromBlock(
            block, 2, BlockNormalization.AlreadyNormalized);

        block[0] = 0f;
        block[1] = 1f;

        Assert.Equal(1f, index.Search([1f, 0f], 1)[0].Score, Places);
    }

    [Fact]
    public void Ids_travel_with_the_block()
    {
        EmbeddingIndex index = EmbeddingIndex.FromBlock(
            [1f, 0f, 0f, 1f], 2, BlockNormalization.AlreadyNormalized, ["east", null]);

        Assert.True(index.HasIds);
        Assert.Equal("east", index.GetId(0));
        Assert.Null(index.GetId(1));
    }

    [Fact]
    public void An_empty_block_makes_an_empty_index()
    {
        EmbeddingIndex index = EmbeddingIndex.FromBlock([], 2, BlockNormalization.Normalize);

        Assert.Equal(0, index.Count);
        Assert.Equal(2, index.Dimension);
    }

    [Fact]
    public void A_block_that_is_not_a_multiple_of_the_dimension_is_refused()
    {
        ArgumentException e = Assert.Throws<ArgumentException>(
            () => EmbeddingIndex.FromBlock([1f, 2f, 3f], 2, BlockNormalization.Off));

        Assert.Contains("not a multiple", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Ids_of_the_wrong_length_are_refused()
    {
        ArgumentException e = Assert.Throws<ArgumentException>(
            () => EmbeddingIndex.FromBlock([1f, 0f], 2, BlockNormalization.Off, ["a", "b"]));

        Assert.Contains("2 entries for 1", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_dimension_below_one_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => EmbeddingIndex.FromBlock([1f, 0f], 0, BlockNormalization.Off));
    }

    [Fact]
    public void A_normalization_outside_the_enum_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => EmbeddingIndex.FromBlock([1f, 0f], 2, (BlockNormalization)99));
    }
}
