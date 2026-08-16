using System.Text.Json;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests.Persistence;

/// <summary>Replays the <c>cases</c> array of a tokenization oracle.</summary>
/// <remarks>
/// Collects every mismatch before failing, so one run reports the whole picture
/// rather than the first divergent string.
/// </remarks>
internal static class OracleReplay
{
    /// <summary>The bounds every synthetic and replayed BPE fixture is loaded under.</summary>
    public static ArtifactLoadOptions BpeBounds() => new()
    {
        MaxTotalBytes = 8L * 1024 * 1024,
        MaxVocabularySize = 100_000,
        MaxArrayLength = 100_000,
        MaxTokenLength = 512,
    };

    public static void AssertEncodings(
        JsonDocument doc,
        Func<string, TokenizationResult> encode,
        string tokensProperty,
        string? modelFilter = null,
        string? nameProperty = null)
    {
        var failures = new List<string>();
        int replayed = 0;

        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            if (modelFilter is not null
                && (!c.TryGetProperty("model", out JsonElement model)
                    || !string.Equals(model.GetString(), modelFilter, StringComparison.Ordinal)))
            {
                continue;
            }

            replayed++;
            string text = c.GetProperty("text").GetString()!;
            string[] expectedTokens = [.. c.GetProperty(tokensProperty).EnumerateArray().Select(e => e.GetString()!)];
            int[] expectedIds = [.. c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32())];

            TokenizationResult actual = encode(text);
            if (!expectedTokens.SequenceEqual(actual.Tokens) || !expectedIds.SequenceEqual(actual.Ids))
            {
                string label = nameProperty is not null ? $"[{c.GetProperty(nameProperty).GetString()}] \"{text}\"" : $"\"{text}\"";
                failures.Add(
                    $"{label}\n  exp tokens: [{string.Join(", ", expectedTokens)}]\n  got tokens: [{string.Join(", ", actual.Tokens)}]" +
                    $"\n  exp ids: [{string.Join(", ", expectedIds)}]\n  got ids: [{string.Join(", ", actual.Ids)}]");
            }
        }

        // A filter that matches nothing would otherwise pass silently.
        Assert.True(replayed > 0, $"No oracle case matched the model filter '{modelFilter ?? "(none)"}'.");
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// Replays the <c>ids</c> / <c>decoded</c> / <c>decoded_skip_specials</c> triple
    /// of every case through <paramref name="decode"/>, called once with
    /// <c>skipSpecialTokens: false</c> and once with <c>true</c>.
    /// </summary>
    public static void AssertDecodes(JsonDocument doc, Func<int[], bool, string> decode)
    {
        var failures = new List<string>();
        int replayed = 0;

        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            replayed++;
            int[] ids = [.. c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32())];
            string expected = c.GetProperty("decoded").GetString()!;
            string expectedSkipping = c.GetProperty("decoded_skip_specials").GetString()!;

            string actual = decode(ids, false);
            string actualSkipping = decode(ids, true);
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                failures.Add($"decode {JsonSerializer.Serialize(expected)} got {JsonSerializer.Serialize(actual)}");
            }
            if (!string.Equals(expectedSkipping, actualSkipping, StringComparison.Ordinal))
            {
                failures.Add($"decode-skipping {JsonSerializer.Serialize(expectedSkipping)} got {JsonSerializer.Serialize(actualSkipping)}");
            }
        }

        // An empty cases array would otherwise pass over nothing at all.
        Assert.True(replayed > 0, "No oracle cases to replay.");
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }
}
