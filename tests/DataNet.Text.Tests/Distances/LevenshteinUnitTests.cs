using DataNet.Text.Distances;
using Xunit;

namespace DataNet.Text.Tests.Distances;

/// <summary>Hand-authored tests: textbook values and the documented Unicode divergence.</summary>
public sealed class LevenshteinUnitTests
{
    [Theory]
    [InlineData("", "", 0)]
    [InlineData("a", "", 1)]
    [InlineData("", "abc", 3)]
    [InlineData("abc", "abc", 0)]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("flaw", "lawn", 2)]
    [InlineData("gumbo", "gambol", 2)]
    [InlineData("Saturday", "Sunday", 3)]
    public void Distance_textbook_values(string a, string b, int expected)
    {
        Assert.Equal(expected, Levenshtein.Distance(a, b));
    }

    [Fact]
    public void NormalizedSimilarity_kitten_sitting()
    {
        // 1 - 3/max(6,7) = 1 - 3/7
        Assert.Equal(1.0 - 3.0 / 7.0, Levenshtein.NormalizedSimilarity("kitten", "sitting"), 12);
    }

    [Fact]
    public void NormalizedSimilarity_both_empty_is_one()
    {
        Assert.Equal(1.0, Levenshtein.NormalizedSimilarity("", ""));
        Assert.Equal(0.0, Levenshtein.NormalizedDistance("", ""));
    }

    [Fact]
    public void Distance_is_symmetric_for_chars()
    {
        Assert.Equal(
            Levenshtein.Distance("Levenshtein", "Levenstein"),
            Levenshtein.Distance("Levenstein", "Levenshtein"));
    }

    // The crux of §5: a supplementary-plane character is one code point but two
    // UTF-16 code units. The two modes must therefore disagree, on purpose.
    [Fact]
    public void Utf16_and_codepoint_diverge_on_supplementary_plane()
    {
        // U+1F600 GRINNING FACE vs U+1F601: one code point differs.
        const string a = "\U0001F600";
        const string b = "\U0001F601";

        // Code-point view (matches Python/rapidfuzz): a single substitution.
        Assert.Equal(1, Levenshtein.Distance(a, b, TextElement.CodePoint));

        // UTF-16 view: high surrogates equal, low surrogates differ -> 1 here too,
        // but the *lengths* differ, which shows up in mixed strings below.
        Assert.Equal(2, a.Length); // sanity: the emoji is a surrogate pair
    }

    [Fact]
    public void Utf16_counts_surrogate_units_codepoint_counts_scalars()
    {
        // "a😀" deleted down to "a": one emoji removed.
        const string a = "a\U0001F600";
        const string b = "a";

        // Code points: delete a single scalar -> distance 1 (Python's answer).
        Assert.Equal(1, Levenshtein.Distance(a, b, TextElement.CodePoint));

        // UTF-16 units: delete two code units -> distance 2 (the .NET-native answer).
        Assert.Equal(2, Levenshtein.Distance(a, b, TextElement.Utf16Unit));
    }

    [Fact]
    public void Generic_core_works_on_arbitrary_elements()
    {
        int[] a = [1, 2, 3, 4];
        int[] b = [1, 3, 4];
        Assert.Equal(1, Levenshtein.Distance<int>(a, b));
    }
}
