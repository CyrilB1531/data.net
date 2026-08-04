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
}
