using System.Text;
using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tests.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

/// <summary>
/// Replays <c>bpe_prefix_space.json</c>: where <c>add_prefix_space</c> lands once a
/// <c>Split</c> step runs before the <c>ByteLevel</c> one.
/// </summary>
public sealed class BpePrefixSpaceTests
{
    private const string Corpus = "bpe_prefix_space.json";

    /// <summary>
    /// The divergence as a user meets it: one space per piece, all of them
    /// surviving the round trip. DataNet used to return <c>" a|b|c|d"</c>.
    /// </summary>
    [Fact]
    public void Every_split_piece_carries_its_own_prefix_space_through_decode()
    {
        var tokenizer = new BpeTokenizer(Vocabulary("presplit_aps"));

        Assert.Equal(" a | b | c | d", tokenizer.Decode(tokenizer.Encode("a|b|c|d").Ids));
    }

    /// <summary>A piece that already begins with a space does not gain a second one.</summary>
    [Fact]
    public void A_piece_that_already_begins_with_a_space_gains_nothing()
    {
        var tokenizer = new BpeTokenizer(Vocabulary("presplit_aps"));

        Assert.Equal(" ab | cd", tokenizer.Decode(tokenizer.Encode("ab| cd").Ids));
    }

    /// <summary>
    /// GPT-2's shape, which this lot must not move: one space at the front,
    /// whatever the text contains.
    /// </summary>
    [Fact]
    public void A_bare_byte_level_still_takes_one_space_at_the_front()
    {
        var tokenizer = new BpeTokenizer(Vocabulary("bare_aps"));

        Assert.Equal(" a|b|c|d", tokenizer.Decode(tokenizer.Encode("a|b|c|d").Ids));
    }

    /// <summary>The four models declaring the space decode the text the <c>Split</c> never matches identically, and the fifth differs by exactly that space.</summary>
    /// <remarks>
    /// Which is what makes the tests above about placement rather than about the
    /// models differing in general. On <c>decoded</c> and not on <c>pieces</c>:
    /// the same four disagree there, <c>presplit_aps</c> and <c>no_split_aps</c>
    /// giving one piece where <c>presplit_aps_regex</c> and <c>bare_aps</c> give
    /// three -- on <c>use_regex</c> rather than on the split.
    /// </remarks>
    [Fact]
    public void The_models_declaring_a_prefix_space_agree_where_the_split_never_matches()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        Dictionary<string, string> decoded = doc.RootElement.GetProperty("cases").EnumerateArray()
            .Where(c => c.GetProperty("text").GetString() == "no split here")
            .ToDictionary(c => c.GetProperty("model").GetString()!, c => c.GetProperty("decoded").GetString()!);

        Assert.Equal(5, decoded.Count);
        Assert.Single(decoded.Where(e => e.Key != "presplit_no_aps").Select(e => e.Value)
            .Distinct(StringComparer.Ordinal));
        Assert.Equal(" " + decoded["presplit_no_aps"], decoded["presplit_aps"]);
        foreach (KeyValuePair<string, string> entry in decoded)
        {
            var tokenizer = new BpeTokenizer(Vocabulary(entry.Key));
            Assert.Equal(entry.Value, tokenizer.Decode(tokenizer.Encode("no split here").Ids));
        }
    }

    /// <summary>
    /// The corpus's own field, and the only one that separates <c>bare_aps</c>
    /// from <c>no_split_aps</c>: those two agree on <c>tokens</c> and <c>ids</c>
    /// for all seven texts and differ only in how many pieces the merge loop is
    /// handed.
    /// </summary>
    [Fact]
    public void The_pre_tokenizer_produces_the_pieces_the_corpus_records()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        var failures = new List<string>();
        int replayed = 0;

        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            replayed++;
            string model = c.GetProperty("model").GetString()!;
            string text = c.GetProperty("text").GetString()!;
            string[] expected = [.. c.GetProperty("pieces").EnumerateArray().Select(e => e.GetString()!)];
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

        Assert.Equal(5, models);
    }

    /// <summary>The pieces <paramref name="model"/> hands the merge loop, spelled as the corpus spells them.</summary>
    private static string[] Pieces(string model, string text)
    {
        BpeVocabulary vocabulary = Vocabulary(model);
        var pre = new BpePreTokenizer(
            vocabulary.PreSplit, vocabulary.PreTokenizerPattern, vocabulary.NoPreTokenizer, vocabulary.AddPrefixSpace);
        List<string> pieces = [];

        pre.Split(text, pieces);
        return [.. pieces.Select(ByteLevel)];
    }

    /// <summary>A raw-text piece through the byte alphabet, where the corpus reads a space as <c>Ġ</c>.</summary>
    private static string ByteLevel(string piece)
    {
        var mapped = new StringBuilder(piece.Length);
        foreach (byte value in Encoding.UTF8.GetBytes(piece))
        {
            mapped.Append(ByteLevelAlphabet.ToChar(value));
        }
        return mapped.ToString();
    }

    private static BpeVocabulary Vocabulary(string model)
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        return Vocabulary(doc.RootElement.GetProperty("metadata").GetProperty("models").GetProperty(model));
    }

    private static BpeVocabulary Vocabulary(JsonElement model)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(model.GetProperty("tokenizer_json").GetString()!));
        return TokenizerJsonLoader.LoadBpe(stream, OracleReplay.BpeBounds());
    }
}
