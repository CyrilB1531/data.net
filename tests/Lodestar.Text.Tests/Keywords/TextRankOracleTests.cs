using Lodestar.Text.Keywords;
using Lodestar.Text.Tests.Oracles;
using Xunit;

namespace Lodestar.Text.Tests.Keywords;

/// <summary>Replays every case of <c>keywords_textrank.json</c> against summa's own numbers.</summary>
public sealed class TextRankOracleTests
{
    private static readonly OracleFile<TextRankCase> Corpus =
        OracleCorpus.Load<TextRankCase>("keywords_textrank.json");

    public static TheoryData<int> Cases()
    {
        var indices = new TheoryData<int>();
        for (int i = 0; i < Corpus.Cases.Count; i++)
        {
            indices.Add(i);
        }

        return indices;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Matches_summa(int index)
    {
        TextRankCase expected = Corpus.Cases[index];
        var options = new TextRankOptions
        {
            StopWords = Corpus.Metadata.StopWords,
            Words = expected.Words,
        };

        IReadOnlyList<KeywordMatch> actual = new TextRank(options).Extract(expected.Text);

        Assert.Equal(expected.Expected.Count, actual.Count);

        // Compared as a multiset: components tied below either algorithm's convergence
        // tolerance can land in either order, a tie-break neither implementation promises.
        Assert.Equal(
            expected.Expected.Select(p => (p.Phrase, p.Score))
                .OrderBy(p => p.Phrase, StringComparer.Ordinal).ThenBy(p => p.Score),
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

    // The empty-theory silent pass is the one failure the cases themselves cannot catch:
    // a corpus that failed to load would run zero cases and report success.
    [Fact]
    public void The_corpus_is_the_one_that_was_committed()
    {
        Assert.Equal(5, Corpus.Cases.Count);
        Assert.Equal("summa", Corpus.Metadata.Library);
        Assert.Equal("1.2.0", Corpus.Metadata.LibraryVersion);
    }
}
