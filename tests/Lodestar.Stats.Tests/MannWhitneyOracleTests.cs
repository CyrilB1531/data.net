using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>Replays <c>tests/oracles/stats_mannwhitney.json</c>.</summary>
public sealed class MannWhitneyOracleTests
{
    [Fact]
    public void Every_case_matches_scipy()
    {
        using JsonDocument document = StatsCorpus.Load("stats_mannwhitney.json");
        int replayed = 0;

        foreach (JsonElement c in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            JsonElement args = c.GetProperty("args");

            TestResult result = MannWhitney.Test(
                StatsCorpus.Doubles(c.GetProperty("a")),
                StatsCorpus.Doubles(c.GetProperty("b")),
                StatsCorpus.Alternative(args),
                args.GetProperty("use_continuity").GetBoolean()
                    ? Continuity.Applied
                    : Continuity.None,
                StatsCorpus.Method(args));

            StatsOracleAsserts.Statistic(
                c.GetProperty("statistic").GetDouble(), result.Statistic, name);
            StatsOracleAsserts.PValue(c.GetProperty("pvalue").GetDouble(), result.PValue, name);
            replayed++;
        }

        Assert.Equal(document.RootElement.GetProperty("metadata").GetProperty("count").GetInt32(),
                     replayed);
    }
}
