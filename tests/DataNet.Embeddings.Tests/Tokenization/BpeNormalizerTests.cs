using System.Text;
using System.Text.Json;
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

    /// <summary>Renders a string as its code points, so a failure names what differs rather than showing two identical-looking lines.</summary>
    private static string Escape(string text) =>
        string.Concat(text.Select(ch => ch < 0x20 || ch > 0x7e ? $"\\u{(int)ch:x4}" : ch.ToString()));
}
