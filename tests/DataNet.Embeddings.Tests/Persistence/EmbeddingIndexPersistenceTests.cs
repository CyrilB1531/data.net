using System.Text;
using DataNet.Embeddings.Search;
using Xunit;

namespace DataNet.Embeddings.Tests.Persistence;

/// <summary>
/// Proves the round trip a persisted index exists for: embed once, query for as
/// long as the file lasts.
/// </summary>
/// <remarks>
/// Score comparisons are bitwise, not tolerant. A tolerance would hide the one
/// failure that matters — vectors that came back almost right and now rank a
/// corpus almost correctly, forever.
/// </remarks>
public sealed class EmbeddingIndexPersistenceTests
{
    [Fact]
    public void The_artifact_declares_its_kind_and_version()
    {
        string json = SaveToString(Sample());

        Assert.StartsWith("{\"$schema\":\"datanet/embedding-index\",\"version\":1,", json, StringComparison.Ordinal);
    }

    [Fact]
    public void The_artifact_carries_the_configuration_and_the_count()
    {
        string json = SaveToString(Sample());

        Assert.Contains("\"dimension\":3", json, StringComparison.Ordinal);
        Assert.Contains("\"normalize\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"count\":2", json, StringComparison.Ordinal);
        Assert.Contains("\"vectors\":\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void An_index_without_ids_writes_no_ids_section()
    {
        Assert.DoesNotContain("\"ids\"", SaveToString(Sample()), StringComparison.Ordinal);
    }

    [Fact]
    public void An_index_with_ids_writes_one_entry_per_vector()
    {
        var index = new EmbeddingIndex(dimension: 3);
        index.Add([1f, 0f, 0f], "first");
        index.Add([0f, 1f, 0f]);

        Assert.Contains("\"ids\":[\"first\",null]", SaveToString(index), StringComparison.Ordinal);
    }

    [Fact]
    public void An_empty_index_still_writes_a_complete_artifact()
    {
        string json = SaveToString(new EmbeddingIndex(dimension: 3));

        Assert.Contains("\"count\":0", json, StringComparison.Ordinal);
        Assert.Contains("\"vectors\":\"\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void A_non_finite_component_is_refused_rather_than_written()
    {
        var index = new EmbeddingIndex(dimension: 2, normalize: false);
        index.Add([1f, float.NaN]);

        using var stream = new MemoryStream();
        InvalidDataException error = Assert.Throws<InvalidDataException>(() => index.Save(stream));

        Assert.Contains("item 0", error.Message, StringComparison.Ordinal);
        Assert.Contains("component 1", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Saving_leaves_the_callers_stream_open()
    {
        using var stream = new MemoryStream();
        Sample().Save(stream);

        // Would throw ObjectDisposedException if Save had disposed it.
        Assert.True(stream.CanWrite);
    }

    [Fact]
    public async Task Saving_asynchronously_writes_the_same_bytes()
    {
        EmbeddingIndex index = Sample();
        using var synchronous = new MemoryStream();
        using var asynchronous = new MemoryStream();

        // SonarLint S6966: the synchronous Save is half of what this test compares.
        // Awaiting SaveAsync in its place would leave the sync writer unexercised
        // and assert that SaveAsync agrees with itself.
#pragma warning disable S6966
        index.Save(synchronous);
#pragma warning restore S6966
        await index.SaveAsync(asynchronous);

        Assert.Equal(synchronous.ToArray(), asynchronous.ToArray());
    }

    [Fact]
    public void Saving_to_a_path_writes_the_same_bytes()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            EmbeddingIndex index = Sample();
            index.Save(path);

            using var stream = new MemoryStream();
            index.Save(stream);
            Assert.Equal(stream.ToArray(), File.ReadAllBytes(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_refused_save_does_not_leave_a_truncated_file_behind()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var index = new EmbeddingIndex(dimension: 2, normalize: false);
        index.Add([1f, float.PositiveInfinity]);

        Assert.Throws<InvalidDataException>(() => index.Save(path));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void A_reloaded_index_scores_bit_for_bit_what_the_original_scored()
    {
        EmbeddingIndex original = Corpus(normalize: true);
        float[] query = [0.3f, -0.7f, 0.2f, 0.9f];

        EmbeddingIndex reloaded = RoundTrip(original);

        IReadOnlyList<SearchResult> before = original.Search(query, k: 4);
        IReadOnlyList<SearchResult> after = reloaded.Search(query, k: 4);
        Assert.Equal(before.Count, after.Count);
        for (int i = 0; i < before.Count; i++)
        {
            Assert.Equal(before[i].Index, after[i].Index);
            Assert.Equal(
                BitConverter.SingleToInt32Bits(before[i].Score),
                BitConverter.SingleToInt32Bits(after[i].Score));
        }
    }

    [Fact]
    public void The_configuration_survives_the_round_trip()
    {
        EmbeddingIndex reloaded = RoundTrip(Corpus(normalize: true));

        Assert.Equal(4, reloaded.Dimension);
        Assert.Equal(3, reloaded.Count);
    }

    [Fact]
    public void An_index_saved_unnormalized_comes_back_unnormalized()
    {
        // A vector of norm 5. If loading renormalized it — or if the flag were lost
        // and the query were normalized — this score could not be 25.
        var original = new EmbeddingIndex(dimension: 2, normalize: false);
        original.Add([3f, 4f]);

        EmbeddingIndex reloaded = RoundTrip(original);

        Assert.Equal(
            BitConverter.SingleToInt32Bits(25f),
            BitConverter.SingleToInt32Bits(reloaded.Search([3f, 4f], k: 1)[0].Score));
    }

    [Fact]
    public void The_same_vectors_saved_under_each_flag_load_differently()
    {
        // [2, 0] rather than [3, 4]: its normalized form and self-dot are both exactly representable,
        // so the assertion can be bitwise without depending on how the accumulation happened to round.
        var normalized = new EmbeddingIndex(dimension: 2, normalize: true);
        normalized.Add([2f, 0f]);
        var raw = new EmbeddingIndex(dimension: 2, normalize: false);
        raw.Add([2f, 0f]);

        float normalizedScore = RoundTrip(normalized).Search([2f, 0f], k: 1)[0].Score;
        float rawScore = RoundTrip(raw).Search([2f, 0f], k: 1)[0].Score;

        Assert.Equal(BitConverter.SingleToInt32Bits(1f), BitConverter.SingleToInt32Bits(normalizedScore));
        Assert.Equal(BitConverter.SingleToInt32Bits(4f), BitConverter.SingleToInt32Bits(rawScore));
    }

    [Fact]
    public void An_empty_index_round_trips()
    {
        EmbeddingIndex reloaded = RoundTrip(new EmbeddingIndex(dimension: 7));

        Assert.Equal(0, reloaded.Count);
        Assert.Equal(7, reloaded.Dimension);
        Assert.False(reloaded.HasIds);
    }

    [Fact]
    public void Ids_round_trip_including_the_awkward_ones()
    {
        var original = new EmbeddingIndex(dimension: 2);
        original.Add([1f, 0f], "documento-café");
        original.Add([0f, 1f], string.Empty);
        original.Add([1f, 1f]);
        original.Add([0f, 0.5f], "日本語");

        EmbeddingIndex reloaded = RoundTrip(original);

        Assert.True(reloaded.HasIds);
        Assert.Equal("documento-café", reloaded.GetId(0));
        Assert.Equal(string.Empty, reloaded.GetId(1));
        Assert.Null(reloaded.GetId(2));
        Assert.Equal("日本語", reloaded.GetId(3));
    }

    [Fact]
    public void An_ids_section_of_nothing_but_nulls_still_reports_it_has_ids()
    {
        // Add never leaves _ids holding only nulls -- only a hand-edited file reaches this shape, and
        // it must still set HasIds: the section was declared, even though every entry in it is absent.
        var index = new EmbeddingIndex(dimension: 2);
        index.Add([1f, 0f]);
        index.Add([0f, 1f]);
        string json = SaveToString(index)
            .Replace("\"count\":2,", "\"count\":2,\"ids\":[null,null],", StringComparison.Ordinal);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        EmbeddingIndex reloaded = EmbeddingIndex.Load(stream);

        Assert.True(reloaded.HasIds);
        Assert.Null(reloaded.GetId(0));
        Assert.Null(reloaded.GetId(1));
    }

    [Fact]
    public void An_index_without_ids_reloads_without_them()
    {
        Assert.False(RoundTrip(Sample()).HasIds);
    }

    [Fact]
    public void More_vectors_can_be_added_to_a_reloaded_index()
    {
        EmbeddingIndex reloaded = RoundTrip(Sample());
        reloaded.Add([0f, 0f, 1f], "added-later");

        Assert.Equal(3, reloaded.Count);
        Assert.Equal("added-later", reloaded.GetId(2));
    }

    [Fact]
    public void Loading_leaves_the_callers_stream_open()
    {
        using var stream = new MemoryStream();
        Sample().Save(stream);
        stream.Position = 0;

        EmbeddingIndex.Load(stream);

        Assert.True(stream.CanRead);
    }

    [Fact]
    public async Task Loading_asynchronously_produces_the_same_index()
    {
        using var stream = new MemoryStream();

        // SonarLint S6966: this call only builds the fixture LoadAsync is tested
        // against. Awaiting SaveAsync in its place would make the setup exercise
        // SaveAsync too, so a failure here could no longer be pinned on LoadAsync.
#pragma warning disable S6966
        Sample().Save(stream);
#pragma warning restore S6966
        stream.Position = 0;

        EmbeddingIndex reloaded = await EmbeddingIndex.LoadAsync(stream);

        Assert.Equal(2, reloaded.Count);
        Assert.Equal(3, reloaded.Dimension);
    }

    [Fact]
    public void Loading_from_a_path_produces_the_same_index()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Sample().Save(path);
            EmbeddingIndex reloaded = EmbeddingIndex.Load(path);

            Assert.Equal(2, reloaded.Count);
            Assert.Equal(3, reloaded.Dimension);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>Two vectors of three dimensions, normalized on insertion.</summary>
    private static EmbeddingIndex Sample()
    {
        var index = new EmbeddingIndex(dimension: 3);
        index.Add([1f, 0f, 0f]);
        index.Add([0.6f, 0.8f, 0f]);
        return index;
    }

    /// <summary>Three vectors of four dimensions, deliberately not unit-length.</summary>
    private static EmbeddingIndex Corpus(bool normalize)
    {
        var index = new EmbeddingIndex(dimension: 4, normalize);
        index.Add([0.1f, 0.2f, 0.3f, 0.4f]);
        index.Add([-0.9f, 0.4f, 0.05f, 0.7f]);
        index.Add([0.33f, 0.33f, 0.33f, 0.33f]);
        return index;
    }

    private static EmbeddingIndex RoundTrip(EmbeddingIndex index)
    {
        using var stream = new MemoryStream();
        index.Save(stream);
        stream.Position = 0;
        return EmbeddingIndex.Load(stream);
    }

    private static string SaveToString(EmbeddingIndex index)
    {
        using var stream = new MemoryStream();
        index.Save(stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
