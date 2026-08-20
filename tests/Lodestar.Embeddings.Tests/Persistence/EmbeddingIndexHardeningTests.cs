using System.Text;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Search;
using Xunit;

namespace Lodestar.Embeddings.Tests.Persistence;

/// <summary>
/// One test per way a saved index can arrive broken. The baseline is a real
/// artifact, mutated in the one place each case is about, so a test that stops
/// exercising its case fails rather than passing vacuously.
/// </summary>
public sealed class EmbeddingIndexHardeningTests
{
    [Fact]
    public void Input_that_is_not_json_is_rejected()
    {
        Assert.Contains("not well-formed JSON", Load("this is not json").Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Input_that_is_not_an_object_is_rejected()
    {
        Assert.Contains("must be a JSON object", Load("[1, 2, 3]").Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_truncated_artifact_is_rejected()
    {
        string json = Baseline();
        Assert.Contains(
            "not well-formed JSON",
            Load(json.Substring(0, json.Length / 2)).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Trailing_content_is_rejected()
    {
        // Utf8JsonReader itself throws on a second top-level token (verified directly, matching the
        // sibling artifact suite) -- ArtifactIo's own "Trailing content" guard never actually fires here.
        Assert.Contains("not well-formed JSON", Load(Baseline() + "{}").Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Another_artifact_kind_is_rejected()
    {
        string json = Baseline().Replace("datanet/embedding-index", "datanet/tfidf-vectorizer", StringComparison.Ordinal);
        Assert.Contains("datanet/embedding-index", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_format_version_is_rejected()
    {
        string json = Baseline().Replace("\"version\":1", "\"version\":2", StringComparison.Ordinal);
        Assert.Contains("Unsupported", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_header_is_rejected()
    {
        string json = Baseline().Replace("\"$schema\":\"datanet/embedding-index\",", "", StringComparison.Ordinal);
        Assert.Contains("$schema", Load(json).Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("\"dimension\":0")]
    [InlineData("\"dimension\":-3")]
    public void A_dimension_below_one_is_rejected(string replacement)
    {
        string json = Baseline().Replace("\"dimension\":3", replacement, StringComparison.Ordinal);
        Assert.Contains("dimension", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_negative_count_is_rejected()
    {
        string json = Baseline().Replace("\"count\":2", "\"count\":-1", StringComparison.Ordinal);
        Assert.Contains("negative", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_count_the_vector_block_cannot_support_is_rejected()
    {
        // The block holds 2 x 3 floats; claiming a thousand items must not allocate
        // for a thousand items.
        string json = Baseline().Replace("\"count\":2", "\"count\":1000", StringComparison.Ordinal);
        InvalidDataException error = Load(json);

        Assert.Contains("1000", error.Message, StringComparison.Ordinal);
        Assert.Contains("vectors", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_property_is_named()
    {
        string json = Baseline().Replace("\"normalize\":true,", "", StringComparison.Ordinal);
        Assert.Contains("normalize", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_property_is_rejected_rather_than_ignored()
    {
        string json = Baseline().Replace("\"count\":2", "\"shards\":4,\"count\":2", StringComparison.Ordinal);
        Assert.Contains("shards", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_vector_block_that_is_not_base64_is_rejected()
    {
        string json = ReplaceVectors(Baseline(), "not base64 at all!!");
        Assert.Contains("not valid base64", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_vector_block_that_is_not_whole_floats_is_rejected()
    {
        // Five bytes: one float and a quarter.
        string json = ReplaceVectors(Baseline(), Convert.ToBase64String(new byte[5]));
        Assert.Contains("32-bit values", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_vector_block_holding_NaN_is_rejected()
    {
        byte[] raw = new byte[2 * 3 * sizeof(float)];
        BitConverter.GetBytes(float.NaN).CopyTo(raw, 0);
        string json = ReplaceVectors(Baseline(), Convert.ToBase64String(raw));

        Assert.Contains("non-finite", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_non_finite_value_deep_in_a_large_block_is_rejected_and_located()
    {
        // Six floats (the case above) is below the SIMD width, so only this million-value block reaches
        // the vector pass; crafted rather than added via Add, whose normalization would erase the NaN.
        byte[] raw = new byte[3_000 * 384 * sizeof(float)];
        BitConverter.GetBytes(float.NaN).CopyTo(raw, (2_000 * 384 + 7) * sizeof(float));

        using var stream = new MemoryStream();
        RealisticIndex().Save(stream);
        string json = ReplaceVectors(Encoding.UTF8.GetString(stream.ToArray()), Convert.ToBase64String(raw));

        Assert.Contains("item 2000, component 7", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_ids_section_of_the_wrong_length_is_rejected()
    {
        string json = WithIds().Replace("[\"a\",\"b\"]", "[\"a\"]", StringComparison.Ordinal);
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => EmbeddingIndex.Load(new MemoryStream(Encoding.UTF8.GetBytes(json))));

        Assert.Contains("ids", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_ids_entry_of_the_wrong_type_is_rejected()
    {
        string json = WithIds().Replace("[\"a\",\"b\"]", "[\"a\",7]", StringComparison.Ordinal);
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => EmbeddingIndex.Load(new MemoryStream(Encoding.UTF8.GetBytes(json))));

        Assert.Contains("ids", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_id_longer_than_the_limit_is_rejected()
    {
        var index = new EmbeddingIndex(dimension: 3);
        index.Add([1f, 0f, 0f], new string('x', 64));
        using var stream = new MemoryStream();
        index.Save(stream);
        stream.Position = 0;

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => EmbeddingIndex.Load(stream, new ArtifactLoadOptions { MaxTokenLength = 16 }));

        Assert.Contains("MaxTokenLength", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_artifact_larger_than_the_byte_limit_is_rejected()
    {
        using var stream = new MemoryStream();
        Sample().Save(stream);
        stream.Position = 0;

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => EmbeddingIndex.Load(stream, new ArtifactLoadOptions { MaxTotalBytes = 8 }));

        Assert.Contains("MaxTotalBytes", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_memory_overload_reads_what_the_stream_overload_reads()
    {
        using var stream = new MemoryStream();
        RealisticIndex().Save(stream);
        byte[] artifact = stream.ToArray();

        EmbeddingIndex fromStream = EmbeddingIndex.Load(new MemoryStream(artifact));
        EmbeddingIndex fromMemory = EmbeddingIndex.Load(artifact.AsMemory());

        Assert.Equal(fromStream.Count, fromMemory.Count);
        Assert.Equal(fromStream.Dimension, fromMemory.Dimension);
        // Same query, same neighbours: the two paths agree on the vectors, not just
        // on the header they both read first.
        float[] query = new float[fromStream.Dimension];
        query[0] = 1f;
        Assert.Equal(
            fromStream.Search(query, 5).Select(r => r.Index),
            fromMemory.Search(query, 5).Select(r => r.Index));
    }

    [Fact]
    public void The_memory_overload_refuses_an_artifact_past_MaxTotalBytes()
    {
        using var stream = new MemoryStream();
        RealisticIndex().Save(stream);

        // The stream paths catch this while accumulating; with the length known up front
        // the memory path refuses before it parses anything at all.
        Assert.Throws<InvalidDataException>(
            () => EmbeddingIndex.Load(stream.ToArray().AsMemory(), new ArtifactLoadOptions { MaxTotalBytes = 8 }));
    }

    [Fact]
    public void An_index_at_a_realistic_scale_loads_with_the_default_options()
    {
        // 3 000 x 384 (1 152 000 values, about 6 MB) is past the old 1 000 000-element MaxArrayLength
        // that ReadSingles no longer applies to vectors -- the scale the library itself advertises.
        using var stream = new MemoryStream();
        RealisticIndex().Save(stream);
        stream.Position = 0;

        EmbeddingIndex loaded = EmbeddingIndex.Load(stream);

        Assert.Equal(3_000, loaded.Count);
        Assert.Equal(384, loaded.Dimension);
    }

    [Fact]
    public void The_vector_block_is_bounded_by_MaxTotalBytes_before_it_is_allocated()
    {
        // Half of a realistic artifact, not an arbitrary tiny number: proves the vector block itself is
        // caught by MaxTotalBytes, not just that some byte cap exists (An_artifact_larger_than_the_byte_limit_is_rejected).
        using var stream = new MemoryStream();
        RealisticIndex().Save(stream);
        long artifactSize = stream.Length;
        stream.Position = 0;

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => EmbeddingIndex.Load(stream, new ArtifactLoadOptions { MaxTotalBytes = artifactSize / 2 }));

        Assert.Contains("MaxTotalBytes", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_artifact_whose_properties_were_reordered_still_loads()
    {
        // A hand-edited file is a supported input: the writer's order is for
        // reproducibility, not for the reader's convenience.
        string json = "{\"vectors\":" + Extract(Baseline(), "\"vectors\":")
            + ",\"count\":2,\"normalize\":true,\"dimension\":3,"
            + "\"version\":1,\"$schema\":\"datanet/embedding-index\"}";

        EmbeddingIndex index = EmbeddingIndex.Load(new MemoryStream(Encoding.UTF8.GetBytes(json)));

        Assert.Equal(2, index.Count);
        Assert.Equal(3, index.Dimension);
    }

    [Fact]
    public void An_artifact_whose_ids_precede_its_count_still_loads()
    {
        // Save always writes count before ids, so no round trip exercises this ordering -- the reader
        // tolerates it anyway, exercising ReadIds' growth path for ids sized before the count arrives.
        string json = "{\"ids\":[\"a\",\"b\"],\"vectors\":" + Extract(WithIds(), "\"vectors\":")
            + ",\"count\":2,\"normalize\":true,\"dimension\":3,"
            + "\"version\":1,\"$schema\":\"datanet/embedding-index\"}";

        EmbeddingIndex index = EmbeddingIndex.Load(new MemoryStream(Encoding.UTF8.GetBytes(json)));

        Assert.Equal(2, index.Count);
        Assert.Equal("a", index.GetId(0));
        Assert.Equal("b", index.GetId(1));
    }

    /// <summary>Two vectors of three dimensions, no ids.</summary>
    private static EmbeddingIndex Sample()
    {
        var index = new EmbeddingIndex(dimension: 3);
        index.Add([1f, 0f, 0f]);
        index.Add([0.6f, 0.8f, 0f]);
        return index;
    }

    private static string Baseline() => Save(Sample());

    /// <summary>3 000 vectors of 384 dimensions — the shape a sentence-transformer corpus actually has.</summary>
    private static EmbeddingIndex RealisticIndex()
    {
        const int count = 3_000;
        const int dimension = 384;
        var index = new EmbeddingIndex(dimension);
        var vector = new float[dimension];
        for (int item = 0; item < count; item++)
        {
            for (int i = 0; i < dimension; i++)
            {
                vector[i] = (item * dimension + i) % 97 / 97f;
            }
            index.Add(vector);
        }
        return index;
    }

    private static string WithIds()
    {
        var index = new EmbeddingIndex(dimension: 3);
        index.Add([1f, 0f, 0f], "a");
        index.Add([0.6f, 0.8f, 0f], "b");
        return Save(index);
    }

    private static string Save(EmbeddingIndex index)
    {
        using var stream = new MemoryStream();
        index.Save(stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Swaps the base64 payload of the vector block for <paramref name="payload"/>.</summary>
    private static string ReplaceVectors(string json, string payload)
    {
        int start = json.IndexOf("\"vectors\":\"", StringComparison.Ordinal) + "\"vectors\":\"".Length;
        int end = json.IndexOf('"', start);
        return string.Concat(json.AsSpan(0, start), payload.AsSpan(), json.AsSpan(end));
    }

    /// <summary>The JSON value following <paramref name="property"/>, to the closing brace.</summary>
    private static string Extract(string json, string property)
    {
        int start = json.IndexOf(property, StringComparison.Ordinal) + property.Length;
        return json.Substring(start, json.Length - start - 1);
    }

    private static InvalidDataException Load(string json) =>
        Assert.Throws<InvalidDataException>(
            () => EmbeddingIndex.Load(new MemoryStream(Encoding.UTF8.GetBytes(json))));
}
