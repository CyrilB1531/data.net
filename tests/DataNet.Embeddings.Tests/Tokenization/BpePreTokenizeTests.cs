using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests.Tokenization;

public sealed class BpePreTokenizeTests
{
    /// <summary>
    /// The split is claimed for three model families but the vocabulary is vendored for one (ADR 0017),
    /// so this is the test carrying the Llama-3 and Qwen2 rows of the parity table. The oracle records
    /// HuggingFace's full pre-tokenizer pipeline -- <c>Split</c> followed by <c>ByteLevel</c> -- so its
    /// <c>pieces</c> are already byte-mapped (<c>"Ġworld"</c>, not <c>" world"</c>). <see cref="BpePreTokenizer"/>
    /// is only the <c>Split</c> stage: the byte alphabet lives in exactly one place, <c>BpeTokenizer</c>,
    /// which would otherwise re-encode an already-mapped piece and corrupt it. This test therefore maps
    /// each produced piece forward through <see cref="ToByteLevel"/> before comparing, to reassemble the
    /// pipeline the oracle actually recorded -- the mapping belongs to the test, not the type under test.
    /// </summary>
    [Fact]
    public void Split_matches_tokenizers_for_every_pattern()
    {
        using JsonDocument doc = OracleLoader.Load("bpe_pretokenize.json");
        JsonElement patterns = doc.RootElement.GetProperty("metadata").GetProperty("patterns");

        var failures = new List<string>();
        var pieces = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("pattern").GetString()!;
            string text = c.GetProperty("text").GetString()!;
            string[] expected = c.GetProperty("pieces").EnumerateArray().Select(e => e.GetString()!).ToArray();

            var splitter = new BpePreTokenizer(null, patterns.GetProperty(name).GetString(), false, false);
            pieces.Clear();
            splitter.Split(text, pieces);
            string[] mapped = [.. pieces.Select(ToByteLevel)];

            if (!expected.SequenceEqual(mapped))
            {
                failures.Add($"[{name}] {JsonSerializer.Serialize(text)}\n  exp: [{string.Join(" | ", expected)}]\n  got: [{string.Join(" | ", mapped)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    // Forward mapping (piece -> alphabet) is total, so a split bug shows as a piece mismatch, not a
    // mapping failure. The reverse (oracle -> bytes) can fail mid-string and confuse the two.
    private static string ToByteLevel(string piece)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(piece);
        var mapped = new char[bytes.Length];
        for (int i = 0; i < bytes.Length; i++)
        {
            mapped[i] = ByteLevelAlphabet.ToChar(bytes[i]);
        }
        return new string(mapped);
    }

    [Fact]
    public void The_patterns_shipped_are_the_patterns_proven()
    {
        using JsonDocument doc = OracleLoader.Load("bpe_pretokenize.json");
        JsonElement patterns = doc.RootElement.GetProperty("metadata").GetProperty("patterns");

        Assert.Equal(patterns.GetProperty("gpt2").GetString(), BpePatterns.Gpt2);
        Assert.Equal(patterns.GetProperty("llama3").GetString(), BpePatterns.Llama3);
        Assert.Equal(patterns.GetProperty("qwen2").GetString(), BpePatterns.Qwen2);
    }

    /// <summary>
    /// Proves <see cref="BpePatterns.Whitespace"/> directly. It is otherwise only
    /// exercised transitively through <c>BpeTokenizerTests</c> (its oracle,
    /// <c>tiny_bpe.json</c>, is the only fixture in this branch declaring
    /// HuggingFace's <c>Whitespace</c> pre-tokenizer type) — a failure there would
    /// report from the wrong file.
    /// </summary>
    [Fact]
    public void The_whitespace_pattern_splits_on_word_boundaries_not_on_whitespace_alone()
    {
        var splitter = new BpePreTokenizer(null, BpePatterns.Whitespace, false, false);
        var pieces = new List<string>();
        splitter.Split("world!", pieces);
        Assert.Equal(["world", "!"], pieces);
    }

    [Fact]
    public void A_pathological_pattern_times_out_rather_than_hanging()
    {
        var splitter = new BpePreTokenizer(null, "(a+)+$", false, false);
        var pieces = new List<string>();
        Assert.Throws<RegexMatchTimeoutException>(
            () => splitter.Split(new string('a', 40) + "!", pieces));
    }
}
