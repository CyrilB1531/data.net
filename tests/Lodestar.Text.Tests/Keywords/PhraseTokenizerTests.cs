using Lodestar.Text.Keywords;
using Xunit;

namespace Lodestar.Text.Tests.Keywords;

public sealed class PhraseTokenizerTests
{
    private static readonly string[] Stop =
        ["of", "the", "over", "a", "and", "are", "for", "all", "to", "in", "is", "this", "that"];

    private static PhraseTokenizer Tokenizer() => new(Stop, @"\b\w+\b");

    [Fact]
    public void Runs_between_stop_words_are_the_candidates()
    {
        IReadOnlyList<IReadOnlyList<string>> runs =
            Tokenizer().Split("Compatibility of systems of linear constraints over the set of natural numbers.");

        Assert.Equal(
            [["compatibility"], ["systems"], ["linear", "constraints"], ["set"], ["natural", "numbers"]],
            runs.Select(r => r.ToArray()).ToArray());
    }

    [Fact]
    public void Punctuation_ends_a_run_even_without_a_stop_word()
    {
        Assert.Equal(
            [["red"], ["green"], ["blue"]],
            Tokenizer().Split("red, green; blue").Select(r => r.ToArray()).ToArray());
    }

    [Fact]
    public void Words_keeps_the_stop_words_and_their_positions()
    {
        Assert.Equal(
            ["linear", "constraints", "over", "the", "set"],
            Tokenizer().Words("linear constraints over the set"));
    }

    [Fact]
    public void A_document_of_only_stop_words_has_no_candidate()
    {
        Assert.Empty(Tokenizer().Split("of the and over a"));
    }
}
