using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>Replays the two k-sample corpora, which share a fixture shape.</summary>
public sealed class GroupTestOracleTests
{
    [Theory]
    [InlineData("stats_anova.json")]
    [InlineData("stats_kruskal.json")]
    public void Every_case_matches_scipy(string fileName)
    {
        using JsonDocument document = StatsCorpus.Load(fileName);
        int replayed = 0;

        foreach (JsonElement c in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = $"{fileName}: {c.GetProperty("name").GetString()}";
            double[][] groups = StatsCorpus.Table(c.GetProperty("groups"));

            TestResult result = c.GetProperty("call").GetString() == "f_oneway"
                ? OneWayAnova.Test(groups)
                : KruskalWallis.Test(groups);

            StatsOracleAsserts.Statistic(
                c.GetProperty("statistic").GetDouble(), result.Statistic, name);
            StatsOracleAsserts.PValue(c.GetProperty("pvalue").GetDouble(), result.PValue, name);
            replayed++;
        }

        Assert.Equal(document.RootElement.GetProperty("metadata").GetProperty("count").GetInt32(),
                     replayed);
    }
}
