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

    // A clean run reaching the document's last token is dropped whole (summa's inner loop
    // reports only on a rejected continuation). Measured against summa 1.2.0.
    [Fact]
    public void A_clean_run_reaching_the_last_token_is_dropped_whole()
    {
        const string doc = "Copper wires conduct electricity through metal circuits";
        IReadOnlyList<KeywordMatch> withoutPeriod = new TextRank(new TextRankOptions { Words = 4 }).Extract(doc);
        IReadOnlyList<KeywordMatch> withPeriod = new TextRank(new TextRankOptions { Words = 4 }).Extract(doc + ".");

        Assert.Equal(2, withoutPeriod.Count);
        Assert.DoesNotContain(withoutPeriod, h => h.Phrase.Contains("metal", StringComparison.Ordinal));
        KeywordMatch circuitsAlone = Assert.Single(withoutPeriod, h => h.Phrase == "circuits");
        Assert.Equal(0.3966567303643845, circuitsAlone.Score, 12);

        Assert.Equal(3, withPeriod.Count);
        KeywordMatch metalAlone = Assert.Single(withPeriod, h => h.Phrase == "metal");
        Assert.Equal(0.39665673036438454, metalAlone.Score, 12);
    }

    // A phrase carries the spelling at its own position, never a document-wide most
    // common form, and consumption is per spelling. Measured against summa 1.2.0.
    [Fact]
    public void A_phrase_carries_its_own_spelling_and_frees_the_other()
    {
        const string doc = "Copper equation predicts electric current while linear equations describe circuits";
        IReadOnlyList<KeywordMatch> hits = new TextRank(new TextRankOptions { Words = 4 }).Extract(doc);

        Assert.Equal(2, hits.Count);
        KeywordMatch phrase = Assert.Single(hits, h => h.Phrase == "equation predicts electric current");
        Assert.Equal(0.44770524766997183, phrase.Score, 12);
        KeywordMatch standalone = Assert.Single(hits, h => h.Phrase == "equations");
        Assert.Equal(0.6525526168600424, standalone.Score, 12);
    }

    // words and clean must come from one pass: a second pass over a differently-cased
    // string could misalign them for a case-sensitive TokenPattern (no summa counterpart).
    [Fact]
    public void A_case_sensitive_pattern_keeps_words_and_cleanliness_aligned()
    {
        IReadOnlyList<KeywordMatch> hits = new TextRank(new TextRankOptions { TokenPattern = @"\b[a-z]+\b", Words = 2 })
            .Extract("Alpha beta Gamma delta epsilon");

        KeywordMatch hit = Assert.Single(hits);
        Assert.Equal("beta delta", hit.Phrase, StringComparer.Ordinal);
        Assert.Equal(0.6121700988537723, hit.Score, 12);
    }

    // linear occurs twice; summa (measured, 1.2.0) glues only the first occurrence, so the
    // second contributes no separate phrase -- the same once-per-document rule as Glue's.
    [Fact]
    public void A_keyword_occurring_twice_appears_in_exactly_one_phrase()
    {
        IReadOnlyList<KeywordMatch> hits = new TextRank(new TextRankOptions { Words = 7 }).Extract(TwoSentences);

        Assert.Equal(6, hits.Count);
        Assert.Single(hits, h => h.Phrase.Split(' ').Contains("linear", StringComparer.Ordinal));
    }

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

    // long-comment: an exact tie, not an approximate one, is what makes this catch a
    //     regression to List<T>.Sort's unstable introsort rather than pass either way.
    //     A single isolated edge is symmetric under swapping its two endpoints -- same
    //     matrix row, same uniform starting vector -- so alpha and beta rank exactly
    //     equal, bit for bit: IEEE-754 addition is commutative, and every iteration sums
    //     the same two products in swapped order for the two nodes. Window=3 lets the
    //     edge span the one stop word between them without Glue joining the two into one
    //     phrase, since gluing still requires raw adjacency, which the stop word breaks.
    [Fact]
    public void A_genuine_tie_keeps_the_order_gluing_produced_it_in()
    {
        IReadOnlyList<KeywordMatch> hits =
            new TextRank(new TextRankOptions { Window = 3, Words = 2 }).Extract("alpha the beta");

        Assert.Equal(2, hits.Count);
        Assert.Equal(hits[0].Score, hits[1].Score);
        Assert.Equal("alpha", hits[0].Phrase, StringComparer.Ordinal);
        Assert.Equal("beta", hits[1].Phrase, StringComparer.Ordinal);
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
