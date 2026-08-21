using Lodestar.Text.Distances;
using Xunit;

namespace Lodestar.Text.Tests.Distances;

// SonarLint S2245: a seeded Random builds a reproducible corpus; no security use.
#pragma warning disable S2245, CA5394

/// <summary>Pins the bit-parallel kernel on the patterns it used to refuse, against the DP it replaces.</summary>
/// <remarks>
/// A pattern above U+00FF could not be indexed into the 256-entry table and fell back to the
/// dynamic program; it now carries a side table instead (#302). What changes is which algorithm
/// runs, not the answer, so the dynamic program is the reference — as it is for the Latin-1 kernel
/// and for the blocked path #273 covered the same way. The generic overload is the DP.
/// </remarks>
public sealed class WideAlphabetKernelTests
{
    private const string Latin = "abcdefghijklmnopqrstuvwxyz ";

    /// <summary>CJK, which is above Latin-1 and inside the BMP: one UTF-16 unit per character.</summary>
    private const string Cjk = "一二三四五六七八九十百千万上下左右前後";

    /// <summary>Emoji, whose code points cluster inside one block — the case the hash exists for.</summary>
    private static readonly string[] Emoji =
        ["😀", "😁", "😂", "🤣", "😃", "😄", "😅", "😆", "😉", "😊", "🙂", "🙃"];

    /// <summary>Lengths straddling the gate, the word boundary, and the length the table is held to.</summary>
    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(129)]
    [InlineData(300)]
    public void A_cjk_pattern_agrees_with_the_dynamic_program(int length)
    {
        AssertAgrees(length, Cjk, mixed: false);
    }

    /// <summary>The mixed case, where both tables carry part of the same pattern.</summary>
    [Theory]
    [InlineData(8)]
    [InlineData(32)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(129)]
    [InlineData(300)]
    public void A_pattern_of_both_alphabets_agrees_with_the_dynamic_program(int length)
    {
        AssertAgrees(length, Latin + Cjk, mixed: true);
    }

    /// <summary>Supplementary characters, which reach the kernel as surrogate pairs in this mode.</summary>
    [Theory]
    [InlineData(8)]
    [InlineData(32)]
    [InlineData(64)]
    public void An_emoji_pattern_agrees_with_the_dynamic_program(int units)
    {
        var rng = new Random(units);
        for (int trial = 0; trial < 200; trial++)
        {
            string a = Repeat(rng, Emoji, units / 2);
            string b = Repeat(rng, Emoji, (units / 2) + rng.Next(0, 3));
            AssertPair(a, b);
        }
    }

    /// <summary>A pattern whose characters are all distinct and all wide: the side table at its fullest.</summary>
    [Fact]
    public void A_pattern_of_sixty_four_distinct_wide_characters_agrees_with_the_dynamic_program()
    {
        var rng = new Random(64);
        for (int trial = 0; trial < 100; trial++)
        {
            char start = (char)(0x4E00 + rng.Next(0, 0x1000));
            char[] pattern = new char[64];
            for (int i = 0; i < 64; i++)
            {
                pattern[i] = (char)(start + i);
            }

            string a = new(pattern);
            string b = Mutate(rng, a, rng.Next(1, 20));
            AssertPair(a, b);
        }
    }

    /// <summary>The bands the gate benchmarks measure, on the question those rows silently assume.</summary>
    /// <remarks>
    /// A CJK row that fell back would measure the dynamic program under a kernel's name, and the
    /// timing could not tell you: both routes return the same number. So the route is asserted
    /// here instead of read off a benchmark. Below the gate both alphabets take the DP by design,
    /// which is why the list starts at 8 — <c>bench/README.md</c> has the gate's own sweep (#383).
    /// </remarks>
    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(12)]
    [InlineData(14)]
    [InlineData(16)]
    [InlineData(18)]
    [InlineData(20)]
    [InlineData(24)]
    [InlineData(32)]
    [InlineData(48)]
    [InlineData(64)]
    [InlineData(96)]
    public void A_cjk_band_the_gate_benchmarks_use_reaches_both_kernels(int band)
    {
        var rng = new Random(band);
        string pattern = Random(rng, Cjk, band);
        string text = Random(rng, Cjk, band);

        Assert.True(Myers.TryDistance(pattern.AsSpan(), text.AsSpan(), out _));
        Assert.True(BitParallelLcs.TrySubsequenceLength(pattern.AsSpan(), text.AsSpan(), out _));
    }

    private static void AssertAgrees(int length, string alphabet, bool mixed)
    {
        var rng = new Random(length + (mixed ? 1000 : 0));
        for (int trial = 0; trial < 200; trial++)
        {
            string a = Random(rng, alphabet, length);
            string b = Mutate(rng, a, rng.Next(1, Math.Max(2, length / 2)));
            AssertPair(a, b);
        }
    }

    private static void AssertPair(string a, string b)
    {
        // TextElement is passed explicitly: with two arguments C# picks the generic overload,
        // applicable in normal form, and both sides would be the dynamic program.
        Assert.Equal(
            Levenshtein.Distance<char>(a.AsSpan(), b.AsSpan()),
            Levenshtein.Distance(a.AsSpan(), b.AsSpan(), TextElement.Utf16Unit));
        Assert.Equal(
            Lcs.SubsequenceLength<char>(a.AsSpan(), b.AsSpan()),
            Lcs.SubsequenceLength(a.AsSpan(), b.AsSpan(), TextElement.Utf16Unit));
        Assert.Equal(
            Indel.Distance<char>(a.AsSpan(), b.AsSpan()),
            Indel.Distance(a.AsSpan(), b.AsSpan(), TextElement.Utf16Unit));
    }

    private static string Random(Random rng, string alphabet, int length)
    {
        char[] value = new char[length];
        for (int i = 0; i < length; i++)
        {
            value[i] = alphabet[rng.Next(alphabet.Length)];
        }
        return new string(value);
    }

    private static string Repeat(Random rng, string[] pieces, int count)
    {
        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < count; i++)
        {
            builder.Append(pieces[rng.Next(pieces.Length)]);
        }
        return builder.ToString();
    }

    private static string Mutate(Random rng, string value, int edits)
    {
        char[] chars = value.ToCharArray();
        for (int i = 0; i < edits && chars.Length > 0; i++)
        {
            int at = rng.Next(chars.Length);
            chars[at] = (char)(chars[at] + 1);
        }
        return new string(chars);
    }
}
