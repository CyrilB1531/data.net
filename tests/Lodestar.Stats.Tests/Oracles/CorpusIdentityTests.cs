using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>
/// Every corpus declares its library, its version and its case count, and the
/// count matches what the file holds.
/// </summary>
/// <remarks>
/// Without this an empty <c>cases</c> array passes as green: each family's
/// replay would iterate nothing and assert nothing. The shape #313 established.
/// </remarks>
public sealed class CorpusIdentityTests
{
    public static TheoryData<string, string> Corpora => new()
    {
        { "stats_ttest.json", "ttest" },
        { "stats_mannwhitney.json", "mannwhitney" },
        { "stats_wilcoxon.json", "wilcoxon" },
        { "stats_chisquare.json", "chisquare" },
        { "stats_fisher.json", "fisher" },
        { "stats_ks.json", "ks" },
        { "stats_anova.json", "anova" },
        { "stats_kruskal.json", "kruskal" },
        { "stats_shapiro.json", "shapiro" },
        { "stats_multiple_comparisons.json", "multiple_comparisons" },
    };

    [Theory]
    [MemberData(nameof(Corpora))]
    public void Corpus_declares_scipy_its_version_its_family_and_a_matching_count(
        string fileName, string family)
    {
        using JsonDocument document = StatsCorpus.Load(fileName);
        JsonElement metadata = document.RootElement.GetProperty("metadata");

        Assert.Equal("scipy", metadata.GetProperty("library").GetString());
        Assert.Equal("1.18.0", metadata.GetProperty("version").GetString());
        Assert.Equal(family, metadata.GetProperty("family").GetString());

        int declared = metadata.GetProperty("count").GetInt32();
        int actual = document.RootElement.GetProperty("cases").GetArrayLength();

        Assert.Equal(declared, actual);
        Assert.True(actual > 0, $"{fileName} holds no cases.");
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void Every_case_records_the_arguments_it_was_generated_with(
        string fileName, string family)
    {
        using JsonDocument document = StatsCorpus.Load(fileName);

        foreach (JsonElement testCase in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            // The arguments are data, not a default the replay may assume: this
            // is what makes a scipy upgrade that moves a default fail loudly.
            Assert.True(testCase.TryGetProperty("args", out _), $"{family}: a case has no args.");
            Assert.True(testCase.TryGetProperty("call", out _), $"{family}: a case has no call.");
            Assert.False(
                string.IsNullOrWhiteSpace(testCase.GetProperty("name").GetString()),
                $"{family}: a case has no name.");
        }
    }
}
