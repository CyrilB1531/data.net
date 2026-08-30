using Lodestar.Embeddings.Search;
using Xunit;

namespace Lodestar.Embeddings.Tests.Persistence;

/// <summary>
/// A payload buffer reused between loads must let the second load observe nothing of
/// the first.
/// </summary>
/// <remarks>
/// A rented array is at least as long as asked for, and its tail holds whatever the
/// previous renter left there. Slicing to the byte count actually read is what keeps
/// that tail out of the parser, so these fail if a slice is ever taken from the
/// buffer's length instead of from the count.
/// </remarks>
public sealed class PooledPayloadTests
{
    [Fact]
    public void A_second_load_observes_nothing_of_the_first()
    {
        // B is deliberately the shorter artifact: that is the case where a slice taken
        // from the buffer rather than from the byte count exposes A's tail.
        EmbeddingIndex a = Build(count: 400, dimension: 16, seed: 1);
        EmbeddingIndex b = Build(count: 40, dimension: 16, seed: 2);

        EmbeddingIndex loadedA = RoundTrip(a);
        EmbeddingIndex loadedB = RoundTrip(b);

        Assert.Equal(b.Count, loadedB.Count);
        for (int i = 0; i < loadedB.Count; i++)
        {
            Assert.Equal(b.GetId(i), loadedB.GetId(i));
        }

        // A's tail would show up as B's search answering with A's neighbours.
        float[] query = Query(16, seed: 7);
        Assert.Equal(b.Search(query, k: 3), loadedB.Search(query, k: 3));
        GC.KeepAlive(loadedA);
    }

    [Fact]
    public void A_short_artifact_after_a_long_one_carries_no_trailing_bytes()
    {
        // The same hazard read from the other end: the long artifact is loaded first so
        // the buffer behind the short one is certain to be longer than the short one.
        EmbeddingIndex longer = Build(count: 900, dimension: 8, seed: 3);
        EmbeddingIndex shorter = Build(count: 3, dimension: 8, seed: 4);

        GC.KeepAlive(RoundTrip(longer));
        EmbeddingIndex loaded = RoundTrip(shorter);

        Assert.Equal(3, loaded.Count);
        for (int i = 0; i < loaded.Count; i++)
        {
            Assert.Equal(shorter.GetId(i), loaded.GetId(i));
            Assert.Equal(shorter.Search(Query(8, seed: i), k: 1), loaded.Search(Query(8, seed: i), k: 1));
        }
    }

    private static EmbeddingIndex RoundTrip(EmbeddingIndex index)
    {
        using var stream = new MemoryStream();
        index.Save(stream);
        stream.Position = 0;
        return EmbeddingIndex.Load(stream);
    }

    private static EmbeddingIndex Build(int count, int dimension, int seed)
    {
        var index = new EmbeddingIndex(dimension);
        for (int item = 0; item < count; item++)
        {
            index.Add(Query(dimension, seed * 1000 + item), $"id-{seed}-{item}");
        }
        return index;
    }

    /// <summary>A deterministic vector; the values matter only in being reproducible.</summary>
    private static float[] Query(int dimension, int seed)
    {
        var vector = new float[dimension];
        uint state = (uint)seed + 1u;
        for (int i = 0; i < dimension; i++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            vector[i] = (state & 0xFFFF) / 65535f;
        }
        return vector;
    }
}
