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

            // One tokenizer per pipeline, reused across its texts: the corpus carries
            // the file once per pipeline rather than once per text, which is what
            // keeps it near two megabytes instead of seventy-eight.
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
    /// What the round trip becomes once a normalizer is declared, for every text
    /// DataNet can actually decode. It does not return the input -- it returns the
    /// normalized input, in Python too -- so what is asserted is that it fails the
    /// same way, against the reference's own decoded string rather than against a
    /// rule this repository invented.
    /// </summary>
    /// <remarks>
    /// A case whose <c>decoded</c> field itself contains U+FFFD is excluded here and
    /// picked up by <see cref="Decode_throws_where_the_reference_is_lossy"/> instead
    /// -- see that test for why DataNet cannot reproduce those three.
    /// </remarks>
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
                if (expected.Contains('\uFFFD', StringComparison.Ordinal))
                {
                    continue;
                }

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
    /// The one known, pre-existing divergence a normalizer can expose: DataNet's
    /// byte-level <see cref="BpeTokenizer.Decode(IReadOnlyList{int}, bool)"/> throws
    /// <see cref="DecoderFallbackException"/> on a byte sequence that is not
    /// well-formed UTF-8, by that method's own documented contract -- while
    /// HuggingFace's decoder is lossy and substitutes U+FFFD instead. Measured
    /// against <c>tokenizers</c> 0.23.1 on this exact file:
    /// <c>tok.decode([256])</c> answers <c>'caf�'</c> rather than raising.
    /// </summary>
    /// <remarks>
    /// Nothing about a normalizer causes this: it is what happens to any added token
    /// whose content is not byte-level encodable end to end, and the pre-#121 added-
    /// token corpora never exercised one because <c>&lt;|endoftext|&gt;</c> is ASCII.
    /// Changing <c>Decode</c>'s exception contract is a public API change with its own
    /// issue and ADR, out of scope here -- this test pins the divergence instead of
    /// hiding it, so it fails the day <c>Decode</c> changes, which is when it needs
    /// revisiting.
    /// </remarks>
    [Fact]
    public void Decode_throws_where_the_reference_is_lossy()
    {
        using JsonDocument doc = OracleLoader.Load("bpe_normalizer.json");

        int divergentCount = 0;
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            BpeVocabulary vocab = TokenizerJsonLoader.LoadBpe(
                Bytes(c.GetProperty("tokenizer_json").GetString()!), OracleReplay.BpeBounds());
            var tokenizer = new BpeTokenizer(vocab);

            foreach (JsonElement t in c.GetProperty("texts").EnumerateArray())
            {
                if (!t.GetProperty("decoded").GetString()!.Contains('\uFFFD', StringComparison.Ordinal))
                {
                    continue;
                }

                divergentCount++;
                int[] ids = [.. tokenizer.Encode(t.GetProperty("text").GetString()!).Ids];
                Assert.Throws<DecoderFallbackException>(() => tokenizer.Decode(ids));
            }
        }

        // Pins the known set at three, all from the added tokens pipeline, so a
        // regenerated corpus that stops exercising this divergence, or starts
        // exercising a different one, is noticed here rather than passing silently.
        Assert.Equal(3, divergentCount);
    }

    /// <summary>
    /// Branch review of #121, finding 7: <see cref="string.Normalize(NormalizationForm)"/>
    /// throws <see cref="ArgumentException"/> on a lone (unpaired) UTF-16 surrogate --
    /// measured with a throwaway probe against every one of the four forms before this
    /// test was written, not assumed. It fires from <c>EncodeGap</c>'s call to
    /// <c>Normalize</c>, which runs before a byte-level model ever gets to re-encode the
    /// gap to UTF-8, so it preempts <see cref="EncoderFallbackException"/>
    /// rather than joining it. Reachable only once a normalizer is declared: with none,
    /// <c>Normalize</c> is never called and the lone surrogate reaches the byte-level path
    /// instead, as before this lot.
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
