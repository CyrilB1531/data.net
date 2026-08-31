using System.Text;
using System.Text.Json;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tests.Persistence;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests.Tokenization;

/// <summary>
/// Replays <c>bpe_byte_fallback.json</c>: an uncovered symbol resolving into byte pieces,
/// in every shape decision 0063 states the rule has.
/// </summary>
/// <remarks>
/// Ten pipelines over one model carrying all 256 pieces, so no symbol falls to the
/// unknown token and the reference's own ordering defect is out of reach. The corpus
/// carries each pipeline's whole <c>tokenizer.json</c>, so what is loaded here is the
/// bytes <c>tokenizers</c> 0.23.1 was handed.
/// </remarks>
public sealed class BpeByteFallbackOracleTests
{
    private const string Corpus = "bpe_byte_fallback.json";

    /// <summary>The corpus's own case count, pinned so a silently shrunken corpus fails here rather than passing on whatever survived.</summary>
    private const int ExpectedCases = 10;

    /// <summary>Every case carries the same seven-text fixture; see <c>BYTE_FALLBACK_TEXTS</c> in the generator.</summary>
    private const int TextsPerCase = 7;

    /// <summary><c>decoder_byte_fallback</c> and <c>decoder_sequence</c>, the only two cases carrying a <c>decoded</c> column.</summary>
    private const int DecodedRows = 2 * TextsPerCase;

    /// <summary>The raw id runs those same two cases carry; see <c>BYTE_FALLBACK_DECODE_RUNS</c> in the generator.</summary>
    private const int DecodeRunRows = 2 * 6;

    /// <summary>Every pipeline and every text, exactly — and the corpus's own shape, so a shrunken corpus fails rather than passing on fewer rows.</summary>
    [Fact]
    public void Encode_reproduces_every_byte_fallback_pipeline()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);
        Assert.Equal(ExpectedCases, doc.RootElement.GetProperty("metadata").GetProperty("count").GetInt32());

        var failures = new List<string>();
        int cases = 0;
        int replayed = 0;
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            cases++;
            string name = c.GetProperty("name").GetString()!;
            var tokenizer = new BpeTokenizer(Vocabulary(c));

            foreach (JsonElement t in c.GetProperty("texts").EnumerateArray())
            {
                replayed++;
                string text = t.GetProperty("text").GetString()!;
                TokenizationResult actual = tokenizer.Encode(text);
                string[] expected = [.. t.GetProperty("tokens").EnumerateArray().Select(e => e.GetString()!)];
                int[] expectedIds = [.. t.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32())];
                if (!expected.SequenceEqual(actual.Tokens, StringComparer.Ordinal) || !expectedIds.SequenceEqual(actual.Ids))
                {
                    failures.Add($"[{name}] {Escape(text)}\n  exp: [{string.Join(", ", expected.Select(Escape))}]\n  got: [{string.Join(", ", actual.Tokens.Select(Escape))}]");
                }
            }
        }

        Assert.Equal(ExpectedCases, cases);
        Assert.Equal(ExpectedCases * TextsPerCase, replayed);
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// The two cases that declare a decoder, which is what makes a decoded column
    /// meaningful here where the metaspace corpus could not carry one.
    /// </summary>
    [Fact]
    public void Decode_reproduces_the_declared_chains()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);

        var failures = new List<string>();
        int decoded = 0;
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            var tokenizer = new BpeTokenizer(Vocabulary(c));

            foreach (JsonElement t in c.GetProperty("texts").EnumerateArray())
            {
                if (!t.TryGetProperty("decoded", out JsonElement expected))
                {
                    continue;
                }
                decoded++;
                string text = t.GetProperty("text").GetString()!;
                string actual = tokenizer.Decode(tokenizer.Encode(text).Ids);
                if (!string.Equals(expected.GetString(), actual, StringComparison.Ordinal))
                {
                    failures.Add($"[{name}] {Escape(text)}: expected {Escape(expected.GetString()!)}, got {Escape(actual)}");
                }
            }
        }

        Assert.Equal(DecodedRows, decoded);
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>The raw id runs, which <c>Encode</c> cannot produce and a truncated generation does.</summary>
    /// <remarks>
    /// A run cut mid-character is where the reference's one-U+FFFD-per-byte rule parts from a
    /// decoder substituting once per maximal invalid subpart, so these rows are the ones a
    /// shared lossy helper would pass this suite while getting wrong. The corpus is the
    /// answer here, not an expectation written beside the code it checks.
    /// </remarks>
    [Fact]
    public void Decode_reproduces_the_reference_on_a_raw_byte_run()
    {
        using JsonDocument doc = OracleLoader.Load(Corpus);

        var failures = new List<string>();
        int runs = 0;
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            if (!c.TryGetProperty("decode_runs", out JsonElement declared))
            {
                continue;
            }
            string name = c.GetProperty("name").GetString()!;
            var tokenizer = new BpeTokenizer(Vocabulary(c));

            foreach (JsonElement run in declared.EnumerateArray())
            {
                runs++;
                List<int> ids = [.. run.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32())];
                string expected = run.GetProperty("decoded").GetString()!;
                string actual = tokenizer.Decode(ids);
                if (!string.Equals(expected, actual, StringComparison.Ordinal))
                {
                    string pieces = string.Join(
                        " ", run.GetProperty("pieces").EnumerateArray().Select(e => e.GetString()!));
                    failures.Add($"[{name}] {pieces}: expected {Escape(expected)}, got {Escape(actual)}");
                }
            }
        }

        Assert.Equal(DecodeRunRows, runs);
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    private static BpeVocabulary Vocabulary(JsonElement testCase) =>
        TokenizerJsonLoader.LoadBpe(
            new MemoryStream(Encoding.UTF8.GetBytes(testCase.GetProperty("tokenizer_json").GetString()!)),
            OracleReplay.BpeBounds());

    /// <summary>Renders a string as its code points, so a failure names what differs rather than showing two identical-looking lines.</summary>
    private static string Escape(string value) =>
        "\"" + string.Concat(value.Select(c => c < 0x20 || c > 0x7E ? $"\\u{(int)c:X4}" : c.ToString())) + "\"";
}
