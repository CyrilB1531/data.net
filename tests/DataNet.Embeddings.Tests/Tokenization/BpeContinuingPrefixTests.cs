using System.Text;
using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tests.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

/// <summary>
/// Replays <c>bpe_continuing_prefix.json</c>: nine hand-built models, each for
/// one thing no other tells apart.
/// </summary>
public sealed class BpeContinuingPrefixTests
{
    private const string Corpus = "bpe_continuing_prefix.json";

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
    /// The prefix belongs to the piece, not to the text: the first symbol of the
    /// second word is bare. Recomputed from the cases rather than asserted, so a
    /// corpus that stopped carrying the distinction fails here.
    /// </summary>
    [Fact]
    public void The_second_piece_starts_bare()
    {
        Assert.Equal(["a", "##b", "a", "##b"], Tokens("two_pieces", "ab ab"));
    }

    /// <summary>
    /// There is no fallback to the undecorated form: <c>b</c> is in the
    /// vocabulary and is not used, because a non-initial symbol is looked up
    /// only as <c>##b</c>.
    /// </summary>
    [Theory]
    [InlineData("no_prefixed_form", "ab", new[] { "a" })]
    [InlineData("no_prefixed_form_unk", "ab", new[] { "a", "[UNK]" })]
    public void A_missing_prefixed_form_does_not_fall_back(string model, string text, string[] expected)
    {
        Assert.Equal(expected, Tokens(model, text));
    }

    /// <summary>
    /// A merge's result is the left side plus the right side without its prefix.
    /// The left keeps its own: "both sides lose it" would give <c>bc</c>.
    /// </summary>
    [Theory]
    [InlineData("merge_stripped_result", "ab", new[] { "ab" })]
    [InlineData("merge_both_prefixed", "abc", new[] { "a", "##bc" })]
    public void A_merge_strips_the_prefix_from_its_right_side_only(
        string model, string text, string[] expected)
    {
        Assert.Equal(expected, Tokens(model, text));
    }

    /// <summary>Prefix then character then suffix, on a symbol that is both.</summary>
    [Fact]
    public void The_prefix_and_the_suffix_compose()
    {
        Assert.Equal(["a", "##b</w>"], Tokens("prefix_and_suffix", "ab"));
    }

    /// <summary>
    /// An empty prefix prefixes nothing, so it must be the model with no prefix
    /// at all — the path every existing file takes, unchanged.
    /// </summary>
    [Theory]
    [InlineData("ab")]
    [InlineData("a b")]
    public void An_empty_prefix_is_the_same_as_none(string text)
    {
        Assert.Equal(Tokens("no_prefix", text), Tokens("empty_prefix", text));
    }

    /// <summary>
    /// The shape the reference refuses to build: a merge whose two sides are
    /// prefixed and whose concatenated form is in the vocabulary rather than the
    /// stripped one. Cited against the reference, not against this repository's
    /// word — the corpus carries the document it was handed and what it answered.
    /// </summary>
    [Fact]
    public void The_reference_refuses_an_unstripped_merge_result_and_so_do_we()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);

        JsonElement refusal = doc.RootElement.GetProperty("metadata").GetProperty("refusals")
            .EnumerateArray().Single();
        Assert.NotEmpty(refusal.GetProperty("error").GetString()!);

        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(
            Bytes(refusal.GetProperty("document").GetString()!), OracleReplay.BpeBounds());

        ArgumentException error = Assert.ThrowsAny<ArgumentException>(() => new BpeTokenizer(vocabulary));
        Assert.Contains("##bc", error.Message, StringComparison.Ordinal);
    }

    /// <summary>A file declaring the prefix now loads, and carries it.</summary>
    [Fact]
    public void The_loader_carries_the_prefix_instead_of_refusing_it()
    {
        const string Json = """
        {"pre_tokenizer":{"type":"Whitespace"},
         "model":{"type":"BPE","vocab":{"a":0,"##b":1},"merges":[],
         "continuing_subword_prefix":"##"}}
        """;

        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds());

        Assert.Equal("##", vocabulary.ContinuingSubwordPrefix);
    }

    /// <summary>An empty prefix reads as no prefix, the way an empty suffix does.</summary>
    [Fact]
    public void An_empty_prefix_reads_as_absent()
    {
        const string Json = """
        {"pre_tokenizer":{"type":"Whitespace"},
         "model":{"type":"BPE","vocab":{"a":0},"merges":[],
         "continuing_subword_prefix":""}}
        """;

        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds());

        Assert.Null(vocabulary.ContinuingSubwordPrefix);
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
