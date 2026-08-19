using Lodestar.Text.Distances;
using Xunit;

namespace Lodestar.Text.Tests.Distances;

// SonarLint S2245: a seeded Random builds a reproducible corpus; no security use.
#pragma warning disable S2245, CA5394

/// <summary>Pins the invariant the LCS kernel's held equality table rests on: every call leaves it clean.</summary>
/// <remarks>
/// A table not zeroed on entry is correct only while every exit restores it, and the exit easy
/// to miss is the refusal — a pattern abandoned partway through, its entries already set. The
/// damage never shows on the call that causes it, only on the next one, whose text reads a mask
/// its predecessor left behind. The dynamic program is the reference here as it is for the
/// kernel itself; Myers measured the other way and kept its <c>stackalloc</c> (#301).
/// </remarks>
public sealed class HeldEqualityTableTests
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyz ";

    /// <summary>Eight characters, Latin-1, sharing no affix with the text: the kernel's own case.</summary>
    private const string Refused = "abcd中fgh";

    [Fact]
    public void The_lcs_kernel_is_unaffected_by_the_pattern_before_it()
    {
        Lcs.SubsequenceLength("ABCDEFGH".AsSpan(), "ZZZZZZZZZZZZ".AsSpan(), TextElement.Utf16Unit);

        Assert.Equal(
            Lcs.SubsequenceLength<char>("abcdefgh".AsSpan(), "ABCDEFGHijkl".AsSpan()),
            Lcs.SubsequenceLength("abcdefgh".AsSpan(), "ABCDEFGHijkl".AsSpan(), TextElement.Utf16Unit));
    }

    [Fact]
    public void The_lcs_kernel_is_unaffected_by_a_refused_pattern()
    {
        Lcs.SubsequenceLength(Refused.AsSpan(), "ZZZZZZZZZZZZ".AsSpan(), TextElement.Utf16Unit);

        Assert.Equal(
            Lcs.SubsequenceLength<char>("wxyzWXYZ".AsSpan(), "abcdefghij".AsSpan()),
            Lcs.SubsequenceLength("wxyzWXYZ".AsSpan(), "abcdefghij".AsSpan(), TextElement.Utf16Unit));
    }

    /// <summary>Lengths on both sides of the pattern length above which the table is not held.</summary>
    [Theory]
    [InlineData(8)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(64)]
    public void A_run_of_pairs_agrees_with_the_dynamic_program(int length)
    {
        var rng = new Random(length);
        for (int trial = 0; trial < 500; trial++)
        {
            // Every third pair is refused partway, so the run interleaves the exit that
            // restores after a full pattern with the one that restores after part of it.
            string a = Random(rng, length, trial % 3 == 0);
            string b = Random(rng, length + rng.Next(0, 8), false);

            Assert.Equal(
                Levenshtein.Distance<char>(a.AsSpan(), b.AsSpan()),
                Levenshtein.Distance(a.AsSpan(), b.AsSpan(), TextElement.Utf16Unit));
            Assert.Equal(
                Lcs.SubsequenceLength<char>(a.AsSpan(), b.AsSpan()),
                Lcs.SubsequenceLength(a.AsSpan(), b.AsSpan(), TextElement.Utf16Unit));
        }
    }

    private static string Random(Random rng, int length, bool refused)
    {
        char[] value = new char[length];
        for (int i = 0; i < length; i++)
        {
            value[i] = Alphabet[rng.Next(Alphabet.Length)];
        }
        if (refused)
        {
            value[length - 1] = '中';
        }
        return new string(value);
    }
}
