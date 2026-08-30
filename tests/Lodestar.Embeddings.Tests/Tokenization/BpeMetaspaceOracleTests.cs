using System.Text;
using System.Text.Json;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tests.Persistence;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests.Tokenization;

/// <summary>
/// Replays <c>bpe_metaspace.json</c>: the whitespace escape a SentencePiece-BPE
/// file writes, in both of the spellings decision 0050 §2 makes one value.
/// </summary>
/// <remarks>
/// Six pipelines over one model whose merges are spelled with the meta symbol, so a
/// stream that reached a whole-word token proves the escape ran. The corpus carries
/// each pipeline's whole <c>tokenizer.json</c>, so what is loaded here is the bytes
/// <c>tokenizers</c> 0.23.1 was handed.
/// </remarks>
public sealed class BpeMetaspaceOracleTests
{
    private const string Corpus = "bpe_metaspace.json";

    private const char MetaSymbol = '\u2581';

    /// <summary>The Llama-2 spelling, which is the one the loader reproduces everywhere.</summary>
    private const string NormalizerCase = "prepend_replace_normalizer";

    /// <summary>
    /// The pipelines that prepend, and so meet the guard <c>Metaspace</c> applies and
    /// the normalizer sequence does not — see <see cref="Divergent"/>.
    /// </summary>
    private static readonly string[] PrependingMetaspaceCases =
        ["metaspace_first", "metaspace_always", "legacy_add_prefix_space"];

    /// <summary>
    /// Every pipeline and every text, exactly — bar the pairs <see cref="Divergent"/>
    /// names, which the test below measures instead.
    /// </summary>
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
                if (Divergent(name, text))
                {
                    continue;
                }
                replayed++;
                Compare(failures, name, text, Expected(t), tokenizer.Encode(text));
            }
        }

        Assert.True(replayed > 0, $"{Corpus} carries no case to replay.");
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// The recorded divergence, measured rather than skipped: on a text that already
    /// begins with the symbol, a <c>Metaspace</c> block prepends nothing and the
    /// <c>Prepend</c>+<c>Replace</c> sequence prepends anyway, and one
    /// <c>MetaspaceEscape</c> carries no guard to tell them apart. So the loader
    /// answers every prepending pipeline with the normalizer spelling's stream.
    /// docs/equivalence.md's <c>pre_tokenizers.Metaspace</c> row records it; this
    /// asserts it is still exactly that, so closing it fails here rather than silently.
    /// </summary>
    [Fact]
    public void The_prepend_guard_is_the_only_divergence_and_it_answers_as_the_normalizer_spelling()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        Dictionary<string, (string[] Tokens, int[] Ids)> normalizer = Streams(doc, NormalizerCase);

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
                if (!Divergent(name, text))
                {
                    continue;
                }
                measured++;
                TokenizationResult actual = tokenizer.Encode(text);
                Compare(failures, name, text, normalizer[text], actual);
                if (Same(Expected(t), actual))
                {
                    failures.Add($"[{name}] {Escape(text)}: the reference is reproduced now, so this pair is no longer a divergence — drop it from Divergent and from docs/equivalence.md.");
                }
            }
        }

        Assert.True(measured > 0, "No divergent pair was measured, so this test proves nothing.");
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
            bool agree = Same(pair.Value, normalizer[pair.Key]);
            if (agree == BeginsWithTheSymbol(pair.Key))
            {
                failures.Add($"{Escape(pair.Key)}: the two spellings {(agree ? "agree" : "differ")}, which is not what beginning with the symbol predicts.");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// Whether the loader is known not to reproduce this pair. A prepending
    /// <c>Metaspace</c> block skips its prepend when the escaped text already begins
    /// with the symbol; a text beginning with a space begins with it after the replace.
    /// </summary>
    private static bool Divergent(string name, string text) =>
        BeginsWithTheSymbol(text)
        && Array.Exists(PrependingMetaspaceCases, n => string.Equals(n, name, StringComparison.Ordinal));

    private static bool BeginsWithTheSymbol(string text) =>
        text.Length > 0 && (text[0] == ' ' || text[0] == MetaSymbol);

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
