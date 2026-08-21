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
    /// <remarks>
    /// Past 64 units the pattern spans more than one word, and that is where the two mechanisms
    /// meet for the first time: the blocked path sizes its side table per call, and surrogate
    /// halves crowd into D800..DFFF, which is the clustering the probe's hash exists for (#302).
    /// </remarks>
    [Theory]
    [InlineData(8)]
    [InlineData(32)]
    [InlineData(64)]
    [InlineData(66)]
    [InlineData(130)]
    [InlineData(300)]
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
    /// <remarks>
    /// 64 is one word's worth, the most a single-word pattern can hold. 300 is the blocked path,
    /// where the table has no fixed size: <c>WideAlphabet.CapacityFor</c> grows it past the 128
    /// slots one word is entitled to, and a pattern of distinct symbols is what makes it (#302).
    /// </remarks>
    [Theory]
    [InlineData(64)]
    [InlineData(300)]
    public void A_pattern_of_distinct_wide_characters_agrees_with_the_dynamic_program(int length)
    {
        var rng = new Random(length);
        for (int trial = 0; trial < 100; trial++)
        {
            char start = (char)(0x4E00 + rng.Next(0, 0x1000));
            char[] pattern = new char[length];
            for (int i = 0; i < length; i++)
            {
                pattern[i] = (char)(start + i);
            }

            // Both ends are moved, so Affixes.Trim strips nothing and the pattern the kernel
            // sees is the whole length — mutating at random left it a third of that (#302).
            string a = new(pattern);
            char[] other = Mutate(rng, a, rng.Next(1, 20)).ToCharArray();
            other[0]++;
            other[^1]++;
            AssertPair(a, new string(other));
        }
    }

    /// <summary>Each kernel takes a wide pattern from its own crossing, and refuses below it.</summary>
    /// <remarks>
    /// A wide row that fell back would measure the dynamic program under a kernel's name, and no
    /// timing could tell you: both routes return the same number. So the route is asserted rather
    /// than read off a benchmark, and it pins the two constants #409 measured — 6 on the LCS
    /// kernel, 10 on Myers — against a later edit moving one of them silently (#411).
    /// </remarks>
    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(12)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    [InlineData(96)]
    public void A_wide_pattern_reaches_each_kernel_from_its_own_crossing(int band)
    {
        var rng = new Random(band);
        string pattern = Random(rng, Cjk, band);
        string text = Random(rng, Cjk, band);

        Assert.Equal(band >= BitParallelLcs.WideMinPatternLength,
            BitParallelLcs.TrySubsequenceLength(pattern.AsSpan(), text.AsSpan(), out _));
        Assert.Equal(band >= Myers.WideMinPatternLength,
            Myers.TryDistance(pattern.AsSpan(), text.AsSpan(), out _));
    }

    /// <summary>And a Latin-1 pattern is never asked the question, at any length.</summary>
    /// <remarks>
    /// The refusal above lives in the branch a wide character takes, so a dense pattern reaches
    /// the kernel from the dispatch's own gate downwards. This is the half that must not have
    /// moved: it is the path <c>fuzz.ratio</c> runs (#411).
    /// </remarks>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(9)]
    [InlineData(64)]
    [InlineData(96)]
    public void A_latin_pattern_reaches_both_kernels_at_every_length(int band)
    {
        var rng = new Random(band + 500);
        string pattern = Random(rng, Latin, band);
        string text = Random(rng, Latin, band);

        Assert.True(BitParallelLcs.TrySubsequenceLength(pattern.AsSpan(), text.AsSpan(), out _));
        Assert.True(Myers.TryDistance(pattern.AsSpan(), text.AsSpan(), out _));
    }

    /// <summary>A Latin-1 pattern against a text that leaves Latin-1, on the blocked route.</summary>
    /// <remarks>
    /// The pattern allocates no side rows since #413, so the blocked lookup meets an empty probe
    /// table — which indexed it and threw until <c>BlockBase</c> gained the guard <c>Lookup</c>
    /// has had since #302. A pattern of ASCII and a text of CJK is an ordinary pairing, not a
    /// curiosity, and nothing exercised it above one machine word.
    /// </remarks>
    [Theory]
    [InlineData(65)]
    [InlineData(200)]
    [InlineData(1000)]
    public void A_latin_pattern_against_a_wide_text_agrees_with_the_dynamic_program(int length)
    {
        var rng = new Random(length);
        string pattern = Random(rng, Latin, length);
        string text = Random(rng, Cjk, length + 5);
        AssertPair(pattern, text);
    }

    /// <summary>Past the length at which the table's own arithmetic used to wrap.</summary>
    /// <remarks>
    /// The blocked table was <c>(256 + slots) × blocks</c> in unchecked <c>int</c>, and slots came
    /// from the pattern's length: past about 262 000 the product wrapped, and <c>Rent</c> either
    /// threw out of a distance function or returned a table too small to index. The answer is
    /// asserted by construction rather than against the DP, which is quadratic and cannot be run
    /// at this size (#413).
    /// </remarks>
    [Theory]
    [InlineData(262145, 0)]
    [InlineData(262145, 10)]
    public void A_pattern_past_the_old_wrap_point_still_answers(int length, int wideCount)
    {
        char[] a = new char[length];
        for (int i = 0; i < length; i++)
        {
            a[i] = Latin[i % Latin.Length];
        }

        // Spread the wide characters out so none of them lands on an edit below.
        for (int k = 0; k < wideCount; k++)
        {
            a[(k * 7919) % length] = Cjk[k % Cjk.Length];
        }

        char[] b = (char[])a.Clone();
        int[] edits = [3, length / 2, length - 4];
        foreach (int at in edits)
        {
            b[at] = b[at] == 'z' ? 'y' : 'z';
        }

        string x = new(a), y = new(b);
        Assert.Equal(edits.Length, Levenshtein.Distance(x.AsSpan(), y.AsSpan(), TextElement.Utf16Unit));
        Assert.Equal(length - edits.Length, Lcs.SubsequenceLength(x.AsSpan(), y.AsSpan(), TextElement.Utf16Unit));
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
