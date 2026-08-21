using System.Buffers;
using Lodestar.Embeddings.Search;
using Lodestar.Internal.Persistence;
using Xunit;

namespace Lodestar.Embeddings.Tests.Persistence;

/// <summary>
/// The asynchronous read past one array, which #377 gave the synchronous one and not this.
/// </summary>
/// <remarks>
/// The defect these close is a disagreement rather than a gap: the same index loaded through
/// <c>Load</c> and failed through <c>LoadAsync</c>, at the size where it matters and nowhere
/// smaller (#396). So the first thing asserted is that the two overloads agree, and the rest
/// mirror <see cref="SegmentedArtifactTests"/> so the two paths cannot drift apart quietly.
/// </remarks>
public sealed class SegmentedAsyncArtifactTests
{
    [Fact]
    public async Task The_asynchronous_segmented_read_carries_the_same_bytes()
    {
        using var stream = new MemoryStream();
        await Index().SaveAsync(stream);
        byte[] whole = stream.ToArray();

        stream.Position = 0;
        ReadOnlySequence<byte> segments =
            await JsonArtifact.ReadAllSegmentsAsync(stream, Limits(1024), CancellationToken.None);

        Assert.False(segments.IsSingleSegment, "the read should have produced several");
        Assert.Equal(whole, segments.ToArray());
    }

    [Fact]
    public async Task The_two_overloads_agree_on_an_artifact_past_one_array()
    {
        EmbeddingIndex original = Index();
        using var stream = new MemoryStream();
        await original.SaveAsync(stream);
        byte[] artifact = stream.ToArray();

        EmbeddingIndex synchronous = EmbeddingIndex.Load(new MemoryStream(artifact), Limits(1024));
        EmbeddingIndex asynchronous = await EmbeddingIndex.LoadAsync(
            new MemoryStream(artifact), Limits(1024), CancellationToken.None);

        Assert.Equal(synchronous.Count, asynchronous.Count);
        Assert.Equal(synchronous.Dimension, asynchronous.Dimension);

        float[] query = new float[original.Dimension];
        query[0] = 1f;
        Assert.Equal(
            synchronous.Search(query, 5).Select(r => r.Index),
            asynchronous.Search(query, 5).Select(r => r.Index));
    }

    [Fact]
    public async Task The_asynchronous_segmented_read_still_refuses_an_artifact_past_MaxTotalBytes()
    {
        using var stream = new MemoryStream();
        await Index().SaveAsync(stream);
        stream.Position = 0;

        await Assert.ThrowsAsync<InvalidDataException>(
            () => JsonArtifact.ReadAllSegmentsAsync(
                stream, Limits(1024, maxTotalBytes: 2048), CancellationToken.None));
    }

    [Fact]
    public async Task A_cancelled_read_throws_rather_than_parsing_a_partial_chain()
    {
        using var stream = new MemoryStream();
        await Index().SaveAsync(stream);
        stream.Position = 0;

        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => EmbeddingIndex.LoadAsync(stream, Limits(1024), source.Token));
    }

    [Fact]
    public async Task An_artifact_that_fits_one_array_still_takes_the_single_buffer_read()
    {
        EmbeddingIndex original = Index();
        using var stream = new MemoryStream();
        await original.SaveAsync(stream);
        stream.Position = 0;

        // The default ceiling, which nothing a test can afford comes near: the dispatch
        // must fall through to the read that was there before #396.
        EmbeddingIndex reloaded = await EmbeddingIndex.LoadAsync(stream);

        Assert.Equal(original.Count, reloaded.Count);
    }

    private static ArtifactLimits Limits(long singleBuffer, long maxTotalBytes = ArtifactLimits.DefaultMaxTotalBytes) =>
        new(
            ArtifactLimits.DefaultMaxVocabularySize,
            ArtifactLimits.DefaultMaxTokenLength,
            ArtifactLimits.DefaultMaxJsonDepth,
            maxTotalBytes,
            ArtifactLimits.DefaultMaxArrayLength,
            singleBuffer);

    private static EmbeddingIndex Index()
    {
        var index = new EmbeddingIndex(dimension: 8);
        for (int i = 0; i < 200; i++)
        {
            float[] v = new float[8];
            v[i % 8] = 1f;
            index.Add(v, $"id-{i}");
        }

        return index;
    }
}
