using System.Text;
using System.Text.Json;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tests.Persistence;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests;

/// <summary>
/// Replays <c>bpe_sequence_split.json</c> end to end: one <c>Sequence</c> over
/// Llama-3's <c>Split</c> pattern, carried with its <c>ByteLevel</c> step's
/// <c>use_regex</c> on and off.
/// </summary>
public sealed class BpeSequenceSplitTests
{
    private const string Corpus = "bpe_sequence_split.json";

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
    /// The loader reads <c>use_regex</c> on a <c>Sequence</c>'s <c>ByteLevel</c>
    /// step, which it ignored entirely before issue #143: on, the step's own
    /// pattern is carried and re-splits; off, only the <c>Split</c> step's is.
    /// </summary>
    [Theory]
    [InlineData("use_regex_on", true)]
    [InlineData("use_regex_off", false)]
    public void The_loader_carries_the_second_pattern_only_when_use_regex_is_on(string model, bool carried)
    {
        BpeVocabulary vocabulary = Vocabulary(Model(model));

        Assert.NotNull(vocabulary.PreSplit);
        Assert.Equal(BpePatterns.Llama3, vocabulary.PreSplit.Pattern);
        Assert.Equal(SplitBehavior.Isolated, vocabulary.PreSplit.Behavior);
        Assert.False(vocabulary.PreSplit.Invert);
        Assert.Equal(carried ? BpePatterns.Gpt2 : null, vocabulary.PreTokenizerPattern);
    }

    /// <summary>
    /// The divergence itself, pinned as a literal rather than only replayed: an
    /// elision keeps its apostrophe attached under the <c>Split</c> pattern alone
    /// and loses it once GPT-2's runs too, which knows only the seven English
    /// contractions. Literal arrays measured from tests/oracles/bpe_sequence_split.json
    /// (its toy vocabulary merges only "'a"/"'ai" and "'h"/"'hu", so "aujourd" and
    /// "hui" are not whole tokens under either reading).
    /// </summary>
    [Fact]
    public void An_elision_is_split_at_the_apostrophe_when_the_second_pattern_runs()
    {
        Assert.Equal(
            ["a", "u", "j", "o", "u", "r", "d", "'", "h", "u", "i"],
            new BpeTokenizer(Vocabulary(Model("use_regex_on"))).Encode("aujourd'hui").Tokens);

        Assert.Equal(
            ["a", "u", "j", "o", "u", "r", "d", "'hu", "i"],
            new BpeTokenizer(Vocabulary(Model("use_regex_off"))).Encode("aujourd'hui").Tokens);
    }

    /// <summary>
    /// An English contraction is in GPT-2's list, so it must not move. Literal
    /// array from tests/oracles/bpe_sequence_split.json cases 6 and 15, which
    /// are themselves identical.
    /// </summary>
    [Fact]
    public void A_listed_contraction_is_the_same_under_both()
    {
        Assert.Equal(
            ["d", "o", "n", "'", "t"],
            new BpeTokenizer(Vocabulary(Model("use_regex_on"))).Encode("don't").Tokens);
        Assert.Equal(
            new BpeTokenizer(Vocabulary(Model("use_regex_on"))).Encode("don't").Tokens,
            new BpeTokenizer(Vocabulary(Model("use_regex_off"))).Encode("don't").Tokens);
    }

    private static JsonElement Model(string name)
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        return doc.RootElement.GetProperty("metadata").GetProperty("models").GetProperty(name).Clone();
    }

    private static BpeVocabulary Vocabulary(JsonElement model) =>
        TokenizerJsonLoader.LoadBpe(
            Bytes(model.GetProperty("tokenizer_json").GetString()!), OracleReplay.BpeBounds());

    private static MemoryStream Bytes(string json) => new MemoryStream(Encoding.UTF8.GetBytes(json));
}
