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

        var merges = new List<MergePair>();
        foreach (string line in File.ReadAllLines(Path.Combine(dir, "gpt2_merges.txt")))
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }
            int space = line.IndexOf(' ');
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
        Assert.Equal(50257, Gpt2Vocabulary().Count);
    }
}
