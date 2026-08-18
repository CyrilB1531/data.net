using Lodestar.Text.Distances;
using Xunit;

namespace Lodestar.Text.Tests.Distances;

// SonarLint S2245: a seeded Random builds a reproducible corpus; no security use.
#pragma warning disable S2245, CA5394

/// <summary>
/// Pins the bit-parallel kernel against the dynamic program it replaces.
/// </summary>
/// <remarks>
/// The oracle corpora cannot do this job. Measured on both of them: of 1 522 cases,
/// 97 reach the kernel and <b>none</b> reaches the blocked path, because every pair is
/// short enough to fit one machine word once the shared ends are trimmed. So the whole
/// multi-word carry and borrow propagation shipped unexecuted by a green suite — the
/// failure ADR 0004's testing note records for #52, one lot later.
///
/// The chain these tests close: the DP is conformant because 1 522 frozen rapidfuzz
/// cases say so, and the kernel is conformant because it agrees with the DP on inputs
/// the corpus never reaches. <see cref="Lcs.SubsequenceLength{T}"/> is the generic
/// overload, which stays on the DP by design, so it is the reference here.
/// </remarks>
public sealed class BitParallelLcsTests
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyz ";

    /// <summary>Lengths straddling every word boundary the blocked path has to carry across.</summary>
    [Theory]
    [InlineData(16)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(129)]
    [InlineData(255)]
    [InlineData(512)]
    public void The_kernel_agrees_with_the_dynamic_program(int length)
    {
        var rng = new Random(length);
        for (int trial = 0; trial < 200; trial++)
        {
            string a = Random(rng, length);
            string b = Mutate(rng, a, rng.Next(1, Math.Max(2, length / 2)));

            // TextElement is passed explicitly: with two arguments C# picks the generic
            // overload -- applicable in normal form -- and both sides would be the DP.
            Assert.Equal(
                Lcs.SubsequenceLength<char>(a.AsSpan(), b.AsSpan()),
                Lcs.SubsequenceLength(a.AsSpan(), b.AsSpan(), TextElement.Utf16Unit));
        }
    }

    /// <summary>Unrelated operands, where nothing trims and the band never collapses.</summary>
    [Theory]
    [InlineData(64)]
    [InlineData(200)]
    [InlineData(512)]
    public void The_kernel_agrees_when_the_operands_share_no_ends(int length)
    {
        var rng = new Random(~length);
        for (int trial = 0; trial < 100; trial++)
        {
            string a = Random(rng, length);
            string b = Random(rng, length);

            Assert.Equal(
                Lcs.SubsequenceLength<char>(a.AsSpan(), b.AsSpan()),
                Lcs.SubsequenceLength(a.AsSpan(), b.AsSpan(), TextElement.Utf16Unit));
        }
    }

    [Fact]
    public void A_pattern_outside_latin1_falls_back_and_still_answers()
    {
        // The kernel refuses a pattern it cannot index; the DP has to catch it, and the
        // refusal is silent, so only the value tells you it happened.
        string a = new string('あ', 40) + "tail";
        string b = new string('あ', 38) + "tail";

        Assert.Equal(
            Lcs.SubsequenceLength<char>(a.AsSpan(), b.AsSpan()),
            Lcs.SubsequenceLength(a.AsSpan(), b.AsSpan(), TextElement.Utf16Unit));
    }

    [Theory]
    [InlineData(80)]
    [InlineData(300)]
    public void Indel_reaches_the_same_answer_as_its_definition(int length)
    {
        // Indel is len(a) + len(b) - 2*LCS by definition; the character path now takes a
        // different route to it than the generic one, and they may not disagree.
        var rng = new Random(length * 31);
        for (int trial = 0; trial < 100; trial++)
        {
            string a = Random(rng, length);
            string b = Mutate(rng, a, rng.Next(1, length / 2));

            Assert.Equal(
                a.Length + b.Length - (2 * Lcs.SubsequenceLength<char>(a.AsSpan(), b.AsSpan())),
                Indel.Distance(a.AsSpan(), b.AsSpan()));
        }
    }

    private static string Random(Random rng, int length)
    {
        char[] text = new char[length];
        for (int i = 0; i < length; i++)
        {
            text[i] = Alphabet[rng.Next(Alphabet.Length)];
        }

        return new string(text);
    }

    private static string Mutate(Random rng, string source, int edits)
    {
        char[] text = source.ToCharArray();
        for (int i = 0; i < edits; i++)
        {
            text[rng.Next(text.Length)] = Alphabet[rng.Next(Alphabet.Length)];
        }

        return new string(text);
    }
}
