using System.Text.Json;
using System.Text.RegularExpressions;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests.Tokenization;

public sealed class BpePreTokenizeTests
{
    /// <summary>
    /// The split is claimed for three model families but the vocabulary is
    /// vendored for one (ADR 0017), so this is the test carrying the Llama-3 and
    /// Qwen2 rows of the parity table. It compares the split output itself,
    /// before any merging, which is exactly what those rows promise.
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

            var splitter = new BpePreTokenizer(patterns.GetProperty(name).GetString());
            pieces.Clear();
            splitter.Split(text, pieces);

            if (!expected.SequenceEqual(pieces))
            {
                failures.Add($"[{name}] {JsonSerializer.Serialize(text)}\n  exp: [{string.Join(" | ", expected)}]\n  got: [{string.Join(" | ", pieces)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
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

    [Fact]
    public void A_pathological_pattern_times_out_rather_than_hanging()
    {
        var splitter = new BpePreTokenizer("(a+)+$");
        var pieces = new List<string>();
        Assert.Throws<RegexMatchTimeoutException>(
            () => splitter.Split(new string('a', 40) + "!", pieces));
    }
}
