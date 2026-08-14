using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests.Tokenization;

/// <summary>
/// Replays <c>normalizer.json</c> against four models carrying four different
/// <c>precompiled_charsmap</c> blobs, including none at all. The corpus answers two questions
/// separately: <c>normalized</c> is the charsmap alone, frozen with the whitespace flags off;
/// <c>pieces</c>/<c>ids</c> are the whole pipeline, so a test replaying only the second could pass
/// with normalization and whitespace handling both wrong in ways that cancel out. Four blobs and not
/// one: <c>nmt_nfkc</c> from stock XLM-R, its case-folding variant, a hand-written three-rule map named
/// only <c>user_defined</c>, and <c>tiny_sp.model</c>, which has no charsmap and must leave every input
/// exactly as it was.
/// </summary>
public sealed class PrecompiledNormalizerTests
{
    private static readonly Dictionary<string, SentencePieceVocabulary> Loaded = [];

    private static SentencePieceVocabulary Vocabulary(string fixture)
    {
        lock (Loaded)
        {
            if (!Loaded.TryGetValue(fixture, out SentencePieceVocabulary? vocabulary))
            {
                vocabulary = SentencePieceModelLoader.Load(
                    Path.Combine(AppContext.BaseDirectory, "oracles", fixture));
                Loaded[fixture] = vocabulary;
            }
            return vocabulary;
        }
    }

    [Fact]
    public void Normalize_matches_sentencepiece()
    {
        using JsonDocument doc = OracleLoader.Load("normalizer.json");

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string fixture = c.GetProperty("model").GetString()!;
            string text = c.GetProperty("text").GetString()!;
            string expected = c.GetProperty("normalized").GetString()!;

            PrecompiledNormalizer? normalizer = Vocabulary(fixture).Normalizer;
            // No charsmap means no rewriting: the identity model's reference values are the inputs
            // themselves, worth asserting rather than skipping.
            string actual = normalizer is null ? text : normalizer.Normalize(text);
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                failures.Add($"{fixture} {Show(text)}\n  exp {Show(expected)}\n  got {Show(actual)}");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void Encode_matches_sentencepiece_through_the_whole_pipeline()
    {
        using JsonDocument doc = OracleLoader.Load("normalizer.json");
        var tokenizers = new Dictionary<string, SentencePieceTokenizer>(StringComparer.Ordinal);

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string fixture = c.GetProperty("model").GetString()!;
            string text = c.GetProperty("text").GetString()!;
            string[] expectedPieces = c.GetProperty("pieces").EnumerateArray().Select(e => e.GetString()!).ToArray();
            int[] expectedIds = c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();

            if (!tokenizers.TryGetValue(fixture, out SentencePieceTokenizer? tokenizer))
            {
                tokenizer = new SentencePieceTokenizer(Vocabulary(fixture));
                tokenizers[fixture] = tokenizer;
            }

            TokenizationResult actual = tokenizer.Encode(text);
            if (!expectedPieces.SequenceEqual(actual.Tokens) || !expectedIds.SequenceEqual(actual.Ids))
            {
                failures.Add(
                    $"{fixture} {Show(text)}\n  exp [{string.Join(", ", expectedPieces.Select(Show))}]\n" +
                    $"  got [{string.Join(", ", actual.Tokens.Select(Show))}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// The oracle covers the rules; this covers that they are read from the map
    /// rather than from the name. <c>custom_norm.model</c> calls itself
    /// <c>user_defined</c> and folds no case; the <c>_cf</c> model does.
    /// </summary>
    [Fact]
    public void The_rules_come_from_the_map_not_from_its_name()
    {
        Assert.Equal("mixed case", Vocabulary("nmt_nfkc_cf.model").Normalizer!.Normalize("MiXeD CaSe"));
        Assert.Equal("MiXeD CaSe", Vocabulary("custom_norm.model").Normalizer!.Normalize("MiXeD CaSe"));
        Assert.Equal("ss 1 ", Vocabulary("custom_norm.model").Normalizer!.Normalize("ß ① ¤"));
        Assert.Null(Vocabulary("tiny_sp.model").Normalizer);
    }

    [Fact]
    public void Two_vocabularies_read_from_the_same_file_carry_equal_normalizers()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "oracles", "custom_norm.model");
        PrecompiledNormalizer first = SentencePieceModelLoader.Load(path).Normalizer!;
        PrecompiledNormalizer second = SentencePieceModelLoader.Load(path).Normalizer!;

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, Vocabulary("nmt_nfkc_cf.model").Normalizer);
    }

    [Theory]
    [InlineData(new byte[] { 1, 2 }, "too short")]
    [InlineData(new byte[] { 5, 0, 0, 0, 1, 2, 3, 4, 5 }, "not a whole number")]
    [InlineData(new byte[] { 0xFC, 0, 0, 0, 1, 2, 3, 4 }, "carries only")]
    [InlineData(new byte[] { 0, 0, 0, 0 }, "empty trie")]
    public void A_malformed_charsmap_is_refused(byte[] charsMap, string expected)
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => PrecompiledNormalizer.FromCharsMap(charsMap));

        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    /// <summary>Renders control characters and the like readably in a failure message.</summary>
    private static string Show(string text) =>
        "\"" + string.Concat(text.Select(c => c switch
        {
            '\t' => "\\t",
            '\n' => "\\n",
            '\r' => "\\r",
            _ when char.IsControl(c) || char.IsWhiteSpace(c) && c != ' ' => $"\\u{(int)c:X4}",
            _ => c.ToString(),
        })) + "\"";
}
