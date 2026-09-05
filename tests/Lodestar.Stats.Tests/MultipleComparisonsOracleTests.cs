using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>
/// Replays <c>tests/oracles/stats_multiple_comparisons.json</c>: BH and BY
/// against scipy, Bonferroni against its own definition.
/// </summary>
public sealed class MultipleComparisonsOracleTests
{
    [Fact]
    public void Every_case_matches_scipy_and_the_bonferroni_definition()
    {
        using JsonDocument document = StatsCorpus.Load("stats_multiple_comparisons.json");
        int replayed = 0;

        foreach (JsonElement c in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            double[] p = StatsCorpus.Doubles(c.GetProperty("p"));

            StatsOracleAsserts.Vector(
                StatsCorpus.Doubles(c.GetProperty("bonferroni")),
                MultipleComparisons.Bonferroni(p), $"{name} bonferroni");
            StatsOracleAsserts.Vector(
                StatsCorpus.Doubles(c.GetProperty("bh")),
                MultipleComparisons.BenjaminiHochberg(p), $"{name} bh");
            StatsOracleAsserts.Vector(
                StatsCorpus.Doubles(c.GetProperty("by")),
                MultipleComparisons.BenjaminiYekutieli(p), $"{name} by");
            replayed++;
        }

        Assert.Equal(document.RootElement.GetProperty("metadata").GetProperty("count").GetInt32(),
                     replayed);
    }
}
