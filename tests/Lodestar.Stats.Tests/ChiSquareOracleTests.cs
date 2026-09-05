using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>Replays <c>tests/oracles/stats_chisquare.json</c>, both calls.</summary>
public sealed class ChiSquareOracleTests
{
    [Fact]
    public void Every_case_matches_scipy()
    {
        using JsonDocument document = StatsCorpus.Load("stats_chisquare.json");
        int replayed = 0;

        foreach (JsonElement c in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            double expectedStatistic = c.GetProperty("statistic").GetDouble();
            double expectedP = c.GetProperty("pvalue").GetDouble();

            if (c.GetProperty("call").GetString() == "chisquare")
            {
                AssertGoodnessOfFit(c, name, expectedStatistic, expectedP);
            }
            else
            {
                AssertContingency(c, name, expectedStatistic, expectedP);
            }

            replayed++;
        }

        Assert.Equal(document.RootElement.GetProperty("metadata").GetProperty("count").GetInt32(),
                     replayed);
    }

    private static void AssertGoodnessOfFit(
        JsonElement c, string name, double expectedStatistic, double expectedP)
    {
        double[] observed = StatsCorpus.Doubles(c.GetProperty("observed"));
        double[] expected = StatsCorpus.Doubles(c.GetProperty("expected_input"));

        TestResult result = expected.Length == 0
            ? ChiSquare.GoodnessOfFit(observed)
            : ChiSquare.GoodnessOfFit(observed, expected);

        StatsOracleAsserts.Statistic(expectedStatistic, result.Statistic, name);
        StatsOracleAsserts.PValue(expectedP, result.PValue, name);
    }

    private static void AssertContingency(
        JsonElement c, string name, double expectedStatistic, double expectedP)
    {
        Chi2ContingencyResult result = ChiSquare.Contingency(
            StatsCorpus.Table(c.GetProperty("table")),
            c.GetProperty("args").GetProperty("correction").GetBoolean()
                ? Continuity.Applied
                : Continuity.None);

        StatsOracleAsserts.Statistic(expectedStatistic, result.Statistic, name);
        StatsOracleAsserts.PValue(expectedP, result.PValue, name);
        Assert.Equal(c.GetProperty("dof").GetInt32(), result.Dof);

        double[][] expectedFreq = StatsCorpus.Table(c.GetProperty("expected_freq"));
        for (int i = 0; i < expectedFreq.Length; i++)
        {
            for (int j = 0; j < expectedFreq[i].Length; j++)
            {
                StatsOracleAsserts.Statistic(
                    expectedFreq[i][j], result.ExpectedFrequencies[i][j],
                    $"{name} expected[{i}][{j}]");
            }
        }
    }
}
