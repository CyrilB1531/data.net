using System.Text;
using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tests.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

/// <summary>
/// Replays <c>bpe_no_op_settings.json</c>: one model per setting issue #118 stopped
/// refusing, each beside a baseline built from the same vocabulary and merges with
/// the setting absent.
/// </summary>
/// <remarks>
/// <para>
/// The loader tests for <c>continuing_subword_prefix: ""</c> and <c>dropout: 0.0</c>
/// assert that a file loads. That proves nothing was thrown; it does not prove the
/// value is a no-op, which is the claim the acceptance rests on. This corpus is
/// where that claim is measured, on <c>tokenizers</c> 0.23.1 rather than on this
/// library's reading of it.
/// </para>
/// <para>
/// Two contrast pairs keep the equalities from being a property of a model too
/// small to notice: <c>end_of_word_suffix: "&lt;/w&gt;"</c> and the two
/// <c>add_prefix_space</c> spellings each change the token stream over the same
/// vocabulary.
/// </para>
/// </remarks>
public sealed class BpeNoOpSettingsTests
{
    private const string Corpus = "bpe_no_op_settings.json";

    /// <summary>The three positions a <c>ByteLevel</c> block can appear in, in corpus order.</summary>
    private static readonly string[] ByteLevelPositions = ["pre_tokenizer", "sequence_step", "decoder"];

    /// <summary>
    /// Every model the corpus carries, replayed through the loader against the exact
    /// bytes <c>tokenizers</c> was handed.
    /// </summary>
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

        // An empty models object would otherwise replay nothing at all.
        Assert.True(models > 0, $"{Corpus} carries no model.");
    }

    /// <summary>
    /// Replaying each case proves the C# matches Python. This proves the claim the
    /// acceptance rests on: Python itself produces the same tokens with the setting
    /// declared as it does with the setting absent.
    /// </summary>
    [Fact]
    public void Each_no_op_setting_encodes_exactly_like_its_baseline()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        Dictionary<string, Dictionary<string, string>> streams = StreamsByModel(doc);

        foreach ((string declared, string baseline) in Pairs(doc, "no_op_pairs"))
        {
            List<string> divergences = Divergences(streams, declared, baseline);

            Assert.True(divergences.Count == 0, string.Join("\n", divergences));
        }
    }

    /// <summary>
    /// The counterweight: over the same vocabulary, a non-empty end-of-word suffix and
    /// the two <c>add_prefix_space</c> spellings do change the token stream. Without
    /// this, every equality above would also hold for a model that ignored all three
    /// settings, and the corpus would prove nothing about the settings at all.
    /// </summary>
    [Fact]
    public void Each_contrast_pair_encodes_differently_from_its_baseline()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        Dictionary<string, Dictionary<string, string>> streams = StreamsByModel(doc);

        foreach ((string declared, string baseline) in Pairs(doc, "contrast_pairs"))
        {
            List<string> divergences = Divergences(streams, declared, baseline);

            Assert.True(divergences.Count > 0, $"{declared} encodes exactly like {baseline}, so this corpus cannot tell the setting from an ignored one.");
        }
    }

    /// <summary>
    /// The refusals issue #118 added, cited against the reference rather than against
    /// this repository's word: the corpus carries the exact document
    /// <c>tokenizers</c> was handed in each of the three positions a <c>ByteLevel</c>
    /// block can appear in, and the error it answered with.
    /// </summary>
    /// <remarks>
    /// Only the two pre-tokenizer positions name the field: the decoder is deserialized
    /// through an untagged enum, so Rust reports the variant that failed to match rather
    /// than the field that was missing. The cause is the same absent default, which is
    /// why the loader's own message names the field in all three.
    /// </remarks>
    [Fact]
    public void The_reference_refuses_every_byte_level_block_that_omits_add_prefix_space()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        var positions = new List<string>();

        foreach (JsonElement refusal in doc.RootElement.GetProperty("metadata").GetProperty("add_prefix_space_refusals").EnumerateArray())
        {
            positions.Add(refusal.GetProperty("position").GetString()!);
            Assert.NotEmpty(refusal.GetProperty("error").GetString()!);

            Exception error = Assert.ThrowsAny<Exception>(() => Load(refusal.GetProperty("document").GetString()!));
            Assert.Contains("add_prefix_space", error.Message, StringComparison.Ordinal);
        }

        Assert.Equal(ByteLevelPositions, positions);
    }

    private static IEnumerable<(string Declared, string Baseline)> Pairs(JsonDocument doc, string property)
    {
        JsonElement pairs = doc.RootElement.GetProperty("metadata").GetProperty(property);

        // A corpus regenerated without a pair would otherwise assert over nothing.
        Assert.True(pairs.GetArrayLength() > 0, $"{Corpus} carries no {property}.");
        return [.. pairs.EnumerateArray().Select(p =>
            (p.GetProperty("case").GetString()!, p.GetProperty("baseline").GetString()!))];
    }

    /// <summary>
    /// Where one model's recorded token streams differ from another's, text for text.
    /// Empty is what a no-op pair claims; non-empty is what a contrast pair claims.
    /// </summary>
    private static List<string> Divergences(
        Dictionary<string, Dictionary<string, string>> streams, string declared, string baseline)
    {
        Dictionary<string, string> withSetting = Recorded(streams, declared);
        Dictionary<string, string> without = Recorded(streams, baseline);

        Assert.Equal(
            without.Keys.OrderBy(k => k, StringComparer.Ordinal),
            withSetting.Keys.OrderBy(k => k, StringComparer.Ordinal));
        return [.. withSetting
            .Where(encoded => !string.Equals(encoded.Value, without[encoded.Key], StringComparison.Ordinal))
            .Select(encoded => $"{declared} \"{encoded.Key}\": {encoded.Value}, where {baseline} gives {without[encoded.Key]}")];
    }

    /// <summary>
    /// The recorded token stream of every case, keyed by model then by text, so a pair
    /// is compared text for text rather than by position in the cases array.
    /// </summary>
    private static Dictionary<string, Dictionary<string, string>> StreamsByModel(JsonDocument doc)
    {
        var streams = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string model = c.GetProperty("model").GetString()!;
            if (!streams.TryGetValue(model, out Dictionary<string, string>? texts))
            {
                texts = new Dictionary<string, string>(StringComparer.Ordinal);
                streams[model] = texts;
            }
            string tokens = string.Join(" ", c.GetProperty("tokens").EnumerateArray().Select(e => e.GetString()));
            string ids = string.Join(" ", c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()));
            texts[c.GetProperty("text").GetString()!] = $"[{tokens}] [{ids}]";
        }
        return streams;
    }

    /// <summary>
    /// One model's recorded streams. Named rather than indexed so a pair naming a model
    /// the corpus has no case for says that, instead of raising a key-not-found.
    /// </summary>
    private static Dictionary<string, string> Recorded(
        Dictionary<string, Dictionary<string, string>> streams, string model)
    {
        Assert.True(streams.TryGetValue(model, out Dictionary<string, string>? texts), $"{Corpus} carries no case for model '{model}'.");
        return texts!;
    }

    private static BpeVocabulary Vocabulary(JsonElement model) => Load(model.GetProperty("tokenizer_json").GetString()!);

    private static BpeVocabulary Load(string tokenizerJson)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(tokenizerJson));
        return TokenizerJsonLoader.LoadBpe(stream, OracleReplay.BpeBounds());
    }
}
