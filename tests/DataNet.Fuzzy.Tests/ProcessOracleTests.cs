using System.Text.Json;
using DataNet.Fuzzy;
using Xunit;

namespace DataNet.Fuzzy.Tests;

public sealed class ProcessOracleTests
{
    private const double Tolerance = 1e-4;

    [Fact]
    public void Extract_matches_rapidfuzz()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "oracles", "process.json");
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = doc.RootElement;

        string[] choices = root.GetProperty("metadata").GetProperty("choices")
            .EnumerateArray().Select(e => e.GetString()!).ToArray();

        foreach (JsonElement c in root.GetProperty("cases").EnumerateArray())
        {
            string query = c.GetProperty("query").GetString()!;
            int limit = c.GetProperty("limit").GetInt32();
            double cutoff = c.GetProperty("cutoff").GetDouble();

            IReadOnlyList<ExtractResult> actual = Process.Extract(query, choices, limit: limit, scoreCutoff: cutoff);
            JsonElement expected = c.GetProperty("results");

            Assert.Equal(expected.GetArrayLength(), actual.Count);
            int r = 0;
            foreach (JsonElement e in expected.EnumerateArray())
            {
                Assert.Equal(e.GetProperty("choice").GetString(), actual[r].Choice);
                Assert.Equal(e.GetProperty("index").GetInt32(), actual[r].Index);
                Assert.True(Math.Abs(e.GetProperty("score").GetDouble() - actual[r].Score) < Tolerance,
                    $"case #{c.GetProperty("id").GetInt32()} rank {r}: score expected {e.GetProperty("score").GetDouble():R}, got {actual[r].Score:R}");
                r++;
            }
        }
    }

    [Fact]
    public void ExtractOne_returns_best()
    {
        string[] choices = ["apple", "banana", "orange"];
        ExtractResult? best = Process.ExtractOne("appel", choices);
        Assert.NotNull(best);
        Assert.Equal("apple", best!.Value.Choice);
    }

    [Fact]
    public void ExtractOne_null_when_all_below_cutoff()
    {
        string[] choices = ["xxxx", "yyyy"];
        Assert.Null(Process.ExtractOne("abcd", choices, scoreCutoff: 50));
    }
}
