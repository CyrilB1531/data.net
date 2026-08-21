using System.Buffers;
using System.Runtime.CompilerServices;

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
    /// <summary>The subsequence length, over the dense equality table or the side table beside it.</summary>
    /// <remarks>
    /// It no longer returns <c>false</c>: a pattern above Latin-1 refused here until #302 gave
    /// the kernel a side table, so every pattern has a route. The signature is kept because
    /// <see cref="Lcs"/>'s gate reads it, and because Myers' twin still refuses an empty pattern.
    /// </remarks>
    public static bool TrySubsequenceLength(
        ReadOnlySpan<char> pattern, ReadOnlySpan<char> text, out int length)
    {
        return pattern.Length <= 64
            ? TrySingleWord(pattern, text, out length)
            : TryBlocked(pattern, text, out length);
    }

    /// <summary>One entry per Latin-1 code unit: a text character above it reads no match.</summary>
    private const int Entries = 256;

    /// <summary>Longest pattern for which restoring a held table beats letting <c>stackalloc</c> zero one.</summary>
    /// <remarks>
    /// Swept over the pair corpus at 0, 16, 32 and 64 (#301). Its length-32 bucket reads 152.2
    /// ns/pair held nowhere, 138.5 at 16, 132.1 at 32 and 134.2 at 64, so the curve is flat
    /// either side of 32 and 32 is taken. Myers got the same sweep and every value was a
    /// regression there — the table is held in this kernel and not in that one, for the reason
    /// <see cref="Held"/> gives.
    /// </remarks>
    private const int MaxHeldPattern = 32;

    // One table per thread, all-zero between calls because every exit restores it -- including
    // the refusal, which has already written entries by the time it discovers it must give up.
    [ThreadStatic]
    private static ulong[]? held;

    /// <summary>The thread's equality table, held rather than zeroed on entry.</summary>
    /// <remarks>
    /// <c>Peq[c]</c> has bit <c>i</c> set where <c>pattern[i] == c</c>. Zeroing those 2 KB is a
    /// fixed cost on work that is <c>O(n)</c>, and <c>stackalloc</c> pays it on every call since
    /// nothing here disables <c>localsinit</c>; restoring costs the pattern instead, <c>O(m)</c>.
    /// Myers measured the other way on the same corpus and keeps its <c>stackalloc</c>: this
    /// recurrence is four operations per text character against that one's dozen, so the same
    /// fixed cost is a far larger share of what a call does.
    /// </remarks>
    private static ulong[] Held => held ??= new ulong[Entries];

    private static bool TrySingleWord(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text, out int length)
    {
        length = 0;
        if (pattern.Length > MaxHeldPattern)
        {
            return TrySingleWordOverStack(pattern, text, out length);
        }

        ulong[] peq = Held;
        if (!TryFill(pattern, peq))
        {
            return TrySingleWordWide(pattern, text, out length);
        }

        length = Scan(peq, default, default, pattern.Length, text);
        Restore(pattern, peq);
        return true;
    }

    /// <summary>The same kernel over a table zeroed by <c>localsinit</c>, past the length that pays for.</summary>
    /// <remarks>
    /// A <c>stackalloc</c> anywhere in a method zeroes on entry to it whether or not its branch
    /// is taken, so the held path only avoids the memset by living in a method that has none.
    /// </remarks>
    private static bool TrySingleWordOverStack(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text, out int length)
    {
        length = 0;
        Span<ulong> peq = stackalloc ulong[Entries];
        if (!TryFill(pattern, peq))
        {
            return TrySingleWordWide(pattern, text, out length);
        }

        length = Scan(peq, default, default, pattern.Length, text);
        return true;
    }

    /// <summary>The kernel for a pattern that leaves Latin-1, which carries a side table beside the dense one.</summary>
    /// <remarks>
    /// Its own method for the reason <see cref="TrySingleWordOverStack"/> gives, and because the
    /// side table is 1.25 KB that a Latin-1 pattern must not be charged for.
    /// </remarks>
    private static bool TrySingleWordWide(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text, out int length)
    {
        Span<ulong> peq = stackalloc ulong[Entries];
        Span<char> keys = stackalloc char[WideAlphabet.Capacity];
        Span<ulong> masks = stackalloc ulong[WideAlphabet.Capacity];

        WideAlphabet.Fill(pattern, peq, keys, masks);
        length = Scan(peq, keys, masks, pattern.Length, text);
        return true;
    }

    /// <summary>Sets each pattern position's bit, or reports a pattern that leaves Latin-1.</summary>
    /// <returns><c>false</c> when a character exceeds U+00FF, the table restored as it was found.</returns>
    private static bool TryFill(ReadOnlySpan<char> pattern, Span<ulong> table)
    {
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            if (c > 0xFF)
            {
                // The exit that is easy to miss: i entries are already written, and the next
                // pattern would read them as its own.
                Restore(pattern.Slice(0, i), table);
                return false;
            }
            table[c] |= 1UL << i;
        }
        return true;
    }

    /// <summary>Clears what <see cref="TryFill"/> wrote, restoring the all-zero invariant.</summary>
    private static void Restore(ReadOnlySpan<char> pattern, Span<ulong> table)
    {
        for (int i = 0; i < pattern.Length; i++)
        {
            table[pattern[i]] = 0UL;
        }
    }

    /// <summary>One machine word of the LCS recurrence, over the pattern's equality table.</summary>
    /// <remarks>Inlined rather than called: this is the hot loop, and a call here costs measurably.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Scan(
        ReadOnlySpan<ulong> peq, ReadOnlySpan<char> keys, ReadOnlySpan<ulong> masks,
        int m, ReadOnlySpan<char> text)
    {
        // All ones: no position has incremented yet. Carries run upward, so the bits
        // above m are masked at the end rather than kept clean throughout.
        ulong v = ulong.MaxValue;
        for (int j = 0; j < text.Length; j++)
        {
            char tc = text[j];
            ulong p = tc <= 0xFF ? peq[tc] : WideAlphabet.Lookup(keys, masks, tc);
            ulong u = v & p;
            v = (v + u) | (v - u);
        }

        ulong mask = m == 64 ? ulong.MaxValue : (1UL << m) - 1;
        return m - PopCount(v & mask);
    }

    private static bool TryBlocked(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text, out int length)
    {
        length = 0;
        int m = pattern.Length;
        int blocks = (m + 63) / 64;

        int slots = WideAlphabet.CapacityFor(m);
        int peqLength = (Entries + slots) * blocks;
        ulong[] peqRented = ArrayPool<ulong>.Shared.Rent(peqLength);
        char[] keysRented = ArrayPool<char>.Shared.Rent(slots);
        ulong[] vRented = ArrayPool<ulong>.Shared.Rent(blocks);
        try
        {
            Span<ulong> peq = peqRented.AsSpan(0, peqLength);
            Span<ulong> v = vRented.AsSpan(0, blocks);
            Span<char> keys = keysRented.AsSpan(0, slots);
            peq.Clear();
            keys.Clear();

            for (int i = 0; i < m; i++)
            {
                char c = pattern[i];
                int row;
                if (c <= 0xFF)
                {
                    row = c;
                }
                else
                {
                    int k = WideAlphabet.Probe(keys, c);
                    keys[k] = c;
                    row = Entries + k;
                }
                peq[(row * blocks) + (i >> 6)] |= 1UL << (i & 63);
            }

            for (int b = 0; b < blocks; b++)
            {
                v[b] = ulong.MaxValue;
            }

            for (int j = 0; j < text.Length; j++)
            {
                char tc = text[j];
                int row = tc <= 0xFF
                    ? tc * blocks
                    : WideAlphabet.BlockBase(keys, tc, blocks, Entries);
                if (row >= 0)
                {
                    Advance(v, peq.Slice(row, blocks), blocks);
                }

                // A character neither table holds matches nothing, so every u is zero
                // and Advance would hand v back exactly as it took it.
            }

            length = m - Count(v, blocks, m);
            return true;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(keysRented);
            ArrayPool<ulong>.Shared.Return(peqRented);
            ArrayPool<ulong>.Shared.Return(vRented);
        }
    }

    /// <summary>One text character, with only the add's carry crossing words.</summary>
    /// <remarks>
    /// <c>u</c> is <c>v &amp; peq</c>, a bit-subset of <c>v</c>, and subtracting a subset
    /// cannot borrow: <c>v - u</c> is <c>v &amp; ~u</c>, so the borrow this threaded between
    /// words was provably zero (#357). The addition still carries — an asymmetry the LCS
    /// recurrence owns, <c>Myers</c> carrying substitution and rightly keeping both chains.
    /// Inlined because it runs once per text character (#320).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Advance(Span<ulong> v, ReadOnlySpan<ulong> peqRow, int blocks)
    {
        ulong carry = 0;
        for (int b = 0; b < blocks; b++)
        {
            ulong value = v[b];
            ulong u = value & peqRow[b];

            ulong sum = value + u;
            ulong carriedSum = sum + carry;
            carry = (sum < value ? 1UL : 0UL) | (carriedSum < sum ? 1UL : 0UL);

            v[b] = carriedSum | (value & ~u);
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
