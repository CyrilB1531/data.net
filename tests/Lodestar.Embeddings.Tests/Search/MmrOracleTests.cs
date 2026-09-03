using System.Text.Json;
using Lodestar.Embeddings.Search;
using Xunit;

namespace Lodestar.Embeddings.Tests.Search;

/// <summary>Replays every case of <c>mmr.json</c> against keybert's own selections.</summary>
public sealed class MmrOracleTests
{
    public static TheoryData<string> Cases()
    {
        var names = new TheoryData<string>();
        using JsonDocument doc = OracleLoader.Load("mmr.json");
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            names.Add(c.GetProperty("name").GetString()!);
        }
        return names;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Matches_keybert(string name)
    {
        using JsonDocument doc = OracleLoader.Load("mmr.json");
        JsonElement expected = doc.RootElement.GetProperty("cases").EnumerateArray()
            .First(c => c.GetProperty("name").GetString() == name);

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

    private static float[] Row(JsonElement array) =>
        [.. array.EnumerateArray().Select(e => (float)e.GetDouble())];
}
