using Lodestar.Text.Keywords;
using Xunit;

namespace Lodestar.Text.Tests.Keywords;

public sealed class RakeTests
{
    private const string Abstract =
        "Compatibility of systems of linear constraints over the set of natural numbers.";

    private static readonly string[] Stop =
        ["of", "the", "over", "a", "and", "are", "for", "all", "to", "in", "is", "this", "that"];

    private static Rake Extractor(RakeOptions? options = null) =>
        new((options ?? new RakeOptions()) with { StopWords = Stop });

    [Fact]
    public void Degree_over_frequency_is_the_default_and_ranks_the_two_pairs_first()
    {
        IReadOnlyList<KeywordMatch> hits = Extractor().Extract(Abstract);

        Assert.Equal(5, hits.Count);
        Assert.Equal(4.0, hits[0].Score, 12);
        Assert.Equal(4.0, hits[1].Score, 12);
        // rake-nltk's tie order (rake_nltk/rake.py:241): phrase descending, the opposite
        // of both text order and ascending alphabetical order.
        Assert.Equal("natural numbers", hits[0].Phrase, StringComparer.Ordinal);
        Assert.Equal("linear constraints", hits[1].Phrase, StringComparer.Ordinal);
        Assert.All(hits.Skip(2), h => Assert.Equal(1.0, h.Score, 12));
    }

    [Fact]
    public void A_three_way_tie_breaks_by_phrase_descending_not_by_text_order()
    {
        // Three pairs tied at 4.0, ordered the same way by text order and by ascending
        // alphabetical order -- only descending-by-phrase gives zeta, gamma, alpha.
        IReadOnlyList<KeywordMatch> hits = Extractor().Extract("alpha beta and gamma delta and zeta eta");

        Assert.Equal(
            ["zeta eta", "gamma delta", "alpha beta"],
            hits.Select(h => h.Phrase),
            StringComparer.Ordinal);
        Assert.All(hits, h => Assert.Equal(4.0, h.Score, 12));
    }

    [Fact]
    public void Word_frequency_flattens_them_all_to_one()
    {
        IReadOnlyList<KeywordMatch> hits =
            Extractor(new RakeOptions { Metric = RakeMetric.WordFrequency }).Extract(Abstract);

        // Every word occurs once, so a one-word phrase scores 1 and a two-word phrase 2.
        Assert.Equal(2.0, hits[0].Score, 12);
        Assert.Equal(2.0, hits[1].Score, 12);
    }

    [Fact]
    public void Word_degree_scores_a_pair_by_its_span()
    {
        IReadOnlyList<KeywordMatch> hits =
            Extractor(new RakeOptions { Metric = RakeMetric.WordDegree }).Extract(Abstract);

        Assert.Equal(4.0, hits[0].Score, 12);
    }

    [Fact]
    public void Length_bounds_are_inclusive_and_count_words()
    {
        IReadOnlyList<KeywordMatch> pairs =
            Extractor(new RakeOptions { MinLength = 2 }).Extract(Abstract);

        Assert.Equal(2, pairs.Count);
        // The installed xunit has no case-sensitive char-in-string assertion; a bare char search is already ordinal.
        Assert.All(pairs, h => Assert.Contains(' ', h.Phrase));
    }

    [Fact]
    public void MaxLength_is_inclusive_and_keeps_a_candidate_at_the_bound()
    {
        // A candidate exactly at MaxLength must survive; a "<" in place of "<=" would drop it.
        IReadOnlyList<KeywordMatch> hits =
            Extractor(new RakeOptions { MaxLength = 2 }).Extract(Abstract);

        Assert.Contains(hits, h => h.Phrase == "natural numbers");
        Assert.Contains(hits, h => h.Phrase == "linear constraints");
    }

    [Fact]
    public void TokenPattern_narrows_what_counts_as_a_word()
    {
        // The excluded digit still sits between the words, so it splits the run into two
        // candidates; a Rake that ignored TokenPattern would report one hit here, not two.
        IReadOnlyList<KeywordMatch> hits = Extractor(new RakeOptions { TokenPattern = @"\b[a-zA-Z]+\b" })
            .Extract("linear 123 constraints");

        Assert.Equal(2, hits.Count);
        Assert.Contains(hits, h => h.Phrase == "linear");
        Assert.Contains(hits, h => h.Phrase == "constraints");
    }

    [Fact]
    public void A_run_the_length_filter_dropped_contributes_to_no_table()
    {
        // With MinLength = 2 the lone "linear" is gone before the tables build, so the
        // pair scores 4; counting the dropped run first would make it 3.5.
        IReadOnlyList<KeywordMatch> hits =
            Extractor(new RakeOptions { MinLength = 2 }).Extract("linear constraints and linear");

        Assert.Single(hits);
        Assert.Equal(4.0, hits[0].Score, 12);
    }

    [Fact]
    public void A_repeated_phrase_is_reported_once_when_repeats_are_excluded()
    {
        var options = new RakeOptions { IncludeRepeatedPhrases = false };
        IReadOnlyList<KeywordMatch> hits = Extractor(options).Extract("linear constraints and linear constraints");

        Assert.Single(hits);
        Assert.Equal("linear constraints", hits[0].Phrase, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData(RakeMetric.WordFrequency, 2.0)]
    [InlineData(RakeMetric.WordDegree, 4.0)]
    public void Excluding_repeats_removes_them_from_the_tables_too(RakeMetric metric, double expected)
    {
        // Measured against rake-nltk: include_repeated_phrases=False leaves degree 2 and
        // frequency 1, not 4 and 2. Deduplicating only the output would read 4.0 and 8.0.
        var options = new RakeOptions { IncludeRepeatedPhrases = false, Metric = metric };
        IReadOnlyList<KeywordMatch> hits = Extractor(options).Extract("linear constraints and linear constraints");

        Assert.Equal(expected, hits[0].Score, 12);
    }

    [Fact]
    public void An_empty_document_yields_nothing()
    {
        Assert.Empty(Extractor().Extract(string.Empty));
    }

    [Fact]
    public void Null_text_is_refused()
    {
        Assert.Throws<ArgumentNullException>(() => Extractor().Extract(null!));
    }

    [Fact]
    public void A_length_range_that_cannot_match_is_refused_at_construction()
    {
        Assert.Throws<ArgumentException>(() => new Rake(new RakeOptions { MinLength = 3, MaxLength = 2 }));
    }
}
