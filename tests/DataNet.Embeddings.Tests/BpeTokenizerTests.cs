using System.Text.Json;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

public sealed class BpeTokenizerTests
{
    /// <summary>Reads tiny_bpe.json directly: this suite tests merging, not loading.</summary>
    internal static BpeVocabulary TinyVocabulary()
    {
        using JsonDocument doc = OracleLoader.Load("tiny_bpe.json");
        JsonElement model = doc.RootElement.GetProperty("model");

        var vocab = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (JsonProperty entry in model.GetProperty("vocab").EnumerateObject())
        {
            vocab[entry.Name] = entry.Value.GetInt32();
        }

        var merges = new List<MergePair>();
        foreach (JsonElement merge in model.GetProperty("merges").EnumerateArray())
        {
            if (merge.ValueKind == JsonValueKind.Array)
            {
                merges.Add(new MergePair(merge[0].GetString()!, merge[1].GetString()!));
            }
            else
            {
                string[] parts = merge.GetString()!.Split(' ');
                merges.Add(new MergePair(parts[0], parts[1]));
            }
        }

        // added_tokens is a sibling of "model", not nested inside it: HuggingFace
        // records it at the top level of tokenizer.json, alongside the pipeline
        // stages that consult it. "[UNK]" is both a plain vocabulary entry (id 0,
        // in model.vocab above) and a declared added token, so BpeTokenizer.Encode
        // recognizes it as a literal match rather than splitting it letter by letter.
        var addedTokens = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (JsonElement added in doc.RootElement.GetProperty("added_tokens").EnumerateArray())
        {
            addedTokens[added.GetProperty("content").GetString()!] = added.GetProperty("id").GetInt32();
        }

        return new BpeVocabulary(vocab, merges)
        {
            AddedTokens = addedTokens,
            EndOfWordSuffix = model.GetProperty("end_of_word_suffix").GetString(),
            UnkToken = model.GetProperty("unk_token").GetString(),
        };
    }

    [Fact]
    public void Encode_matches_tokenizers()
    {
        using JsonDocument doc = OracleLoader.Load("bpe.json");
        var tokenizer = new BpeTokenizer(TinyVocabulary());

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string text = c.GetProperty("text").GetString()!;
            string[] expectedTokens = c.GetProperty("tokens").EnumerateArray().Select(e => e.GetString()!).ToArray();
            int[] expectedIds = c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();

            TokenizationResult actual = tokenizer.Encode(text);
            if (!expectedTokens.SequenceEqual(actual.Tokens) || !expectedIds.SequenceEqual(actual.Ids))
            {
                failures.Add($"{JsonSerializer.Serialize(text)}\n  exp: [{string.Join(" | ", expectedTokens)}]\n  got: [{string.Join(" | ", actual.Tokens)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void TryGetId_finds_a_literal_entry()
    {
        var tokenizer = new BpeTokenizer(TinyVocabulary());
        Assert.True(tokenizer.TryGetId("[UNK]", out int unk));
        Assert.Equal(0, unk);
        Assert.False(tokenizer.TryGetId("definitely-not-a-token", out _));
    }

    [Fact]
    public void An_unknown_token_absent_from_the_vocabulary_is_refused()
    {
        BpeVocabulary broken = TinyVocabulary() with { UnkToken = "[NOPE]" };
        Assert.Throws<ArgumentException>(() => new BpeTokenizer(broken));
    }

    [Fact]
    public void A_merge_naming_a_missing_token_is_dropped()
    {
        BpeVocabulary vocab = TinyVocabulary();
        var merges = new List<MergePair>(vocab.Merges) { new("zzz", "qqq") };
        var tokenizer = new BpeTokenizer(vocab with { Merges = merges });

        // The pair cannot apply, so tokenization is unchanged.
        Assert.Equal(
            new BpeTokenizer(vocab).Encode("the quick brown fox").Ids,
            tokenizer.Encode("the quick brown fox").Ids);
    }

    /// <summary>
    /// Regression test for the leftmost/longest added-token rule: the corpus
    /// declares exactly one added token ("[UNK]"), so it cannot distinguish this
    /// rule from any other. Two added tokens sharing a prefix and starting at the
    /// same position must resolve to the longer one, matching HuggingFace's
    /// AddedVocabulary (Aho-Corasick <c>LeftmostLongest</c>).
    /// </summary>
    [Fact]
    public void Added_tokens_prefer_the_longest_match_at_the_same_position()
    {
        BpeVocabulary vocab = TinyVocabulary() with
        {
            AddedTokens = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["<a>"] = 1000,
                ["<a>b"] = 1001,
            },
        };
        var tokenizer = new BpeTokenizer(vocab);

        TokenizationResult result = tokenizer.Encode("<a>b");

        Assert.Equal(["<a>b"], result.Tokens);
        Assert.Equal([1001], result.Ids);
    }

    /// <summary>
    /// Regression test for the other half of the rule: a shorter match that starts
    /// earlier is chosen over a longer match that starts later, even though "longer
    /// wins" is the tie-break at equal positions. Leftmost always wins first.
    /// </summary>
    [Fact]
    public void Added_tokens_prefer_the_earliest_position_over_a_longer_later_match()
    {
        BpeVocabulary vocab = TinyVocabulary() with
        {
            AddedTokens = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["<z>"] = 2000,
                ["<a><a>"] = 2001,
            },
        };
        var tokenizer = new BpeTokenizer(vocab);

        TokenizationResult result = tokenizer.Encode("<z><a><a>");

        Assert.Equal(["<z>", "<a><a>"], result.Tokens);
        Assert.Equal([2000, 2001], result.Ids);
    }

    /// <summary>
    /// A malformed vocabulary can declare an empty added token: the loader this
    /// vocabulary is meant to come from bounds a token's upper length but never
    /// rejects an empty one. Left unfiltered, <c>IndexOf("", pos)</c> always
    /// returns <c>pos</c>, so the scan in <c>Encode</c> never advances.
    /// </summary>
    [Fact]
    public void An_empty_added_token_is_ignored_rather_than_hanging_encode()
    {
        BpeVocabulary vocab = TinyVocabulary() with
        {
            AddedTokens = new Dictionary<string, int>(StringComparer.Ordinal) { [string.Empty] = 999 },
        };
        var tokenizer = new BpeTokenizer(vocab);

        TokenizationResult result = tokenizer.Encode("the quick brown fox");

        Assert.Equal(new BpeTokenizer(TinyVocabulary()).Encode("the quick brown fox").Ids, result.Ids);
    }
}
