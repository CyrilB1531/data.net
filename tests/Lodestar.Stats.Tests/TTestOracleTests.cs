using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>
/// Replays <c>tests/oracles/stats_ttest.json</c>. Each case names the scipy call
/// and the arguments it was generated with, and the replay reads them rather
/// than assuming a default.
/// </summary>
public sealed class TTestOracleTests
{
    [Fact]
    public void Every_case_matches_scipy()
    {
        using JsonDocument document = StatsCorpus.Load("stats_ttest.json");
        int replayed = 0;

        foreach (JsonElement c in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            string call = c.GetProperty("call").GetString()!;
            JsonElement args = c.GetProperty("args");
            double[] a = StatsCorpus.Doubles(c.GetProperty("a"));
            double[] b = StatsCorpus.Doubles(c.GetProperty("b"));
            Alternative alternative = StatsCorpus.Alternative(args);

            TTestResult result = call switch
            {
                "ttest_ind" => TTest.Independent(
                    a, b, alternative,
                    args.GetProperty("equal_var").GetBoolean() ? Variance.Equal : Variance.Welch),
                "ttest_rel" => TTest.Paired(a, b, alternative),
                "ttest_1samp" => TTest.OneSample(
                    a, args.GetProperty("popmean").GetDouble(), alternative),
                _ => throw new InvalidDataException($"Unknown call '{call}'."),
            };

            StatsOracleAsserts.Statistic(
                StatsCorpus.Number(c.GetProperty("statistic")), result.Statistic, name);
            StatsOracleAsserts.PValue(c.GetProperty("pvalue").GetDouble(), result.PValue, name);
            StatsOracleAsserts.Statistic(c.GetProperty("df").GetDouble(), result.Df, $"{name} df");

            (double low, double high) = result.ConfidenceInterval(0.95);
            StatsOracleAsserts.Statistic(
                StatsCorpus.Number(c.GetProperty("ci_low")), low, $"{name} ci low");
            StatsOracleAsserts.Statistic(
                StatsCorpus.Number(c.GetProperty("ci_high")), high, $"{name} ci high");

            replayed++;
        }

        // The corpus is not empty, and the loop did not skip it.
        Assert.Equal(document.RootElement.GetProperty("metadata").GetProperty("count").GetInt32(),
                     replayed);
    }
}
