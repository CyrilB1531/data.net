using System.Text;
using System.Text.Json;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tests.Persistence;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests;

/// <summary>
/// Replays <c>bpe_fuse_unk.json</c>: seven hand-built models, each recorded with
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
    /// which it cannot. Recomputed from <c>cases</c> rather than trusted from
    /// <c>metadata.fuse_pairs[].differs</c> — the value the generator computed —
    /// which is strictly stronger: it also catches a <c>cases</c>/<c>metadata</c>
    /// disagreement, following
    /// <see cref="BpeNoOpSettingsTests.Each_no_op_setting_encodes_exactly_like_its_baseline"/>.
    /// </summary>
    [Theory]
    [InlineData("in_piece_fused", true)]
    [InlineData("unk_merge_fused", true)]
    [InlineData("covered_unk_fused", true)]
    [InlineData("across_split_fused", false)]
    [InlineData("no_unk_fused", false)]
    [InlineData("byte_level_fused", false)]
    [InlineData("end_of_word_fused", true)]
    public void The_flag_changes_exactly_the_models_it_should(string fused, bool expected)
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        Dictionary<string, Dictionary<string, string>> streams = StreamsByModel(doc);

        string unfused = doc.RootElement.GetProperty("metadata").GetProperty("fuse_pairs")
            .EnumerateArray().Single(p => p.GetProperty("fused").GetString() == fused)
            .GetProperty("unfused").GetString()!;

        List<string> divergences = Divergences(streams, fused, unfused);

        Assert.Equal(expected, divergences.Count > 0);
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
        // "q" is a letter, so it and "Z" land in the same Whitespace piece and can sit next to each
        // other in one run -- needed to tell a real "q" apart from a "Z" substituted to "q".
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
        // The split these cases are written against: the theory below reasons about
        // what lands in one `Whitespace` piece, so the pattern is part of the claim.
        new(vocab, merges) { UnkToken = unk, FuseUnk = fuse, PreTokenizerPattern = BpePatterns.Whitespace };

    private static BpeVocabulary Vocabulary(JsonElement model)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(model.GetProperty("tokenizer_json").GetString()!));
        return TokenizerJsonLoader.LoadBpe(stream, OracleReplay.BpeBounds());
    }

    /// <summary>
    /// The recorded token stream of every case, keyed by model then by text, so a pair
    /// is compared text for text rather than by position in the cases array. Mirrors
    /// <see cref="BpeNoOpSettingsTests.StreamsByModel"/>.
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
            texts[c.GetProperty("text").GetString()!] = tokens;
        }
        return streams;
    }

    /// <summary>
    /// Where the fused model's recorded token streams differ from the unfused
    /// model's, text for text.
    /// </summary>
    private static List<string> Divergences(
        Dictionary<string, Dictionary<string, string>> streams, string fused, string unfused)
    {
        Assert.True(streams.TryGetValue(fused, out Dictionary<string, string>? withFusing), $"{Corpus} carries no case for model '{fused}'.");
        Assert.True(streams.TryGetValue(unfused, out Dictionary<string, string>? without), $"{Corpus} carries no case for model '{unfused}'.");

        Assert.Equal(
            without.Keys.OrderBy(k => k, StringComparer.Ordinal),
            withFusing.Keys.OrderBy(k => k, StringComparer.Ordinal));
        return [.. withFusing
            .Where(encoded => !string.Equals(encoded.Value, without[encoded.Key], StringComparison.Ordinal))
            .Select(encoded => $"{fused} \"{encoded.Key}\": {encoded.Value}, where {unfused} gives {without[encoded.Key]}")];
    }
}
