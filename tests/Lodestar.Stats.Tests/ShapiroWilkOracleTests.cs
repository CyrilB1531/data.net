using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>Replays <c>tests/oracles/stats_shapiro.json</c>.</summary>
public sealed class ShapiroWilkOracleTests
{
    [Fact]
    public void Every_case_matches_scipy()
    {
        using JsonDocument document = StatsCorpus.Load("stats_shapiro.json");
        int replayed = 0;

        foreach (JsonElement c in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;

            TestResult result = ShapiroWilk.Test(StatsCorpus.Doubles(c.GetProperty("x")));

            // Statistic uses the absolute tolerance (Royston's approximation both sides);
            // p-value is relative (a normal tail of a fitted transform).
            StatsOracleAsserts.Statistic(
                c.GetProperty("statistic").GetDouble(), result.Statistic, name);
            StatsOracleAsserts.PValue(c.GetProperty("pvalue").GetDouble(), result.PValue, name);
            replayed++;
        }

        Assert.Equal(document.RootElement.GetProperty("metadata").GetProperty("count").GetInt32(),
                     replayed);
    }
}
