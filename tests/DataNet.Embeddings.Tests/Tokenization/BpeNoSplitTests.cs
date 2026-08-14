using System.Text;
using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tests.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

/// <summary>
/// The three pre-tokenizer shapes <see cref="BpeTokenizer"/> refuses to guess
/// between, the mode itself, and <c>bpe_no_split.json</c>: seven hand-built
/// models, four of which reach the mode through a file.
/// </summary>
public sealed class BpeNoSplitTests
{
    private const string Corpus = "bpe_no_split.json";

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

    [Fact]
    public void Encode_matches_tokenizers_for_every_model_the_corpus_carries()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        int models = 0;

        foreach (JsonProperty model in doc.RootElement.GetProperty("metadata").GetProperty("models").EnumerateObject())
        {
            models++;
            var tokenizer = new BpeTokenizer(Vocabulary(model.Value));
            OracleReplay.AssertEncodings(doc, tokenizer.Encode, "tokens", model.Name, nameProperty: "model");
        }

        Assert.True(models > 0, $"{Corpus} carries no model.");
    }

    /// <summary>
    /// The defect this lot exists for: a file declaring no pre-tokenizer used to
    /// load as the Whitespace split, which is a different token stream and said
    /// nothing about it. Both streams are the corpus's own, cases 0 and 3.
    /// </summary>
    [Fact]
    public void A_file_declaring_no_pre_tokenizer_no_longer_loads_as_whitespace()
    {
        Assert.True(Vocabulary("absent").NoPreTokenizer);

        Assert.Equal(["a", "[UNK]", "a"], Tokens("absent", "aZ Za"));
        Assert.Equal(["a", "[UNK]", "[UNK]", "a"], Tokens("whitespace", "aZ Za"));
    }

    /// <summary>
    /// A byte-level model with <c>use_regex</c> off loads, where it was refused —
    /// and hands the merge loop one piece, which the corpus's <c>oĠ</c> merge
    /// makes visible: it spans the cut the flag would have made. Cases 6 and 10.
    /// </summary>
    [Fact]
    public void A_byte_level_model_with_use_regex_off_loads_and_does_not_split()
    {
        Assert.True(Vocabulary("byte_level_no_regex").NoPreTokenizer);

        Assert.Equal(
            ["h", "e", "l", "l", "oĠ", "w", "o", "r", "l", "d"],
            Tokens("byte_level_no_regex", "hello world"));
        Assert.Equal(
            ["h", "e", "l", "l", "o", "Ġ", "w", "o", "r", "l", "d"],
            Tokens("byte_level_regex", "hello world"));
    }

    /// <summary>The round trip survives the mode, which is what makes it worth having.</summary>
    /// <remarks>
    /// <c>byte_level_no_regex</c>, not <c>no_regex_prefix_space</c>: what breaks the
    /// round trip there is the prepended space, not the mode — the test below.
    /// </remarks>
    [Theory]
    [InlineData("hello world")]
    [InlineData("  leading and trailing  ")]
    public void A_no_split_encoding_round_trips(string text)
    {
        var tokenizer = new BpeTokenizer(Vocabulary("byte_level_no_regex"));

        Assert.Equal(text, tokenizer.Decode(tokenizer.Encode(text).Ids));
    }

    /// <summary>
    /// The prepended space comes back out, so the text is not what was handed in.
    /// Replays case 14's <c>decoded</c>, which nothing else in this corpus reads —
    /// its cases carry no <c>decoded_skip_specials</c>, so <c>AssertDecodes</c> cannot.
    /// </summary>
    [Fact]
    public void The_prefix_space_model_decodes_the_space_it_prepended()
    {
        var tokenizer = new BpeTokenizer(Vocabulary("no_regex_prefix_space"));

        Assert.Equal(" hello world", tokenizer.Decode(tokenizer.Encode("hello world").Ids));
    }

    private static string[] Tokens(string model, string text) =>
        [.. new BpeTokenizer(Vocabulary(model)).Encode(text).Tokens];

    private static BpeVocabulary Vocabulary(string model)
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        return Vocabulary(doc.RootElement.GetProperty("metadata").GetProperty("models").GetProperty(model));
    }

    private static BpeVocabulary Vocabulary(JsonElement model)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(model.GetProperty("tokenizer_json").GetString()!));
        return TokenizerJsonLoader.LoadBpe(stream, OracleReplay.BpeBounds());
    }
}
