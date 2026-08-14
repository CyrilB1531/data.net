using System.Text;
using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tests.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests.Tokenization;

/// <summary>
/// Replays <c>unicode_forms.json</c> — the answer to the one question issue
/// #121's D5 makes the gate for reproducing a normalization form at all.
/// </summary>
public sealed class BpeNormalizerTests
{
    /// <summary>
    /// Whether .NET's normalization tables agree with the ones tokenizers uses.
    /// This is the question the spec's D5 makes the gate for reproducing a form at
    /// all: .NET normalizes through the platform's Unicode tables and Rust through
    /// its own crate, so agreement is measurable but not assumable, and a form that
    /// disagreed would have to be refused rather than reproduced wrongly.
    /// </summary>
    [Fact]
    public void The_four_forms_agree_with_the_reference_character_for_character()
    {
        using JsonDocument doc = OracleLoader.Load("unicode_forms.json");

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string form = c.GetProperty("form").GetString()!;
            string text = c.GetProperty("text").GetString()!;
            string expected = c.GetProperty("normalized").GetString()!;

            string actual = text.Normalize(form switch
            {
                "NFC" => NormalizationForm.FormC,
                "NFKC" => NormalizationForm.FormKC,
                "NFD" => NormalizationForm.FormD,
                "NFKD" => NormalizationForm.FormKD,
                _ => throw new InvalidOperationException($"the corpus names a form this test does not know: {form}"),
            });

            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                failures.Add($"[{form}] {Escape(text)}: expected {Escape(expected)}, got {Escape(actual)}");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// Every pipeline in the frozen corpus: the four forms, a Sequence, an empty
    /// Sequence, and a normalizer beside both halves of the added-token table.
    /// </summary>
    [Fact]
    public void Encode_reproduces_every_normalizer_pipeline()
    {
        using JsonDocument doc = OracleLoader.Load("bpe_normalizer.json");

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;

            // One tokenizer per pipeline: the corpus carries the file once per
            // pipeline rather than once per text, which is what keeps it small.
            BpeVocabulary vocab = TokenizerJsonLoader.LoadBpe(
                Bytes(c.GetProperty("tokenizer_json").GetString()!), OracleReplay.BpeBounds());
            var tokenizer = new BpeTokenizer(vocab);

            foreach (JsonElement t in c.GetProperty("texts").EnumerateArray())
            {
                string text = t.GetProperty("text").GetString()!;
                int[] expectedIds = [.. t.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32())];
                string[] expectedTokens = [.. t.GetProperty("tokens").EnumerateArray().Select(e => e.GetString()!)];
                TokenizationResult result = tokenizer.Encode(text);
                int[] actualIds = [.. result.Ids];
                string[] actualTokens = [.. result.Tokens];

                if (!expectedIds.SequenceEqual(actualIds))
                {
                    failures.Add($"[{name}] {Escape(text)}: expected ids [{string.Join(", ", expectedIds)}], got [{string.Join(", ", actualIds)}]");
                }
                if (!expectedTokens.SequenceEqual(actualTokens, StringComparer.Ordinal))
                {
                    failures.Add($"[{name}] {Escape(text)}: expected tokens [{string.Join(", ", expectedTokens.Select(Escape))}], got [{string.Join(", ", actualTokens.Select(Escape))}]");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// The round trip returns the normalized input, not the original, once a
    /// normalizer is declared -- matched against the reference's own decoded string.
    /// </summary>
    [Fact]
    public void Decode_returns_what_the_reference_returns_normalizer_included()
    {
        using JsonDocument doc = OracleLoader.Load("bpe_normalizer.json");

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            BpeVocabulary vocab = TokenizerJsonLoader.LoadBpe(
                Bytes(c.GetProperty("tokenizer_json").GetString()!), OracleReplay.BpeBounds());
            var tokenizer = new BpeTokenizer(vocab);

            foreach (JsonElement t in c.GetProperty("texts").EnumerateArray())
            {
                string expected = t.GetProperty("decoded").GetString()!;
                string actual = tokenizer.Decode([.. tokenizer.Encode(t.GetProperty("text").GetString()!).Ids]);

                if (!string.Equals(expected, actual, StringComparison.Ordinal))
                {
                    failures.Add($"[{name}] expected {Escape(expected)}, got {Escape(actual)}");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// Branch review of #121, finding 7: <see cref="string.Normalize(NormalizationForm)"/>
    /// throws <see cref="ArgumentException"/> on a lone UTF-16 surrogate -- measured
    /// against all four forms with a throwaway probe. It preempts
    /// <see cref="EncoderFallbackException"/>, since <c>EncodeGap</c> normalizes before
    /// a byte-level model re-encodes to UTF-8. Reachable only once a normalizer is
    /// declared: with none, <c>Normalize</c> is never called.
    /// </summary>
    [Fact]
    public void Encode_throws_ArgumentException_on_a_lone_surrogate_once_a_normalizer_is_declared()
    {
        const string json = """
            {"version":"1.0","normalizer":{"type":"NFC"},
             "pre_tokenizer":{"type":"ByteLevel","add_prefix_space":false},
             "decoder":{"type":"ByteLevel","add_prefix_space":false},
             "model":{"type":"BPE","vocab":{"a":0,"b":1},"merges":[]}}
            """;
        BpeVocabulary vocab = TokenizerJsonLoader.LoadBpe(Bytes(json), OracleReplay.BpeBounds());
        var tokenizer = new BpeTokenizer(vocab);

        Assert.Throws<ArgumentException>(() => tokenizer.Encode("a\uD800b"));
    }

    /// <summary>Renders a string as its code points, so a failure names what differs rather than showing two identical-looking lines.</summary>
    private static string Escape(string text) =>
        string.Concat(text.Select(ch => ch < 0x20 || ch > 0x7e ? $"\\u{(int)ch:x4}" : ch.ToString()));

    private static MemoryStream Bytes(string json) => new MemoryStream(Encoding.UTF8.GetBytes(json));
}
