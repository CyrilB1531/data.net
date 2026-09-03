using System.Text.Json;
using Lodestar.Text.Keywords;
using Xunit;

namespace Lodestar.Text.Tests.Keywords;

/// <summary>Replays every case of <c>keywords_rake.json</c> against rake-nltk's own numbers.</summary>
public sealed class RakeOracleTests
{
    public static TheoryData<string> Cases()
    {
        var names = new TheoryData<string>();
        using JsonDocument doc = OracleLoader.Load("keywords_rake.json");
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            names.Add(c.GetProperty("name").GetString()!);
        }
        return names;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Matches_rake_nltk(string name)
    {
        using JsonDocument doc = OracleLoader.Load("keywords_rake.json");
        JsonElement metadata = doc.RootElement.GetProperty("metadata");
        string[] stop = metadata.GetProperty("stop_words").EnumerateArray().Select(e => e.GetString()!).ToArray();
        string pattern = metadata.GetProperty("token_pattern").GetString()!;

        JsonElement expected = doc.RootElement.GetProperty("cases").EnumerateArray()
            .First(c => c.GetProperty("name").GetString() == name);

        var options = new RakeOptions
        {
            StopWords = stop,
            TokenPattern = pattern,
            Metric = Enum.Parse<RakeMetric>(expected.GetProperty("metric").GetString()!),
            MinLength = expected.GetProperty("min_length").GetInt32(),
            MaxLength = expected.GetProperty("max_length").GetInt32(),
            IncludeRepeatedPhrases = expected.GetProperty("include_repeated_phrases").GetBoolean(),
        };

        IReadOnlyList<KeywordMatch> actual = new Rake(options).Extract(expected.GetProperty("text").GetString()!);
        JsonElement[] rows = [.. expected.GetProperty("expected").EnumerateArray()];

        Assert.Equal(rows.Length, actual.Count);

        // Compared as a multiset: rake-nltk's order among equal scores is its sort's,
        // and a tie-break neither implementation promises is not a behaviour to match.
        Assert.Equal(
            rows.Select(r => (r.GetProperty("phrase").GetString()!, r.GetProperty("score").GetDouble()))
                .OrderBy(p => p.Item1, StringComparer.Ordinal).ThenBy(p => p.Item2),
            actual.Select(m => (m.Phrase, m.Score))
                .OrderBy(p => p.Phrase, StringComparer.Ordinal).ThenBy(p => p.Score),
            new PhraseScoreComparer());
    }

    private sealed class PhraseScoreComparer : IEqualityComparer<(string Phrase, double Score)>
    {
        public bool Equals((string Phrase, double Score) a, (string Phrase, double Score) b) =>
            string.Equals(a.Phrase, b.Phrase, StringComparison.Ordinal) && Math.Abs(a.Score - b.Score) <= 1e-9;

        public int GetHashCode((string Phrase, double Score) value) => value.Phrase.GetHashCode(StringComparison.Ordinal);
    }
}
