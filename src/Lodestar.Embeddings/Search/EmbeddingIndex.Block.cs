using Lodestar.Internal.Persistence;

namespace Lodestar.Embeddings.Search;

public sealed partial class EmbeddingIndex
{
    /// <summary>Builds an index from a contiguous block of vectors, in one copy.</summary>
    /// <remarks>
    /// For a caller that already holds a whole corpus laid out row after row — a <c>.npy</c>
    /// block, a model's output, a column read out of a store. Replaying it through
    /// <see cref="Add(ReadOnlySpan{float})"/> costs three times the read that produced it
    /// (issue #474); this copies once.
    /// </remarks>
    /// <param name="block">The vectors, row after row, in C order.</param>
    /// <param name="dimension">The embedding dimension; <paramref name="block"/>'s length must be a multiple of it.</param>
    /// <param name="normalization">What is to be done about normalization, and what the index's own flag becomes.</param>
    /// <param name="ids">One id per vector, or <see langword="null"/> for an anonymous index. Copied, never retained.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dimension"/> is below 1, or <paramref name="normalization"/> is not one of the enum's values.</exception>
    /// <exception cref="ArgumentException"><paramref name="block"/>'s length is not a multiple of <paramref name="dimension"/>, or <paramref name="ids"/> holds a number of entries other than the vector count.</exception>
    public static EmbeddingIndex FromBlock(
        ReadOnlySpan<float> block,
        int dimension,
        BlockNormalization normalization,
        IReadOnlyList<string?>? ids = null)
    {
        int count = CheckBlock(block.Length, dimension, normalization, ids);

        // Uninitialized: every element is written by the copy that follows.
        float[] data = Buffers.AllocateUninitialized<float>(block.Length);
        block.CopyTo(data);
        return Seed(data, dimension, count, normalization, CopyIds(ids));
    }

    /// <summary>Validates the three arguments that can disagree, and returns the vector count.</summary>
    private static int CheckBlock(
        int length,
        int dimension,
        BlockNormalization normalization,
        IReadOnlyList<string?>? ids)
    {
        Guard.NotLessThan(dimension, 1);

        // A cast reaches a value the enum does not name, and every branch below would then
        // read it as AlreadyNormalized — a wrong score rather than a refusal.
        if (normalization is not (BlockNormalization.Normalize
            or BlockNormalization.AlreadyNormalized
            or BlockNormalization.Off))
        {
            throw new ArgumentOutOfRangeException(
                nameof(normalization), normalization, "is not a BlockNormalization value.");
        }

        if (length % dimension != 0)
        {
            // "block" as a literal, not nameof: the parameter belongs to the two factories
            // that call this, and theirs is the name a caller has to read.
#pragma warning disable S3928, CA2208
            throw new ArgumentException(
                $"block length {length} is not a multiple of dimension {dimension}.", "block");
#pragma warning restore S3928, CA2208
        }

        int count = length / dimension;
        if (ids is not null && ids.Count != count)
        {
            throw new ArgumentException(
                $"ids holds {ids.Count} entries for {count} vectors.", nameof(ids));
        }
        return count;
    }

    /// <summary>The ids as an array the index can keep, or null for an anonymous index.</summary>
    /// <remarks>
    /// Copied by both factories, adopted by neither. The block is where the bytes are; an id
    /// list is the head, and an <c>IReadOnlyList</c> is not an array to take in the first place.
    /// </remarks>
    private static string?[]? CopyIds(IReadOnlyList<string?>? ids)
    {
        if (ids is null)
        {
            return null;
        }

        var copy = new string?[ids.Count];
        for (int i = 0; i < ids.Count; i++)
        {
            copy[i] = ids[i];
        }
        return copy;
    }

    /// <summary>Puts a validated block into a new index, normalizing it only when asked.</summary>
    /// <remarks>
    /// The one place the fields are set from a block, reached by both factories and by
    /// <c>Restore</c>, so the JSON load path and the block path cannot drift apart.
    /// </remarks>
    private static EmbeddingIndex Seed(
        float[] data,
        int dimension,
        int count,
        BlockNormalization normalization,
        string?[]? ids)
    {
        var index = new EmbeddingIndex(dimension, normalization != BlockNormalization.Off)
        {
            _data = data,
            _length = count * dimension,
            _count = count,
            _ids = ids,
        };

        if (normalization == BlockNormalization.Normalize)
        {
            for (int item = 0; item < count; item++)
            {
                index.NormalizeStored(item * dimension);
            }
        }
        return index;
    }
}
