using System.Text.Json;
using DataNet.Fuzzy;
using Xunit;

namespace DataNet.Fuzzy.Tests;

public sealed class FuzzOracleTests
{
    private const double Tolerance = 1e-4;

    private static JsonDocument Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "oracles", "fuzz.json");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    [Fact]
    public void All_ratios_match_rapidfuzz()
    {
        using JsonDocument doc = Load();
        var failures = new List<string>();

        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string a = c.GetProperty("a").GetString()!;
            string b = c.GetProperty("b").GetString()!;

            Check(failures, c, "ratio", c.GetProperty("ratio").GetDouble(), Fuzz.Ratio(a, b));
            Check(failures, c, "partial_ratio", c.GetProperty("partial_ratio").GetDouble(), Fuzz.PartialRatio(a, b));
            Check(failures, c, "token_sort_ratio", c.GetProperty("token_sort_ratio").GetDouble(), Fuzz.TokenSortRatio(a, b));
            Check(failures, c, "token_set_ratio", c.GetProperty("token_set_ratio").GetDouble(), Fuzz.TokenSetRatio(a, b));
            Check(failures, c, "wratio", c.GetProperty("wratio").GetDouble(), Fuzz.WRatio(a, b));
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Theory]
    [InlineData("hello world", "world hello", 100.0)]   // token_sort ignores order
    [InlineData("new york mets", "new york mets", 100.0)]
    public void TokenSort_known(string a, string b, double expected)
    {
        Assert.Equal(expected, Fuzz.TokenSortRatio(a, b), 4);
    }

    [Fact]
    public void Ratio_is_indel_not_levenshtein()
    {
        // fuzz.ratio uses Indel similarity: "abc"/"abcd" -> 2*3/(3+4)*100.
        Assert.Equal(200.0 * 3 / 7, Fuzz.Ratio("abc", "abcd"), 4);
    }

    private static void Check(List<string> failures, JsonElement c, string name, double expected, double actual)
    {
        if (Math.Abs(expected - actual) > Tolerance)
        {
            string a = c.GetProperty("a").GetString()!;
            string b = c.GetProperty("b").GetString()!;
            failures.Add($"[#{c.GetProperty("id").GetInt32()}] {name}(\"{a}\", \"{b}\"): expected {expected:R}, got {actual:R}");
        }
    }
}
