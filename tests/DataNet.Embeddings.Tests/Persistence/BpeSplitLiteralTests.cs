using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests.Persistence;

/// <summary>
/// Replays <c>bpe_split_literal.json</c>: a <c>Split</c> step spelling its pattern
/// <c>{"String": …}</c>, which is what the default Python call writes.
/// </summary>
public sealed class BpeSplitLiteralTests
{
    private const string Corpus = "bpe_split_literal.json";

    /// <summary>
    /// Every piece the corpus recorded, over all twelve models. The pieces carry
    /// the evidence, not the tokens: these models have no merges, so the token
    /// list is the text's bytes one by one wherever the boundaries fell, and a
    /// pattern that split in the wrong place would still emit them all.
    /// </summary>
    [Fact]
    public void Every_recorded_piece_is_reproduced()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        var failures = new List<string>();
        int replayed = 0;

        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            replayed++;
            string model = c.GetProperty("model").GetString()!;
            string text = c.GetProperty("text").GetString()!;
            string[] expected = [.. c.GetProperty("pieces").EnumerateArray().Select(p => p.GetString()!)];
            string[] actual = Pieces(model, text);

            if (!expected.SequenceEqual(actual))
            {
                failures.Add(
                    $"[{model}] \"{text}\"\n  exp: [{string.Join(", ", expected)}]\n  got: [{string.Join(", ", actual)}]");
            }
        }

        Assert.Equal(doc.RootElement.GetProperty("metadata").GetProperty("count").GetInt32(), replayed);
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// Every literal and its escaped twin, end to end, over every text the corpus
    /// carries. The corpus is the authority: where this disagrees, the code is wrong.
    /// </summary>
    [Fact]
    public void Encode_matches_tokenizers_for_every_model_the_corpus_carries()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);

        foreach (JsonProperty model in doc.RootElement.GetProperty("metadata").GetProperty("models").EnumerateObject())
        {
            var tokenizer = new BpeTokenizer(Vocabulary(model.Value));
            OracleReplay.AssertEncodings(doc, tokenizer.Encode, "tokens", model.Name, nameProperty: "model");
        }
    }

    /// <summary>
    /// The case that proves the literal was escaped rather than passed through:
    /// unescaped, <c>\d</c> would match the digit and take it out of its gap.
    /// </summary>
    /// <remarks>
    /// Asserted against what the corpus recorded, not against this library's own
    /// escaped model — comparing DataNet to DataNet would pass with no escape at
    /// all, since both sides would then be wrong the same way.
    /// </remarks>
    [Fact]
    public void A_backslash_d_literal_matches_two_characters_and_not_a_digit()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        JsonElement recorded = doc.RootElement.GetProperty("cases").EnumerateArray()
            .Single(c => c.GetProperty("model").GetString() == "backslash_d_literal"
                      && c.GetProperty("text").GetString() == "a\\db 7");
        string[] expected = [.. recorded.GetProperty("pieces").EnumerateArray().Select(p => p.GetString()!)];

        Assert.Equal(expected, Pieces("backslash_d_literal", "a\\db 7"));
    }

    /// <summary>A pattern node carrying neither spelling is refused, naming both.</summary>
    [Fact]
    public void A_pattern_declaring_neither_spelling_is_refused()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Refusal("pattern_empty"), OracleReplay.BpeBounds()));

        Assert.Contains("String", error.Message, StringComparison.Ordinal);
        Assert.Contains("Regex", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Both spellings at once is refused: the reference writes exactly one. Asserted
    /// on the wording, not only the type — a message naming one spelling is the very
    /// defect #167 closes, and the type alone would stay green through it.
    /// </summary>
    [Fact]
    public void A_pattern_declaring_both_spellings_is_refused()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Refusal("pattern_both"), OracleReplay.BpeBounds()));

        Assert.Contains("both pattern.Regex and pattern.String", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A null beside a literal is still both spellings. Keyed on the value being a
    /// string, this loaded as the escaped <c>\|</c>; keyed on the key being there, it
    /// is the refusal above, which is what the reference's Serde does with the node.
    /// </summary>
    [Fact]
    public void A_pattern_declaring_a_null_beside_a_literal_is_refused()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(
                DocumentWithPattern("{\"Regex\":null,\"String\":\"|\"}"), OracleReplay.BpeBounds()));

        Assert.Contains("both pattern.Regex and pattern.String", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The pieces the loaded model produces, mapped the way the corpus recorded
    /// them: <c>pre_tokenize_str</c> returns what the <c>ByteLevel</c> step made of
    /// each piece, while <see cref="BpePreTokenizer.Split"/> runs before any mapping.
    /// </summary>
    private static string[] Pieces(string model, string text)
    {
        BpeVocabulary vocabulary = Vocabulary(model);
        Assert.NotNull(vocabulary.PreSplit);

        List<string> pieces = [];
        new BpePreTokenizer(vocabulary.PreSplit, null, false, false).Split(text, pieces);
        return [.. pieces.Select(ByteMapped)];
    }

    private static string ByteMapped(string piece) =>
        new([.. Encoding.UTF8.GetBytes(piece).Select(ByteLevelAlphabet.ToChar)]);

    /// <summary>
    /// A working document with its <c>Split</c> step's <c>pattern</c> node replaced by
    /// the one the corpus recorded. <c>metadata.refusals</c> carries pattern nodes and
    /// no document, because these two shapes are DataNet's refusals: <c>tokenizers</c>
    /// builds neither, so there is no reference file to have captured.
    /// </summary>
    private static MemoryStream Refusal(string shape)
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        JsonElement pattern = doc.RootElement.GetProperty("metadata").GetProperty("refusals")
            .EnumerateArray().Single(r => r.GetProperty("shape").GetString() == shape)
            .GetProperty("pattern");

        return DocumentWithPattern(pattern.GetRawText());
    }

    private static MemoryStream DocumentWithPattern(string patternNode)
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        JsonNode document = JsonNode.Parse(ModelJson(doc, "pipe_literal"))!;

        document["pre_tokenizer"]!["pretokenizers"]![0]!["pattern"] = JsonNode.Parse(patternNode);
        return Bytes(document.ToJsonString());
    }

    private static string ModelJson(JsonDocument doc, string model) =>
        doc.RootElement.GetProperty("metadata").GetProperty("models")
            .GetProperty(model).GetProperty("tokenizer_json").GetString()!;

    private static BpeVocabulary Vocabulary(string model)
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        return TokenizerJsonLoader.LoadBpe(Bytes(ModelJson(doc, model)), OracleReplay.BpeBounds());
    }

    private static BpeVocabulary Vocabulary(JsonElement model) =>
        TokenizerJsonLoader.LoadBpe(
            Bytes(model.GetProperty("tokenizer_json").GetString()!), OracleReplay.BpeBounds());

    private static MemoryStream Bytes(string json) => new MemoryStream(Encoding.UTF8.GetBytes(json));
}
