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

            // The statistic is Royston's rational approximation on both sides, so
            // it agrees to the repository's absolute tolerance; the p-value is a
            // normal tail of a fitted transform, which is why it is relative.
            StatsOracleAsserts.Statistic(
                c.GetProperty("statistic").GetDouble(), result.Statistic, name);
            StatsOracleAsserts.PValue(c.GetProperty("pvalue").GetDouble(), result.PValue, name);
            replayed++;
        }

        Assert.Equal(document.RootElement.GetProperty("metadata").GetProperty("count").GetInt32(),
                     replayed);
    }
}
