using Lodestar.Text.Keywords;
using Lodestar.Text.Stemming;
using Lodestar.Text.Vectorization;
using Xunit;

namespace Lodestar.Text.Tests.Keywords;

public sealed class TextRankTests
{
    private const string TwoSentences =
        "Compatibility of systems of linear constraints over the set of natural numbers. " +
        "Criteria of compatibility of a system of linear Diophantine equations.";

    [Fact]
    public void The_four_highest_match_what_summa_publishes()
    {
        IReadOnlyList<KeywordMatch> hits = new TextRank(new TextRankOptions { Words = 4 }).Extract(TwoSentences);

        Assert.Equal(4, hits.Count);
        Assert.Equal("numbers", hits[0].Phrase, StringComparer.Ordinal);
        Assert.Equal(0.526895906655717, hits[0].Score, 12);
    }

    // Every Words=8 survivor here ends up glued, so a Single()-on-"contains a space"
    // lookup throws; this checks the mean property against every glued phrase instead.
    [Fact]
    public void Adjacent_survivors_are_glued_and_scored_by_their_mean()
    {
        IReadOnlyList<KeywordMatch> hits = new TextRank(new TextRankOptions { Words = 8 }).Extract(TwoSentences);
        Dictionary<string, double> scoreByStem = RankStems(TwoSentences);

        IReadOnlyList<KeywordMatch> glued = hits.Where(h => h.Phrase.Contains(' ', StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(glued);
        foreach (KeywordMatch hit in glued)
        {
            double[] parts = hit.Phrase.Split(' ').Select(w => scoreByStem[EnglishSnowballStemmer.Stem(w)]).ToArray();
            Assert.Equal(parts.Average(), hit.Score, 12);
        }
    }

    // Mirrors what TextRank.Extract builds internally: one entry per raw token, null
    // where a stop word stood, ranked by the same graph the extractor consumes.
    private static Dictionary<string, double> RankStems(string text)
    {
        StopWordSet stop = StopWordSet.Adopt(StopWords.English);
        var tokenizer = new PhraseTokenizer(StopWords.English, @"\b\w+\b");
        string?[] stream = tokenizer.Words(text)
            .Select(word => stop.Contains(word) ? null : EnglishSnowballStemmer.Stem(word))
            .ToArray();

        var graph = new WordGraph(stream, window: 2);
        double[] ranked = graph.Rank(damping: 0.85, tolerance: 1e-12, maxIterations: 1000);
        return graph.Nodes
            .Select((stem, i) => (stem, score: ranked[i]))
            .ToDictionary(p => p.stem, p => p.score, StringComparer.Ordinal);
    }

    [Fact]
    public void Words_overrides_ratio()
    {
        var byRatio = new TextRank(new TextRankOptions { Ratio = 0.2 }).Extract(TwoSentences);
        var byCount = new TextRank(new TextRankOptions { Ratio = 0.2, Words = 5 }).Extract(TwoSentences);

        Assert.NotEqual(byRatio.Count, byCount.Count);
    }

    [Fact]
    public void A_document_with_no_co_occurrence_yields_nothing()
    {
        Assert.Empty(new TextRank().Extract("Alpha."));
    }

    [Fact]
    public void An_empty_document_yields_nothing()
    {
        Assert.Empty(new TextRank().Extract(string.Empty));
    }

    [Fact]
    public void Null_text_is_refused()
    {
        Assert.Throws<ArgumentNullException>(() => new TextRank().Extract(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_window_below_one_is_refused(int window)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextRank(new TextRankOptions { Window = window }));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    public void A_damping_outside_the_open_unit_interval_is_refused(double damping)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextRank(new TextRankOptions { Damping = damping }));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.5)]
    public void A_ratio_outside_zero_to_one_is_refused(double ratio)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextRank(new TextRankOptions { Ratio = ratio }));
    }
}
