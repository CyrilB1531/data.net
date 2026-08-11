using System.Text;
using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tests.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

/// <summary>
/// Replays <c>bpe_fuse_unk.json</c>: six hand-built models, each recorded with
/// <c>fuse_unk</c> off and on.
/// </summary>
public sealed class BpeFuseUnkTests
{
    private const string Corpus = "bpe_fuse_unk.json";

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
    /// The corpus's own claim about itself: which models the flag changes and
    /// which it cannot. Without this, every model could be a shape too small for
    /// the flag to matter and the replay above would still pass.
    /// </summary>
    [Theory]
    [InlineData("in_piece_fused", true)]
    [InlineData("unk_merge_fused", true)]
    [InlineData("covered_unk_fused", true)]
    [InlineData("across_split_fused", false)]
    [InlineData("no_unk_fused", false)]
    [InlineData("byte_level_fused", false)]
    public void The_flag_changes_exactly_the_models_it_should(string fused, bool expected)
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);

        JsonElement pair = doc.RootElement.GetProperty("metadata").GetProperty("fuse_pairs")
            .EnumerateArray().Single(p => p.GetProperty("fused").GetString() == fused);

        Assert.Equal(expected, pair.GetProperty("differs").GetBoolean());
    }

    /// <summary>
    /// The order of fusing and merging, which nothing else can see: with a merge
    /// whose left side is the unknown token, a fused run reaches <c>[UNK]a</c>.
    /// Fusing after the merge loop would give <c>[UNK]</c> then <c>a</c>.
    /// </summary>
    [Fact]
    public void A_fused_run_merges_with_what_follows_it()
    {
        BpeVocabulary vocabulary = Build(
            new Dictionary<string, int> { ["[UNK]"] = 0, ["a"] = 1, ["[UNK]a"] = 2 },
            [new MergePair("[UNK]", "a")],
            unk: "[UNK]",
            fuse: true);

        Assert.Equal(["[UNK]a"], new BpeTokenizer(vocabulary).Encode("ZZa").Tokens);
    }

    /// <summary>
    /// The trigger is that the character was substituted, not that the id equals
    /// the unknown id. They differ whenever the unknown token is itself covered.
    /// </summary>
    [Theory]
    [InlineData("qZ", new[] { "q", "q" })]
    [InlineData("Zq", new[] { "q", "q" })]
    [InlineData("qq", new[] { "q", "q" })]
    [InlineData("ZZ", new[] { "q" })]
    public void A_covered_unknown_token_does_not_fuse_with_a_substitution(string text, string[] expected)
    {
        // "q" is a letter, not punctuation, so it and "Z" land in the same
        // `Whitespace` piece and can sit next to each other in one run —
        // which the test needs, to tell a real "q" apart from a "Z"
        // substituted to "q".
        BpeVocabulary vocabulary = Build(
            new Dictionary<string, int> { ["q"] = 0, ["a"] = 1 }, [], unk: "q", fuse: true);

        Assert.Equal(expected, new BpeTokenizer(vocabulary).Encode(text).Tokens);
    }

    /// <summary>
    /// <c>Vocab</c> and <c>Merges</c> are the record's positional parameters;
    /// everything else is an initializer property.
    /// </summary>
    private static BpeVocabulary Build(
        Dictionary<string, int> vocab, IReadOnlyList<MergePair> merges, string? unk, bool fuse) =>
        new(vocab, merges) { UnkToken = unk, FuseUnk = fuse };

    private static BpeVocabulary Vocabulary(JsonElement model) =>
        TokenizerJsonLoader.LoadBpe(
            new MemoryStream(Encoding.UTF8.GetBytes(model.GetProperty("tokenizer_json").GetString()!)));
}
