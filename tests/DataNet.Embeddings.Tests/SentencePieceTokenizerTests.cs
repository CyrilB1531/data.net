using System.Text.Json;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

public sealed class SentencePieceTokenizerTests
{
    private static SentencePieceTokenizer BuildFromOracle(JsonDocument doc)
    {
        JsonElement meta = doc.RootElement.GetProperty("metadata");
        var vocab = new List<SentencePiece>();
        foreach (JsonElement e in meta.GetProperty("vocab").EnumerateArray())
        {
            vocab.Add(new SentencePiece(
                e.GetProperty("piece").GetString()!,
                e.GetProperty("score").GetDouble(),
                e.GetProperty("id").GetInt32()));
        }
        int unkId = meta.GetProperty("unk_id").GetInt32();
        return new SentencePieceTokenizer(vocab, unkId);
    }

    [Fact]
    public void Encode_matches_sentencepiece()
    {
        using JsonDocument doc = OracleLoader.Load("sentencepiece.json");
        SentencePieceTokenizer tokenizer = BuildFromOracle(doc);

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string text = c.GetProperty("text").GetString()!;
            string[] expectedPieces = c.GetProperty("pieces").EnumerateArray().Select(e => e.GetString()!).ToArray();
            int[] expectedIds = c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();

            TokenizationResult actual = tokenizer.Encode(text);
            if (!expectedPieces.SequenceEqual(actual.Tokens) || !expectedIds.SequenceEqual(actual.Ids))
            {
                failures.Add($"\"{text}\"\n  exp pieces: [{string.Join(", ", expectedPieces)}]\n  got pieces: [{string.Join(", ", actual.Tokens)}]\n  exp ids: [{string.Join(", ", expectedIds)}]\n  got ids: [{string.Join(", ", actual.Ids)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }
}
