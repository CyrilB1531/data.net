using System.Text;
using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tests.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

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
}
