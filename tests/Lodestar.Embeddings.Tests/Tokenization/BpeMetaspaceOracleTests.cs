using System.Text;
using System.Text.Json;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tests.Persistence;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests.Tokenization;

/// <summary>
/// Replays <c>bpe_metaspace.json</c>: the whitespace escape a SentencePiece-BPE
/// file writes, in both of the spellings decisions 0050 §2 and 0062 govern.
/// </summary>
/// <remarks>
/// Six pipelines over one model whose merges are spelled with the meta symbol, so a
/// stream that reached a whole-word token proves the escape ran. The corpus carries
/// each pipeline's whole <c>tokenizer.json</c>, so what is loaded here is the bytes
/// <c>tokenizers</c> 0.23.1 was handed — and every pipeline is reproduced exactly.
/// </remarks>
public sealed class BpeMetaspaceOracleTests
{
    private const string Corpus = "bpe_metaspace.json";

    private const char MetaSymbol = '\u2581';

    /// <summary>The special token both target models declare, and the corpus's own piece boundary.</summary>
    private const string AddedToken = "<s>";

    /// <summary>The Llama-2 spelling, whose prepend is unconditional.</summary>
    private const string NormalizerCase = "prepend_replace_normalizer";

    /// <summary>
    /// The pipelines that prepend, and so meet the guard <c>Metaspace</c> applies and
    /// the normalizer sequence does not.
    /// </summary>
    private static readonly string[] PrependingMetaspaceCases =
        ["metaspace_first", "metaspace_always", "legacy_add_prefix_space"];

