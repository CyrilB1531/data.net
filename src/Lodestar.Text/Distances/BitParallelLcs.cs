using System.Buffers;

namespace Lodestar.Text.Distances;

/// <summary>Bit-parallel LCS length: the fast path under <see cref="Lcs"/>, <see cref="Indel"/> and <c>fuzz.ratio</c>.</summary>
/// <remarks>
/// Myers' machinery — a dense Latin-1 equality table, blocked into 64-bit words — over a
/// different recurrence, Myers carrying substitution and LCS not. <c>V</c> holds the LCS
/// row, a set bit being a position that did not increment, advanced per text character by
/// <c>V = (V + (V &amp; P)) | (V - (V &amp; P))</c>; the answer counts the cleared bits.
/// Derived from the published recurrence (Hyyrö), not transcribed — decision 0003.
/// </remarks>
internal static class BitParallelLcs
{
    /// <summary>The subsequence length, or false when the pattern leaves the dense alphabet.</summary>
    public static bool TrySubsequenceLength(
        ReadOnlySpan<char> pattern, ReadOnlySpan<char> text, out int length)
    {
        return pattern.Length <= 64
            ? TrySingleWord(pattern, text, out length)
            : TryBlocked(pattern, text, out length);
    }

    private static bool TrySingleWord(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text, out int length)
    {
        length = 0;
        int m = pattern.Length;

        Span<ulong> peq = stackalloc ulong[256];
        peq.Clear();
        for (int i = 0; i < m; i++)
        {
            char c = pattern[i];
            if (c > 0xFF)
            {
                return false; // pattern outside Latin-1: let the DP handle it
            }
            peq[c] |= 1UL << i;
        }

        // All ones: no position has incremented yet. Carries run upward, so the bits
        // above m are masked at the end rather than kept clean throughout.
        ulong v = ulong.MaxValue;
        for (int j = 0; j < text.Length; j++)
        {
            char tc = text[j];
            ulong p = tc <= 0xFF ? peq[tc] : 0UL;
            ulong u = v & p;
            v = (v + u) | (v - u);
        }

        ulong mask = m == 64 ? ulong.MaxValue : (1UL << m) - 1;
        length = m - PopCount(v & mask);
        return true;
    }

    private static bool TryBlocked(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text, out int length)
    {
        length = 0;
        int m = pattern.Length;
        int blocks = (m + 63) / 64;

        int peqLength = 256 * blocks;
        ulong[] peqRented = ArrayPool<ulong>.Shared.Rent(peqLength);
        ulong[] vRented = ArrayPool<ulong>.Shared.Rent(blocks);
        try
        {
            Span<ulong> peq = peqRented.AsSpan(0, peqLength);
            Span<ulong> v = vRented.AsSpan(0, blocks);
            peq.Clear();

            for (int i = 0; i < m; i++)
            {
                char c = pattern[i];
                if (c > 0xFF)
                {
                    return false; // pattern outside Latin-1: let the DP handle it
                }
                peq[(c * blocks) + (i >> 6)] |= 1UL << (i & 63);
            }

            for (int b = 0; b < blocks; b++)
            {
                v[b] = ulong.MaxValue;
            }

            for (int j = 0; j < text.Length; j++)
            {
                char tc = text[j];
                int peqBase = tc <= 0xFF ? tc * blocks : -1;
                Advance(v, peq, peqBase, blocks);
            }

            length = m - Count(v, blocks, m);
            return true;
        }
        finally
        {
            ArrayPool<ulong>.Shared.Return(peqRented);
            ArrayPool<ulong>.Shared.Return(vRented);
        }
    }

    /// <summary>One text character, with the add's carry and the subtract's borrow crossing words.</summary>
    /// <remarks>
    /// The single-word update is one addition and one subtraction; over several words each
    /// becomes multi-precision, and the two propagate independently — which is why the carry
    /// and the borrow are tracked separately rather than as one flag.
    /// </remarks>
    private static void Advance(Span<ulong> v, ReadOnlySpan<ulong> peq, int peqBase, int blocks)
    {
        ulong carry = 0;
        ulong borrow = 0;
        for (int b = 0; b < blocks; b++)
        {
            ulong value = v[b];
            ulong u = value & (peqBase >= 0 ? peq[peqBase + b] : 0UL);

            ulong sum = value + u;
            ulong carriedSum = sum + carry;
            carry = (sum < value ? 1UL : 0UL) | (carriedSum < sum ? 1UL : 0UL);

            ulong difference = value - u;
            ulong borrowedDifference = difference - borrow;
            borrow = (value < u ? 1UL : 0UL) | (difference < borrow ? 1UL : 0UL);

            v[b] = carriedSum | borrowedDifference;
        }
    }

    /// <summary>Set bits below <paramref name="m"/>: the positions that never incremented.</summary>
    private static int Count(ReadOnlySpan<ulong> v, int blocks, int m)
    {
        int set = 0;
        for (int b = 0; b < blocks - 1; b++)
        {
            set += PopCount(v[b]);
        }

        int tail = m - ((blocks - 1) * 64);
        ulong mask = tail == 64 ? ulong.MaxValue : (1UL << tail) - 1;
        return set + PopCount(v[blocks - 1] & mask);
    }

#if NET
    private static int PopCount(ulong value) => System.Numerics.BitOperations.PopCount(value);
#else
    /// <summary>The SWAR population count, netstandard2.0 having no BitOperations.</summary>
    private static int PopCount(ulong value)
    {
        value -= (value >> 1) & 0x5555555555555555UL;
        value = (value & 0x3333333333333333UL) + ((value >> 2) & 0x3333333333333333UL);
        value = (value + (value >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
        return (int)((value * 0x0101010101010101UL) >> 56);
    }
#endif
}
