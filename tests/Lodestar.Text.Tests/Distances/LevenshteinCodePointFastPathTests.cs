using Lodestar.Text;
using Lodestar.Text.Distances;
using Xunit;

namespace Lodestar.Text.Tests.Distances;

// SonarLint S2245 / CA5394: a seeded Random builds a reproducible corpus for this
// test; the sequence is fixed by the seed and nothing here is a security decision.
#pragma warning disable S2245, CA5394

/// <summary>
/// The code-point fast path against the dynamic program it replaces (#208).
/// </summary>
/// <remarks>
/// The oracle corpus proves the answer matches rapidfuzz, which is the contract.
/// This proves something the oracle cannot: that the two implementations *inside
/// this repository* agree, at the three boundaries the fast path has and the
/// oracle's random draws only visit by luck -- the 16-code-point gate, the
/// 64-code-point word, and the 255-symbol dense alphabet past which the renaming
/// refuses and the DP takes over. A failure here localizes to the kernel; a
/// failure in the oracle tests does not.
/// </remarks>
public sealed class LevenshteinCodePointFastPathTests
{
    private const int Seed = 20260818;

    // U+1F300..U+1FAFF: the emoji block, every character a surrogate pair.
    private const int Base = 0x1F300;
    private const int Span = 0x1FAFF - 0x1F300 + 1;

    /// <summary>
    /// The same distance computed without the fast path: <c>Distance{T}</c> over
    /// <c>int</c> is DP-only by construction, which is what makes it the control.
    /// </summary>
    private static int ByDynamicProgram(string a, string b)
    {
        int[] left = ToCodePoints(a);
        int[] right = ToCodePoints(b);
        return Levenshtein.Distance<int>(left, right);
    }

    private static int[] ToCodePoints(string text)
    {
        var points = new List<int>(text.Length);
        int i = 0;
        while (i < text.Length)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                points.Add(char.ConvertToUtf32(text[i], text[i + 1]));
                i += 2;
            }
            else
            {
                points.Add(text[i]);
                i++;
            }
        }
        return [.. points];
    }

    private static string Supplementary(Random rng, int length, int distinct)
    {
        var text = new System.Text.StringBuilder(length * 2);
        for (int i = 0; i < length; i++)
        {
            text.Append(char.ConvertFromUtf32(Base + (rng.Next(distinct) * Span / distinct)));
        }
        return text.ToString();
    }

    [Theory]
    // Straddling the 16-code-point gate, where the fast path opens.
    [InlineData(15, 32)]
    [InlineData(16, 32)]
    [InlineData(17, 32)]
    // Straddling the 64-code-point word, where the blocked kernel takes over.
    [InlineData(63, 32)]
    [InlineData(64, 32)]
    [InlineData(65, 32)]
    // Past the 255-symbol dense alphabet, where the renaming refuses.
    [InlineData(300, 254)]
    [InlineData(300, 255)]
    [InlineData(600, 512)]
    public void Agrees_with_the_dynamic_program_at_every_boundary(int length, int distinct)
    {
        var rng = new Random(Seed + length + distinct);

        for (int trial = 0; trial < 50; trial++)
        {
            string a = Supplementary(rng, length, distinct);
            string b = Supplementary(rng, length, distinct);

            Assert.Equal(ByDynamicProgram(a, b), Levenshtein.Distance(a, b, TextElement.CodePoint));
        }
    }

    [Fact]
    public void Counts_a_supplementary_character_as_one_edit()
    {
        // The whole reason the mode exists: one emoji is one code point and two
        // UTF-16 units, so the two readings must disagree here -- and this pair is
        // long enough to take the fast path, which the oracle corpus had no case
        // for before this change.
        string a = string.Concat(Enumerable.Repeat("\U0001F600", 20));
        string b = string.Concat(Enumerable.Repeat("\U0001F600", 19)) + "\U0001F601";

        Assert.Equal(1, Levenshtein.Distance(a, b, TextElement.CodePoint));
        Assert.Equal(1, Levenshtein.Distance(a, b));
        Assert.Equal(ByDynamicProgram(a, b), Levenshtein.Distance(a, b, TextElement.CodePoint));
    }

    [Fact]
    public void Agrees_with_the_dynamic_program_on_a_mixed_alphabet()
    {
        // Supplementary characters, Latin-1 and CJK in one pattern: the renaming
        // has to be indifferent to where in Unicode a symbol sits, and a table
        // keyed by the low byte would collide these.
        var rng = new Random(Seed);
        const string bmp = "aéz中文";

        for (int trial = 0; trial < 200; trial++)
        {
            var text = new System.Text.StringBuilder();
            int length = rng.Next(16, 120);
            for (int i = 0; i < length; i++)
            {
                text.Append(rng.Next(2) == 0
                    ? bmp[rng.Next(bmp.Length)].ToString()
                    : char.ConvertFromUtf32(Base + rng.Next(40)));
            }

            string a = text.ToString();
            string b = a.Length > 4 ? a.Remove(rng.Next(a.Length - 2), 2) : a;

            Assert.Equal(ByDynamicProgram(a, b), Levenshtein.Distance(a, b, TextElement.CodePoint));
        }
    }
}