    /// <summary>Every pipeline and every text, exactly.</summary>
    [Fact]
    public void Encode_reproduces_every_metaspace_pipeline()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);

        var failures = new List<string>();
        int replayed = 0;
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            var tokenizer = new BpeTokenizer(Vocabulary(c));

            foreach (JsonElement t in c.GetProperty("texts").EnumerateArray())
            {
                string text = t.GetProperty("text").GetString()!;
                replayed++;
                Compare(failures, name, text, Expected(t), tokenizer.Encode(text));
            }
        }

        Assert.True(replayed > 0, $"{Corpus} carries no case to replay.");
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// The guard is load-bearing: on a text that already begins with the symbol, the two
    /// spellings are two values, and the loader answers each with its own. Test one
    /// already proves each matches its reference; this proves they are not the same
    /// stream, so a guard quietly dropped fails here as well as there.
    /// </summary>
    [Fact]
    public void The_two_spellings_are_reproduced_apart_on_a_text_that_already_begins_with_the_symbol()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        BpeTokenizer normalizer = Tokenizer(doc, NormalizerCase);

        var failures = new List<string>();
        int measured = 0;
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            if (!Array.Exists(PrependingMetaspaceCases, n => string.Equals(n, name, StringComparison.Ordinal)))
            {
                continue;
            }
            var tokenizer = new BpeTokenizer(Vocabulary(c));

            foreach (JsonElement t in c.GetProperty("texts").EnumerateArray())
            {
                string text = t.GetProperty("text").GetString()!;
                if (!BeginsWithTheSymbol(text) || CarriesTheAddedToken(text))
                {
                    continue;
                }
                measured++;
                if (Same(Pair(tokenizer.Encode(text)), Pair(normalizer.Encode(text))))
                {
                    failures.Add($"[{name}] {Escape(text)}: the two spellings produced one stream, so the prepend guard did not run.");
                }
            }
        }

        Assert.True(measured > 0, "No guarded pair was measured, so this test proves nothing.");
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// Decision 0050 §2's premise, as <c>tokenizers</c> itself answers it: the two
    /// spellings are one value on every text that does not already begin with the
    /// symbol, and two values on every text that does. The corpus alone settles this —
    /// no Lodestar type is involved — which is what makes it the boundary the loader
    /// has to live with rather than one it chose.
    /// </summary>
    [Fact]
    public void The_two_spellings_agree_on_every_text_that_does_not_already_begin_with_the_symbol()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        Dictionary<string, (string[] Tokens, int[] Ids)> metaspace = Streams(doc, "metaspace_first");
        Dictionary<string, (string[] Tokens, int[] Ids)> normalizer = Streams(doc, NormalizerCase);

        var failures = new List<string>();
        foreach (KeyValuePair<string, (string[] Tokens, int[] Ids)> pair in metaspace)
        {
            if (CarriesTheAddedToken(pair.Key))
            {
                // With a token in the text the prepend scheme parts too, which the test
                // below measures — this one is about the guard alone.
                continue;
            }
            bool agree = Same(pair.Value, normalizer[pair.Key]);
            if (agree == BeginsWithTheSymbol(pair.Key))
            {
                failures.Add($"{Escape(pair.Key)}: the two spellings {(agree ? "agree" : "differ")}, which is not what beginning with the symbol predicts.");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// <c>first</c> prepends to the opening piece and <c>always</c> to every one, and an
    /// added token is a piece — so a token standing before a gap is what tells the two
    /// schemes apart on a model that splits at nothing else. The corpus answers it with
    /// no Lodestar type involved; test one is what proves the loader agrees.
    /// </summary>
    [Fact]
    public void An_added_token_before_a_gap_is_what_tells_first_from_always()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        Dictionary<string, (string[] Tokens, int[] Ids)> first = Streams(doc, "metaspace_first");
        Dictionary<string, (string[] Tokens, int[] Ids)> always = Streams(doc, "metaspace_always");

        var failures = new List<string>();
        int measured = 0;
        foreach (KeyValuePair<string, (string[] Tokens, int[] Ids)> pair in first)
        {
            if (!CarriesTheAddedToken(pair.Key))
            {
                continue;
            }
            measured++;

            // The one text where they still agree opens on the token and then on a space:
            // "always" would prepend, and the guard takes it back.
            bool expected = string.Equals(pair.Key, AddedToken + " the cat", StringComparison.Ordinal);
            if (Same(pair.Value, always[pair.Key]) != expected)
            {
                failures.Add($"{Escape(pair.Key)}: first and always {(expected ? "should agree here and do not" : "agree, so the scheme is not read per piece")}.");
            }
        }

        Assert.True(measured > 0, "No text carrying the added token was measured, so this test proves nothing.");
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// Whether a prepending <c>Metaspace</c> block's guard fires on this text: it reads
    /// the text after the replace, so a leading space counts as much as a leading symbol.
    /// </summary>
    private static bool BeginsWithTheSymbol(string text) =>
        text.Length > 0 && (text[0] == ' ' || text[0] == MetaSymbol);

    private static bool CarriesTheAddedToken(string text) =>
        text.Contains(AddedToken, StringComparison.Ordinal);

    private static void Compare(
        List<string> failures, string name, string text, (string[] Tokens, int[] Ids) expected, TokenizationResult actual)
    {
        if (Same(expected, actual))
        {
            return;
        }
        failures.Add(
            $"[{name}] {Escape(text)}\n  exp tokens: [{string.Join(", ", expected.Tokens.Select(Escape))}]\n  got tokens: [{string.Join(", ", actual.Tokens.Select(Escape))}]" +
            $"\n  exp ids: [{string.Join(", ", expected.Ids)}]\n  got ids: [{string.Join(", ", actual.Ids)}]");
    }

    private static bool Same((string[] Tokens, int[] Ids) expected, TokenizationResult actual) =>
        expected.Tokens.SequenceEqual(actual.Tokens, StringComparer.Ordinal) && expected.Ids.SequenceEqual(actual.Ids);

    private static bool Same((string[] Tokens, int[] Ids) left, (string[] Tokens, int[] Ids) right) =>
        left.Tokens.SequenceEqual(right.Tokens, StringComparer.Ordinal) && left.Ids.SequenceEqual(right.Ids);

    /// <summary>One encoded result as the pair the comparisons take.</summary>
    private static (string[] Tokens, int[] Ids) Pair(TokenizationResult result) =>
        ([.. result.Tokens], [.. result.Ids]);

    /// <summary>The tokenizer one named case's <c>tokenizer.json</c> loads to.</summary>
    private static BpeTokenizer Tokenizer(JsonDocument doc, string name)
    {
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            if (string.Equals(c.GetProperty("name").GetString(), name, StringComparison.Ordinal))
            {
                return new BpeTokenizer(Vocabulary(c));
            }
        }
        throw new InvalidOperationException($"{Corpus} carries no case named '{name}'.");
    }

    /// <summary>The text-to-stream map one named case carries, so two cases can be compared text by text.</summary>
    private static Dictionary<string, (string[] Tokens, int[] Ids)> Streams(JsonDocument doc, string name)
    {
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            if (!string.Equals(c.GetProperty("name").GetString(), name, StringComparison.Ordinal))
            {
                continue;
            }
            var streams = new Dictionary<string, (string[], int[])>(StringComparer.Ordinal);
            foreach (JsonElement t in c.GetProperty("texts").EnumerateArray())
            {
                streams[t.GetProperty("text").GetString()!] = Expected(t);
            }
            return streams;
        }
        throw new InvalidOperationException($"{Corpus} carries no case named '{name}'.");
    }

    private static (string[] Tokens, int[] Ids) Expected(JsonElement text) =>
        ([.. text.GetProperty("tokens").EnumerateArray().Select(e => e.GetString()!)],
         [.. text.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32())]);

    private static BpeVocabulary Vocabulary(JsonElement testCase) =>
        TokenizerJsonLoader.LoadBpe(
            new MemoryStream(Encoding.UTF8.GetBytes(testCase.GetProperty("tokenizer_json").GetString()!)),
            OracleReplay.BpeBounds());

    /// <summary>Renders a string as its code points, so a failure names what differs rather than showing two identical-looking lines.</summary>
    private static string Escape(string text) =>
        string.Concat(text.Select(ch => ch < 0x20 || ch > 0x7e ? $"\\u{(int)ch:x4}" : ch.ToString()));
}
