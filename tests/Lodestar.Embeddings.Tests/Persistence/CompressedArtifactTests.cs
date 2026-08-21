using System.IO.Compression;
using Lodestar.Embeddings.Search;
using Lodestar.Internal.Persistence;
using Xunit;

namespace Lodestar.Embeddings.Tests.Persistence;

/// <summary>
/// Compression is the caller's, on both sides, and these pin that it stays possible.
/// </summary>
/// <remarks>
/// #378 measured deflate at 26.67x the save and 7.19x the load for 26% of the disk, and
/// declined to build it into the format: a caller already has it by wrapping the stream,
/// a decompressing stream being neither seekable nor of known length and so taking the
/// growable read path. That worked by construction rather than by decision, and nothing
/// tested it — a change to the read path could have taken it away in silence.
/// </remarks>
public sealed class CompressedArtifactTests
{
    [Fact]
    public void An_index_saved_through_a_compressing_stream_loads_back_the_same()
    {
        EmbeddingIndex original = Index();
        byte[] compressed = SaveCompressed(original);

        using var source = new MemoryStream(compressed);
        using var decompressing = new GZipStream(source, CompressionMode.Decompress);
        EmbeddingIndex reloaded = EmbeddingIndex.Load(decompressing);

        Assert.Equal(original.Count, reloaded.Count);
        Assert.Equal(original.Dimension, reloaded.Dimension);

        float[] query = new float[original.Dimension];
        query[0] = 1f;
        Assert.Equal(
            original.Search(query, 5).Select(r => r.Index),
            reloaded.Search(query, 5).Select(r => r.Index));
    }

    [Fact]
    public async Task The_asynchronous_load_reads_a_compressing_stream_too()
    {
        EmbeddingIndex original = Index();
        byte[] compressed = SaveCompressed(original);

        using var source = new MemoryStream(compressed);
        using var decompressing = new GZipStream(source, CompressionMode.Decompress);
        EmbeddingIndex reloaded = await EmbeddingIndex.LoadAsync(decompressing);

        Assert.Equal(original.Count, reloaded.Count);
        Assert.Equal(original.Dimension, reloaded.Dimension);
    }

    [Fact]
    public void A_compressed_artifact_round_trips_through_a_file()
    {
        EmbeddingIndex original = Index();
        string path = Path.Combine(Path.GetTempPath(), $"lodestar-{Guid.NewGuid():N}.json.gz");
        try
        {
            using (var file = File.Create(path))
            using (var compressing = new GZipStream(file, CompressionLevel.Optimal))
            {
                original.Save(compressing);
            }

            using var opened = File.OpenRead(path);
            using var decompressing = new GZipStream(opened, CompressionMode.Decompress);
            Assert.Equal(original.Count, EmbeddingIndex.Load(decompressing).Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Compressed_bytes_handed_over_undecompressed_say_which_byte_was_wrong()
    {
        byte[] compressed = SaveCompressed(Index());

        var thrown = Assert.Throws<InvalidDataException>(
            () => EmbeddingIndex.Load(new MemoryStream(compressed)));

        // gzip's first byte. A caller who forgot the wrapper needs to recognise it.
        Assert.Contains("0x1F", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_limits_are_enforced_on_what_the_artifact_expands_to()
    {
        byte[] compressed = SaveCompressed(Index());

        // The guarantee that makes wrapping safe: a small compressed artifact cannot
        // spend an unbounded budget by expanding, because the cap counts bytes read.
        using var source = new MemoryStream(compressed);
        using var decompressing = new GZipStream(source, CompressionMode.Decompress);
        Assert.Throws<InvalidDataException>(
            () => EmbeddingIndex.Load(decompressing, TinyTotal()));
    }

    private static byte[] SaveCompressed(EmbeddingIndex index)
    {
        using var buffer = new MemoryStream();
        using (var compressing = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
        {
            index.Save(compressing);
        }

        return buffer.ToArray();
    }

    private static ArtifactLimits TinyTotal() =>
        new(
            ArtifactLimits.DefaultMaxVocabularySize,
            ArtifactLimits.DefaultMaxTokenLength,
            ArtifactLimits.DefaultMaxJsonDepth,
            maxTotalBytes: 512,
            ArtifactLimits.DefaultMaxArrayLength,
            ArtifactLimits.DefaultMaxSingleBuffer);

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
