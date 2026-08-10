using System.Text.Json;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

public sealed class WordPieceTokenizerTests
{
    private static readonly string[] AbTokens = ["ab", "##c"];
    private static readonly string[] UnkTokens = ["[UNK]"];

    private static WordPieceTokenizer BuildFromOracle(JsonDocument doc)
    {
        JsonElement vocabEl = doc.RootElement.GetProperty("metadata").GetProperty("vocab");
        var vocab = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (JsonProperty p in vocabEl.EnumerateObject())
        {
            vocab[p.Name] = p.Value.GetInt32();
        }
        string unk = doc.RootElement.GetProperty("metadata").GetProperty("unk_token").GetString()!;
        return new WordPieceTokenizer(vocab, unk);
    }

    [Fact]
    public void Encode_matches_huggingface()
    {
        using JsonDocument doc = OracleLoader.Load("wordpiece.json");
        WordPieceTokenizer tokenizer = BuildFromOracle(doc);

        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string text = c.GetProperty("text").GetString()!;
            string[] expectedTokens = c.GetProperty("tokens").EnumerateArray().Select(e => e.GetString()!).ToArray();
            int[] expectedIds = c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();

            TokenizationResult actual = tokenizer.Encode(text);

            Assert.True(expectedTokens.SequenceEqual(actual.Tokens),
                $"'{text}': tokens differ.\n  expected: [{string.Join(", ", expectedTokens)}]\n  actual:   [{string.Join(", ", actual.Tokens)}]");
            Assert.True(expectedIds.SequenceEqual(actual.Ids), $"'{text}': ids differ.");
        }
    }

    [Fact]
    public void Unknown_word_becomes_unk()
    {
        var vocab = new Dictionary<string, int> { ["[UNK]"] = 0, ["ab"] = 1, ["##c"] = 2 };
        var t = new WordPieceTokenizer(vocab);
        Assert.Equal(AbTokens, t.Encode("abc").Tokens);
        Assert.Equal(UnkTokens, t.Encode("xyz").Tokens);
    }

    /// <summary>The vocabulary the added-token cases share: two flavours of the same marker.</summary>
    /// <remarks>
    /// <c>[CLS]</c> and <c>[cls]</c> are both present, at different ids, so a case
    /// that emits one of them cannot be passing by accident on the other.
    /// </remarks>
    private static WordPieceTokenizer WithAdded(params AddedToken[] added) =>
        new(new WordPieceVocabulary(
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["[UNK]"] = 0,
                ["a"] = 1,
                ["b"] = 2,
                ["[CLS]"] = 3,
                ["[cls]"] = 4,
                ["<s>"] = 5,
            },
            "[UNK]",
            "##",
            Lowercase: true)
        {
            AddedTokens = added,
        });

    /// <summary>
    /// A non-normalized entry — what <c>add_special_tokens</c> writes, and what the
    /// loader assumes when a file marks an entry <c>special</c> and says nothing
    /// about <c>normalized</c>.
    /// </summary>
    [Fact]
    public void A_non_normalized_added_token_is_matched_against_the_raw_text()
    {
        WordPieceTokenizer tokenizer = WithAdded(new AddedToken("[CLS]", 3) { Special = true });

        Assert.Equal(new TokenizationResult(["a", "[CLS]", "b"], [1, 3, 2]), tokenizer.Encode("a [CLS] b"));
        // Measured against tokenizers 0.23.1: the entry is matched against the raw
        // text, so lowercased input never reaches it and falls through to the model —
        // where the Whitespace pre-tokenizer cuts '[cls]' into '[', 'cls' and ']',
        // none of which the vocabulary holds. Three unknowns, not one.
        Assert.Equal(
            new TokenizationResult(["a", "[UNK]", "[UNK]", "[UNK]", "b"], [1, 0, 0, 0, 2]),
            tokenizer.Encode("a [cls] b"));
    }

    [Theory]
    [InlineData("a [CLS] b")]
    [InlineData("a [cls] b")]
    public void A_normalized_added_token_is_lowercased_along_with_the_text(string text)
    {
        WordPieceTokenizer tokenizer = WithAdded(new AddedToken("[CLS]", 3) { Normalized = true });

        // Either spelling matches, and both emit the *normalized* text carrying the
        // added token's own id — 3, not the 4 the vocabulary maps '[cls]' to.
        Assert.Equal(new TokenizationResult(["a", "[cls]", "b"], [1, 3, 2]), tokenizer.Encode(text));
    }

    /// <summary>
    /// The pass an entry runs in is decided by <see cref="AddedToken.Normalized"/>
    /// alone, not by <see cref="AddedToken.Special"/>. Measured against tokenizers
    /// 0.23.1: <c>special=true, normalized=true</c> matches both spellings and emits
    /// the lowercased text, exactly as an ordinary entry does — where
    /// <c>special=true, normalized=false</c> (the previous test) matches neither.
    /// The two flags are independent and this is the case that separates them.
    /// </summary>
    [Theory]
    [InlineData("a [CLS] b")]
    [InlineData("a [cls] b")]
    public void A_special_added_token_that_is_normalized_runs_in_the_normalized_pass(string text)
    {
        WordPieceTokenizer tokenizer = WithAdded(new AddedToken("[CLS]", 3) { Special = true, Normalized = true });

        Assert.Equal(new TokenizationResult(["a", "[cls]", "b"], [1, 3, 2]), tokenizer.Encode(text));
    }

    /// <summary>
    /// <c>lstrip</c> absorbs the whitespace into the token the match emits, id
    /// unchanged. Measured against tokenizers 0.23.1, which reports the same token
    /// and the offsets to match.
    /// </summary>
    [Fact]
    public void An_added_token_with_lstrip_absorbs_the_whitespace_before_it()
    {
        WordPieceTokenizer tokenizer = WithAdded(new AddedToken("<s>", 5) { Lstrip = true, Special = true });

        Assert.Equal(new TokenizationResult(["a", "  <s>", "b"], [1, 5, 2]), tokenizer.Encode("a  <s> b"));
    }

    /// <summary>
    /// The raw half is matched in an outer pass, so one of its entries wins over a
    /// normalized one matching further left — <c>x</c> and <c>a</c> reach the model
    /// as the single unknown word <c>xa</c> rather than <c>a&lt;s&gt;</c> being
    /// matched at index 1. Measured; leftmost-wins across the whole table, which is
    /// the natural guess, gives the other answer.
    /// </summary>
    [Fact]
    public void A_raw_added_token_outranks_a_normalized_one_starting_further_left()
    {
        WordPieceTokenizer tokenizer = WithAdded(
            new AddedToken("<s>", 5) { Special = true },
            new AddedToken("a<s", 6) { Normalized = true });

        Assert.Equal(new TokenizationResult(["[UNK]", "<s>", "[UNK]"], [0, 5, 0]), tokenizer.Encode("xa<s>y"));
    }
}
