using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

/// <summary>
/// The three pre-tokenizer shapes <see cref="BpeTokenizer"/> now refuses to guess
/// between, and the mode itself. The corpus <c>bpe_no_split.json</c> is replayed
/// by the loader tests, once a file shape can reach the mode.
/// </summary>
public sealed class BpeNoSplitTests
{
    /// <summary>
    /// A vocabulary declaring neither a pattern nor the mode is refused rather
    /// than defaulted. It used to mean the Whitespace split; reinterpreting it
    /// would change what an existing caller gets with nothing to say so.
    /// </summary>
    [Fact]
    public void A_vocabulary_declaring_no_pre_tokenizer_at_all_is_refused()
    {
        var vocabulary = new BpeVocabulary(new Dictionary<string, int> { ["a"] = 0 }, []);

        ArgumentException error = Assert.Throws<ArgumentException>(() => new BpeTokenizer(vocabulary));
        Assert.Contains(nameof(BpeVocabulary.NoPreTokenizer), error.Message, StringComparison.Ordinal);
    }

    /// <summary>The mode beside a pattern contradicts itself, so it is refused too.</summary>
    /// <remarks>
    /// The message is asserted, not only the type: this constructor raises
    /// <see cref="ArgumentException"/> at seven places, so a guard added ahead of
    /// this one would leave a type-only assertion green and empty.
    /// </remarks>
    [Fact]
    public void The_mode_and_a_pattern_together_are_refused()
    {
        var vocabulary = new BpeVocabulary(new Dictionary<string, int> { ["a"] = 0 }, [])
        {
            NoPreTokenizer = true,
            PreTokenizerPattern = BpePatterns.Whitespace,
        };

        ArgumentException error = Assert.Throws<ArgumentException>(() => new BpeTokenizer(vocabulary));
        Assert.Contains(
            $"{nameof(BpeVocabulary.NoPreTokenizer)} and {nameof(BpeVocabulary.PreTokenizerPattern)} together",
            error.Message,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// The same contradiction through the other pattern: a <c>Split</c> step is a
    /// split, so it cannot stand beside a mode that says nothing is split.
    /// </summary>
    [Fact]
    public void The_mode_and_a_pre_split_together_are_refused()
    {
        var vocabulary = new BpeVocabulary(new Dictionary<string, int> { ["a"] = 0 }, [])
        {
            NoPreTokenizer = true,
            PreSplit = new BpeSplitStep(@"\w+", SplitBehavior.Isolated, Invert: false),
        };

        ArgumentException error = Assert.Throws<ArgumentException>(() => new BpeTokenizer(vocabulary));
        Assert.Contains(
            $"{nameof(BpeVocabulary.NoPreTokenizer)} and {nameof(BpeVocabulary.PreSplit)} together",
            error.Message,
            StringComparison.Ordinal);
    }

    /// <summary>The pattern the constructor used to supply is a member now, so a caller who meant the classic lineage can say it.</summary>
    /// <remarks>
    /// The merge is what makes this a claim about the split rather than about coverage:
    /// <c>"a!"</c> is in the vocabulary and reachable in one merge, so only the word
    /// boundary between the letter and the punctuation keeps the two apart.
    /// <c>WhitespaceSplit</c> (<c>\S+</c>) or the mode below would give <c>["a!"]</c>.
    /// </remarks>
    [Fact]
    public void The_whitespace_pattern_is_reachable_and_splits_on_word_boundaries()
    {
        var vocabulary = new BpeVocabulary(
            new Dictionary<string, int> { ["a"] = 0, ["!"] = 1, ["a!"] = 2 },
            [new MergePair("a", "!")])
        {
            PreTokenizerPattern = BpePatterns.Whitespace,
        };

        Assert.Equal(["a", "!"], new BpeTokenizer(vocabulary).Encode("a!").Tokens);
    }

    /// <summary>
    /// The mode itself: the merge loop is handed the whole segment, so a merge
    /// that spans a space applies. With any split it could not.
    /// </summary>
    [Fact]
    public void The_mode_hands_the_merge_loop_one_piece()
    {
        var vocabulary = new BpeVocabulary(
            new Dictionary<string, int> { ["a"] = 0, [" "] = 1, ["b"] = 2, ["a "] = 3, ["a b"] = 4 },
            [new MergePair("a", " "), new MergePair("a ", "b")])
        {
            NoPreTokenizer = true,
        };

        Assert.Equal(["a b"], new BpeTokenizer(vocabulary).Encode("a b").Tokens);
    }
}
