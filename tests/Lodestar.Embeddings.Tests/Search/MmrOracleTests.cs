using System.Text.Json;
using Lodestar.Embeddings.Search;
using Xunit;

namespace Lodestar.Embeddings.Tests.Search;

/// <summary>Replays every case of <c>mmr.json</c> against keybert's own selections.</summary>
public sealed class MmrOracleTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("mmr.json");

    private static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("cases").EnumerateArray()];

    public static TheoryData<string> CaseNames()
    {
        var names = new TheoryData<string>();
        foreach (JsonElement c in Cases)
        {
            names.Add(c.GetProperty("name").GetString()!);
        }
        return names;
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void Matches_keybert(string name)
    {
        JsonElement expected = Cases.First(c => c.GetProperty("name").GetString() == name);

        float[] query = Row(expected.GetProperty("query"));
        float[][] candidates = [.. expected.GetProperty("candidates").EnumerateArray().Select(Row)];

        int[] chosen = Mmr.Select(
            query,
            candidates,
            expected.GetProperty("count").GetInt32(),
            expected.GetProperty("lambda").GetDouble());

        // The set, not the sequence: keybert re-sorts its picks by relevance.
        Assert.Equal(
            expected.GetProperty("selected").EnumerateArray().Select(e => e.GetInt32()).Order(),
            chosen.Order());
    }

    // The empty-theory silent pass is the one failure the cases can't catch themselves:
    // a corpus lost to a bad load or merge would run zero theories and report success.
    [Fact]
    public void The_corpus_is_the_one_that_was_committed()
    {
        JsonElement metadata = Document.RootElement.GetProperty("metadata");
        Assert.Equal(8, Cases.Count);
        Assert.Equal(8, metadata.GetProperty("count").GetInt32());
        Assert.Equal("keybert", metadata.GetProperty("library").GetString());
        Assert.Equal("0.9.0", metadata.GetProperty("library_version").GetString());
    }

    private static float[] Row(JsonElement array) =>
        [.. array.EnumerateArray().Select(e => (float)e.GetDouble())];
}
