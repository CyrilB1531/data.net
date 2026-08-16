using System.Text;
using System.Text.Json;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tests.Persistence;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests;

/// <summary>
/// Replays <c>bpe_duplicate_merge.json</c>: a merge table naming the same pair
/// twice, beside the two tables that keep one occurrence each.
/// </summary>
public sealed class BpeDuplicateMergeTests
{
    private const string Corpus = "bpe_duplicate_merge.json";

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
    /// The measurement issue #160 exists for: the last occurrence wins, and it
    /// takes the last rank. Before this, <c>BpeTokenizer</c> kept the first.
    /// </summary>
    [Fact]
    public void The_last_occurrence_of_a_repeated_pair_wins()
    {
        Assert.Equal(Tokens("last_kept", "abcd"), Tokens("duplicate", "abcd"));
        Assert.NotEqual(Tokens("first_kept", "abcd"), Tokens("duplicate", "abcd"));
    }

    /// <summary>
    /// Pinned as a literal too, so a corpus that stopped carrying the
    /// distinction fails here rather than silently asserting less.
    /// </summary>
    [Fact]
    public void The_duplicate_merges_b_and_c_before_a_and_b()
    {
        Assert.Equal(["a", "bc", "d"], Tokens("duplicate", "abcd"));
        Assert.Equal(["ab", "cd"], Tokens("first_kept", "abcd"));
    }

    /// <summary>
    /// The control: where the rank ordering decides nothing, the two readings
    /// agree — so the tests above are measuring the ordering and not some other
    /// difference between the two tables.
    /// </summary>
    [Fact]
    public void The_two_readings_agree_where_rank_order_decides_nothing()
    {
        Assert.Equal(Tokens("first_kept", "ab"), Tokens("last_kept", "ab"));
    }

    private static string[] Tokens(string model, string text)
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        var tokenizer = new BpeTokenizer(Vocabulary(
            doc.RootElement.GetProperty("metadata").GetProperty("models").GetProperty(model)));
        return [.. tokenizer.Encode(text).Tokens];
    }

    private static BpeVocabulary Vocabulary(JsonElement model) =>
        TokenizerJsonLoader.LoadBpe(
            Bytes(model.GetProperty("tokenizer_json").GetString()!), OracleReplay.BpeBounds());

    private static MemoryStream Bytes(string json) => new MemoryStream(Encoding.UTF8.GetBytes(json));
}
