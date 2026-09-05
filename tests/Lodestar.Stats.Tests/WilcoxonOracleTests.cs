using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>Replays <c>tests/oracles/stats_wilcoxon.json</c>.</summary>
public sealed class WilcoxonOracleTests
{
    [Fact]
    public void Every_case_matches_scipy()
    {
        using JsonDocument document = StatsCorpus.Load("stats_wilcoxon.json");
        int replayed = 0;

        foreach (JsonElement c in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            JsonElement args = c.GetProperty("args");

            ZeroMethod zeroMethod = args.GetProperty("zero_method").GetString() switch
            {
                "wilcox" => ZeroMethod.Wilcox,
                "pratt" => ZeroMethod.Pratt,
                "zsplit" => ZeroMethod.ZSplit,
                var other => throw new InvalidDataException($"Unknown zero_method '{other}'."),
            };

            TestResult result = Wilcoxon.Paired(
                StatsCorpus.Doubles(c.GetProperty("x")),
                StatsCorpus.Doubles(c.GetProperty("y")),
                zeroMethod,
                StatsCorpus.Alternative(args),
                args.GetProperty("correction").GetBoolean() ? Continuity.Applied : Continuity.None,
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
