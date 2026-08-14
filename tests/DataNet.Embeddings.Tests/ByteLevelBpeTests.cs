using System.Text.Json;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

public sealed class ByteLevelBpeTests
{
    /// <summary>
    /// Builds GPT-2's model from the vendored files directly, so a failure here
    /// is the tokenizer rather than the loader (which Task 11 covers).
    /// </summary>
    internal static BpeVocabulary Gpt2Vocabulary()
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "oracles");
        using JsonDocument vocabDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "gpt2_vocab.json")));

        var vocab = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (JsonProperty entry in vocabDoc.RootElement.EnumerateObject())
        {
            vocab[entry.Name] = entry.Value.GetInt32();
        }

        string[] lines = File.ReadAllLines(Path.Combine(dir, "gpt2_merges.txt"));

        // Only a leading "#version:" line is a header. '#' is also GPT-2's own alphabet
        // symbol, so eight real merges start with it ("# #", "## ##", ...) and must not be skipped.
        int first = lines.Length > 0 && lines[0].StartsWith("#version", StringComparison.Ordinal) ? 1 : 0;

        var merges = new List<MergePair>(lines.Length - first);
        for (int i = first; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.Length == 0)
            {
                continue;
            }
            int space = line.IndexOf(' ', StringComparison.Ordinal);
            merges.Add(new MergePair(line.Substring(0, space), line.Substring(space + 1)));
        }

        return new BpeVocabulary(vocab, merges)
        {
            ByteLevel = true,
            PreTokenizerPattern = BpePatterns.Gpt2,
        };
    }

    [Fact]
    public void Encode_matches_tokenizers_over_the_gpt2_vocabulary()
    {
        using JsonDocument doc = OracleLoader.Load("bytelevel_bpe.json");
        var tokenizer = new BpeTokenizer(Gpt2Vocabulary());

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string text = c.GetProperty("text").GetString()!;
            string[] expectedTokens = c.GetProperty("tokens").EnumerateArray().Select(e => e.GetString()!).ToArray();
            int[] expectedIds = c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();

            TokenizationResult actual = tokenizer.Encode(text);
            if (!expectedTokens.SequenceEqual(actual.Tokens) || !expectedIds.SequenceEqual(actual.Ids))
            {
                failures.Add($"{JsonSerializer.Serialize(text)}\n  exp: [{string.Join(" | ", expectedTokens)}]\n  got: [{string.Join(" | ", actual.Tokens)}]\n  exp ids: [{string.Join(", ", expectedIds)}]\n  got ids: [{string.Join(", ", actual.Ids)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void The_vendored_vocabulary_is_the_one_the_corpus_was_built_from()
    {
        // 50 257 is GPT-2's size. A fixture that silently changed shape would
        // otherwise surface as a wall of token diffs rather than as itself.
        BpeVocabulary vocabulary = Gpt2Vocabulary();
        Assert.Equal(50257, vocabulary.Count);

        // gpt2_merges.txt has 50 001 lines (header plus 50 000 merges); 256 byte symbols
        // plus those merges plus <|endoftext|> reconciles to the vocabulary count above.
        Assert.Equal(50000, vocabulary.Merges.Count);
    }

    [Fact]
    public void Decode_matches_tokenizers_in_both_modes()
    {
        using JsonDocument doc = OracleLoader.Load("bytelevel_bpe.json");
        var tokenizer = new BpeTokenizer(Gpt2Vocabulary());

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            int[] ids = c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();
            string expected = c.GetProperty("decoded").GetString()!;
            string expectedSkipping = c.GetProperty("decoded_skip_specials").GetString()!;

            string actual = tokenizer.Decode(ids);
            string actualSkipping = tokenizer.Decode(ids, skipSpecialTokens: true);
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                failures.Add($"decode {JsonSerializer.Serialize(c.GetProperty("text").GetString())}\n  exp: {JsonSerializer.Serialize(expected)}\n  got: {JsonSerializer.Serialize(actual)}");
            }
            if (!string.Equals(expectedSkipping, actualSkipping, StringComparison.Ordinal))
            {
                failures.Add($"decode(skip) {JsonSerializer.Serialize(c.GetProperty("text").GetString())}\n  exp: {JsonSerializer.Serialize(expectedSkipping)}\n  got: {JsonSerializer.Serialize(actualSkipping)}");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// The property byte-level BPE exists to guarantee. It is asserted separately
    /// from the oracle comparison because it is the claim a user relies on, and
    /// because a mapping-table error can satisfy neither while looking like only
    /// one is broken.
    /// </summary>
    [Fact]
    public void Decode_of_Encode_is_the_input()
    {
        using JsonDocument doc = OracleLoader.Load("bytelevel_bpe.json");
        var tokenizer = new BpeTokenizer(Gpt2Vocabulary());

        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string text = c.GetProperty("text").GetString()!;
            Assert.Equal(text, tokenizer.Decode(tokenizer.Encode(text).Ids));
        }
    }

    [Fact]
    public void The_span_overload_agrees_with_the_list_one()
    {
        var tokenizer = new BpeTokenizer(Gpt2Vocabulary());
        int[] ids = [.. tokenizer.Encode("round trip 東京 👋").Ids];
        Assert.Equal(tokenizer.Decode(ids), tokenizer.Decode(ids.AsSpan()));
    }

    [Fact]
    public void Decode_rejects_an_id_outside_the_vocabulary()
    {
        var tokenizer = new BpeTokenizer(Gpt2Vocabulary());
        Assert.Throws<ArgumentOutOfRangeException>(() => tokenizer.Decode(new[] { 999_999 }));
    }

    /// <summary>
    /// Not proof <c>ignore_merges</c> does anything -- <c>ids_ignore_merges</c> equals <c>ids</c> for
    /// all 20 cases, since every multi-symbol entry in a natively-trained vocabulary like GPT-2's is
    /// reachable by replaying its own creating merge (checked exhaustively over all 50 257 entries;
    /// none diverges). What it pins is the complement: a short-circuit keyed on the wrong string, or
    /// applied before the byte-level mapping, would turn some of these 20 red even though the flag
    /// should be inert here. The test proving the flag does something is
    /// <see cref="BpeTokenizerTests.IgnoreMerges_emits_a_whole_piece_present_in_the_vocabulary"/>.
    /// </summary>
    [Fact]
    public void IgnoreMerges_is_behaviour_neutral_on_a_natively_trained_vocabulary()
    {
        using JsonDocument doc = OracleLoader.Load("bytelevel_bpe.json");
        var tokenizer = new BpeTokenizer(Gpt2Vocabulary() with { IgnoreMerges = true });

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string text = c.GetProperty("text").GetString()!;
            int[] expected = c.GetProperty("ids_ignore_merges").EnumerateArray().Select(e => e.GetInt32()).ToArray();
            int[] actual = [.. tokenizer.Encode(text).Ids];
            if (!expected.SequenceEqual(actual))
            {
                failures.Add($"{JsonSerializer.Serialize(text)}\n  exp: [{string.Join(", ", expected)}]\n  got: [{string.Join(", ", actual)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }
}
