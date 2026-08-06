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

    /// <summary>Two vectors of three dimensions, normalized on insertion.</summary>
    private static EmbeddingIndex Sample()
    {
        var index = new EmbeddingIndex(dimension: 3);
        index.Add([1f, 0f, 0f]);
        index.Add([0.6f, 0.8f, 0f]);
        return index;
    }

    private static string SaveToString(EmbeddingIndex index)
    {
        using var stream = new MemoryStream();
        index.Save(stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
