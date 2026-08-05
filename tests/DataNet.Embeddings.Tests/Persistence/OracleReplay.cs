using System.Text.Json;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests.Persistence;

/// <summary>Replays the <c>cases</c> array of a tokenization oracle.</summary>
/// <remarks>
/// Collects every mismatch before failing, so one run reports the whole picture
/// rather than the first divergent string.
/// </remarks>
internal static class OracleReplay
{
    public static void AssertEncodings(
        JsonDocument doc,
        Func<string, TokenizationResult> encode,
        string tokensProperty,
        string? modelFilter = null)
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
                failures.Add(
                    $"\"{text}\"\n  exp tokens: [{string.Join(", ", expectedTokens)}]\n  got tokens: [{string.Join(", ", actual.Tokens)}]" +
                    $"\n  exp ids: [{string.Join(", ", expectedIds)}]\n  got ids: [{string.Join(", ", actual.Ids)}]");
            }
        }

        // A filter that matches nothing would otherwise pass silently.
        Assert.True(replayed > 0, $"No oracle case matched the model filter '{modelFilter ?? "(none)"}'.");
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }
}
