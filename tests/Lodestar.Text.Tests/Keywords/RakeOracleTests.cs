using Lodestar.Text.Keywords;
using Lodestar.Text.Tests.Oracles;
using Xunit;

namespace Lodestar.Text.Tests.Keywords;

/// <summary>Replays every case of <c>keywords_rake.json</c> against rake-nltk's own numbers.</summary>
public sealed class RakeOracleTests
{
    private static readonly OracleFile<RakeCase> Corpus =
        OracleCorpus.Load<RakeCase>("keywords_rake.json");

    public static TheoryData<int> Indices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Corpus.Cases.Count; i++)
        {
            data.Add(i);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void Matches_rake_nltk(int index)
    {
        RakeCase expected = Corpus.Cases[index];
        var options = new RakeOptions
        {
            StopWords = Corpus.Metadata.StopWords,
            TokenPattern = Corpus.Metadata.TokenPattern,
            Metric = Enum.Parse<RakeMetric>(expected.Metric),
            MinLength = expected.MinLength,
            MaxLength = expected.MaxLength,
            IncludeRepeatedPhrases = expected.IncludeRepeatedPhrases,
        };

        IReadOnlyList<KeywordMatch> actual = new Rake(options).Extract(expected.Text);

        Assert.Equal(expected.Expected.Count, actual.Count);

        // Compared positionally, not as a multiset: rake-nltk's tie order is specified
        // (rake_nltk/rake.py:241) and only the score keeps a tolerance.
        Assert.Equal(
            expected.Expected.Select(r => (r.Phrase, r.Score)),
            actual.Select(m => (m.Phrase, m.Score)),
            new ApproximatePhraseScoreComparer());
    }

    [Fact]
    public void The_corpus_is_the_one_that_was_committed()
    {
        Assert.Equal(30, Corpus.Cases.Count);
        Assert.Equal("rake-nltk", Corpus.Metadata.Library);
        Assert.Equal("1.0.6", Corpus.Metadata.LibraryVersion);
    }
}
