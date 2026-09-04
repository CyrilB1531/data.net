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
        AssertRankingMatches(expected.Expected, actual);
    }

    // Order is free only within a run of adjacent scores tied inside 1e-9; a real rank gap
    // is checked positionally, catching the right phrases and scores reported at the wrong rank.
    private static void AssertRankingMatches(IReadOnlyList<TextRankPhrase> expected, IReadOnlyList<KeywordMatch> actual)
    {
        var comparer = new ApproximatePhraseScoreComparer();
        int start = 0;
        for (int i = 0; i < expected.Count; i++)
        {
            bool tiedWithNext = i + 1 < expected.Count && Math.Abs(expected[i].Score - expected[i + 1].Score) <= 1e-9;
            if (tiedWithNext)
            {
                continue;
            }

            int length = i - start + 1;
            IEnumerable<(string Phrase, double Score)> expectedRun = expected.Skip(start).Take(length)
                .Select(p => (p.Phrase, p.Score)).OrderBy(p => p.Phrase, StringComparer.Ordinal);
            IEnumerable<(string Phrase, double Score)> actualRun = actual.Skip(start).Take(length)
                .Select(m => (m.Phrase, m.Score)).OrderBy(p => p.Phrase, StringComparer.Ordinal);
            Assert.Equal(expectedRun, actualRun, comparer);
            start = i + 1;
        }
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
