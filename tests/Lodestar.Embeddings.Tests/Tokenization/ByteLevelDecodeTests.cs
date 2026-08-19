using System.Text.Json;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests.Tokenization;

/// <summary>
/// Replays <c>bytelevel_decode_stream.json</c> -- issue #149's own corpus, frozen
/// from decoding the GPT-2 vocabulary one id at a time.
/// </summary>
public sealed class ByteLevelDecodeTests
{
    /// <summary>
    /// Each id decoded on its own, which is how a caller consumes a stream. Every
    /// token of a CJK or emoji text is a fragment of a multi-byte character, so this
    /// is the case that threw before issue #149 rather than an exotic one.
    /// </summary>
    [Fact]
    public void Decode_of_one_id_at_a_time_matches_the_reference()
    {
        using JsonDocument doc = OracleLoader.Load("bytelevel_decode_stream.json");
        BpeTokenizer tokenizer = new(ByteLevelBpeTests.Gpt2Vocabulary());

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            int[] ids = [.. c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32())];
            string[] expected = [.. c.GetProperty("per_id_decoded").EnumerateArray().Select(e => e.GetString()!)];

            for (int i = 0; i < ids.Length; i++)
            {
                string actual = tokenizer.Decode([ids[i]]);
                if (!string.Equals(expected[i], actual, StringComparison.Ordinal))
                {
                    failures.Add($"id {ids[i]}: expected {Escape(expected[i])}, got {Escape(actual)}");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// The whole stream still decodes exactly. A complete, valid byte sequence never
    /// reaches the fallback, so the round trip this package promises is untouched by
    /// the substitution the test above measures.
    /// </summary>
    [Fact]
    public void Decode_of_the_whole_stream_is_unchanged_by_the_fallback()
    {
        using JsonDocument doc = OracleLoader.Load("bytelevel_decode_stream.json");
        BpeTokenizer tokenizer = new(ByteLevelBpeTests.Gpt2Vocabulary());

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            int[] ids = [.. c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32())];
            string expected = c.GetProperty("decoded").GetString()!;
            string actual = tokenizer.Decode(ids);

            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                failures.Add($"{Escape(expected)} != {Escape(actual)}");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
        Assert.DoesNotContain('\uFFFD', string.Join("", doc.RootElement.GetProperty("cases")
            .EnumerateArray().Select(c => c.GetProperty("decoded").GetString()!)));
    }

    /// <summary>Renders a string as its code points, so a failure names what differs rather than showing two identical-looking lines.</summary>
    private static string Escape(string text) =>
        string.Concat(text.Select(ch => ch < 0x20 || ch > 0x7e ? $"\\u{(int)ch:x4}" : ch.ToString()));
}
