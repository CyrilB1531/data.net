using System.Buffers;
using Lodestar.Embeddings.Search;
using Lodestar.Internal.Persistence;
using Xunit;

namespace Lodestar.Embeddings.Tests.Persistence;

/// <summary>
/// The read path taken when an artifact does not fit one array, exercised at kilobytes.
/// </summary>
/// <remarks>
/// The ceiling is the CLR's <see cref="byte"/>-array limit, so the honest test would save
/// an index past two gibibytes — which nobody would run, which is how the path would ship
/// untested. <see cref="ArtifactLimits.MaxSingleBuffer"/> is settable for that reason
/// alone: the same branch, reached at a size a test suite can afford (#377).
/// </remarks>
public sealed class SegmentedArtifactTests
{
    [Fact]
    public void An_artifact_read_in_segments_parses_to_the_same_index()
    {
        EmbeddingIndex original = Index();
        using var stream = new MemoryStream();
        original.Save(stream);
        stream.Position = 0;

        // Small enough that the artifact spans many segments, which is the point.
        ReadOnlySequence<byte> segments = JsonArtifact.ReadAllSegments(stream, Limits(1024));

        Assert.True(segments.Length > 1024, "the artifact should not fit one segment");
        Assert.False(segments.IsSingleSegment, "the read should have produced several");
    }

    [Fact]
    public void The_segmented_read_carries_the_same_bytes_as_the_single_buffer_one()
    {
        using var stream = new MemoryStream();
        Index().Save(stream);
        byte[] whole = stream.ToArray();

        stream.Position = 0;
        ReadOnlySequence<byte> segments = JsonArtifact.ReadAllSegments(stream, Limits(1024));

        Assert.Equal(whole, segments.ToArray());
    }

    [Fact]
    public void An_index_loads_through_the_segmented_path_with_the_same_answers()
    {
        EmbeddingIndex original = Index();
        using var stream = new MemoryStream();
        original.Save(stream);
        byte[] artifact = stream.ToArray();

        EmbeddingIndex viaOneBuffer = EmbeddingIndex.Load(new MemoryStream(artifact));
        EmbeddingIndex viaSegments = LoadSegmented(artifact, 1024);

        Assert.Equal(viaOneBuffer.Count, viaSegments.Count);
        Assert.Equal(viaOneBuffer.Dimension, viaSegments.Dimension);

        float[] query = new float[viaOneBuffer.Dimension];
        query[0] = 1f;
        Assert.Equal(
            viaOneBuffer.Search(query, 5).Select(r => r.Index),
            viaSegments.Search(query, 5).Select(r => r.Index));
    }

    [Fact]
    public void The_segmented_read_still_refuses_an_artifact_past_MaxTotalBytes()
    {
        using var stream = new MemoryStream();
        Index().Save(stream);
        stream.Position = 0;

        Assert.Throws<InvalidDataException>(
            () => JsonArtifact.ReadAllSegments(stream, Limits(1024, maxTotalBytes: 2048)));
    }

    private static EmbeddingIndex LoadSegmented(byte[] artifact, long singleBuffer)
    {
        using var stream = new MemoryStream(artifact);
        return EmbeddingIndex.Load(stream, Limits(singleBuffer));
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
