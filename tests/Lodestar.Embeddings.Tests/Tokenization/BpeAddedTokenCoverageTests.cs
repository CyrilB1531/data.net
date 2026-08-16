using System.Text;
using System.Text.Json;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tests.Persistence;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests;

/// <summary>
/// Replays <c>bpe_added_token_coverage.json</c>: one model whose added token is
/// absent from <c>model.vocab</c>, carried once with <c>single_word</c> and once
/// without.
/// </summary>
public sealed class BpeAddedTokenCoverageTests
{
    private const string Corpus = "bpe_added_token_coverage.json";

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
    /// The corpus's own discriminating power, recomputed from the cases rather
    /// than read from a field the generator filled in: under
    /// <c>single_word</c> the scanner declines inside a word, and the streams
    /// must part company there and only there.
    /// </summary>
    [Theory]
    [InlineData("aQa", true)]
    [InlineData("ZQZ", true)]
    [InlineData("QQ", true)]
    [InlineData("aQ", true)]
    [InlineData("Q", false)]
    [InlineData("a Q a", false)]
    public void The_two_models_differ_exactly_where_the_scanner_declines(string text, bool expected)
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);

        string[] strict = Tokens(doc, "single_word", text);
        string[] loose = Tokens(doc, "any_position", text);

        Assert.Equal(expected, !strict.SequenceEqual(loose));
    }

    /// <summary>
    /// An added token the model does not declare is not a covered symbol: the
    /// character it spells is substituted, as the reference substitutes it.
    /// </summary>
    [Fact]
    public void An_added_token_outside_the_model_does_not_cover_its_character()
    {
        BpeTokenizer tokenizer = StrictModel();

        Assert.Equal(["a", "[UNK]", "a"], tokenizer.Encode("aQa").Tokens);
        Assert.Equal(["[UNK]", "[UNK]", "[UNK]"], tokenizer.Encode("ZQZ").Tokens);
    }

    /// <summary>
    /// And it is still a token: identity keeps the folded view, which is what
    /// <c>token_to_id</c> and <c>decode</c> report on the reference.
    /// </summary>
    [Fact]
    public void The_same_added_token_still_resolves_by_id_and_decodes()
    {
        BpeTokenizer tokenizer = StrictModel();

        Assert.True(tokenizer.TryGetId("Q", out int id));
        Assert.Equal(["Q"], tokenizer.Encode("a Q a").Tokens.ToArray()[1..2]);
        Assert.Equal("Q", tokenizer.Decode([id]));
    }

    private static BpeTokenizer StrictModel()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        return new BpeTokenizer(Vocabulary(
            doc.RootElement.GetProperty("metadata").GetProperty("models").GetProperty("single_word")));
    }

    private static string[] Tokens(JsonDocument doc, string model, string text)
    {
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            if (c.GetProperty("model").GetString() == model && c.GetProperty("text").GetString() == text)
            {
                return [.. c.GetProperty("tokens").EnumerateArray().Select(t => t.GetString()!)];
            }
        }
        throw new Xunit.Sdk.XunitException($"{Corpus} carries no case for {model} / {text}.");
    }

    private static BpeVocabulary Vocabulary(JsonElement model)
    {
        using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(model.GetProperty("tokenizer_json").GetString()!));
        return TokenizerJsonLoader.LoadBpe(stream, OracleReplay.BpeBounds());
    }

    /// <summary>
    /// Three shapes the reference refuses, cited against the reference rather
    /// than against this repository's word: the corpus carries the exact
    /// document it was handed and the error it answered with. Two are refused
    /// while the document is read; the third — an <c>unk_token</c> declared
    /// only in <c>added_tokens</c> — is refused only from <c>encode</c>, which
    /// is why Lodestar's refusal of that one, at construction, is earlier than
    /// the reference's own.
    /// </summary>
    [Theory]
    [InlineData("unk_only_in_added_tokens", "encode")]
    [InlineData("merge_names_an_added_token", "load")]
    [InlineData("merge_result_missing", "load")]
    public void The_reference_refuses_it_too_and_so_do_we(string shape, string raisedBy)
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);

        JsonElement refusal = doc.RootElement.GetProperty("metadata").GetProperty("refusals")
            .EnumerateArray().Single(r => r.GetProperty("shape").GetString() == shape);

        Assert.NotEmpty(refusal.GetProperty("error").GetString()!);
        Assert.Equal(raisedBy, refusal.GetProperty("raised_by").GetString());

        using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(refusal.GetProperty("document").GetString()!));
        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(stream, OracleReplay.BpeBounds());

        Assert.ThrowsAny<ArgumentException>(() => new BpeTokenizer(vocabulary));
    }

    /// <summary>
    /// A merge naming a token the model does not declare names it in the message,
    /// because a file with three hundred merges needs to know which one.
    /// </summary>
    [Fact]
    public void An_orphan_merge_names_itself()
    {
        var vocabulary = new BpeVocabulary(
            new Dictionary<string, int> { ["a"] = 0, ["b"] = 1, ["ab"] = 2 },
            [new MergePair("a", "b"), new MergePair("x", "y")])
        {
            // The classic split, so the merge table is the only thing wrong with this
            // vocabulary -- an undeclared pre-tokenizer would be refused ahead of it.
            PreTokenizerPattern = BpePatterns.Whitespace,
        };

        ArgumentException error = Assert.Throws<ArgumentException>(() => new BpeTokenizer(vocabulary));

        Assert.Contains(
            "The merge at rank 1 names 'x' and 'y', and the vocabulary does not contain both.",
            error.Message,
            StringComparison.Ordinal);
    }
}
