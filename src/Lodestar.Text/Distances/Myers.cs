using System.Buffers;
using System.Runtime.CompilerServices;

namespace Lodestar.Text.Distances;


// SonarLint S3776: cognitive complexity. TryBlocked is a transcription of
// Hyyro's blocked formulation — the nested loop and its carry threading ARE the
// algorithm, and splitting them would break the one-to-one reading against the
// paper that makes a bit-manipulation kernel auditable at all. It is also the
// hot path: helper calls here cost measurably.
#pragma warning disable S3776
/// <summary>
/// Myers' bit-parallel edit-distance algorithm: a single machine word for
/// patterns up to 64 characters, <see cref="TryBlocked"/> beyond that.
/// </summary>
/// <remarks>
/// <c>O(n·⌈m/w⌉)</c> against the DP's <c>O(n·m)</c> (Myers 1999; Hyyrö 2003). Neither entry
/// point is restricted to an alphabet: the <see cref="char"/> one puts what leaves Latin-1 in
/// <see cref="WideAlphabet"/>'s side table (#302, #382), the code-point one renames first.
/// Backlog in <c>docs/decisions/0004-levenshtein-myers-backlog.md</c>.
/// </remarks>
internal static class Myers
{
    /// <summary>How many distinct code points a renamed pattern may hold.</summary>
    /// <remarks>
    /// 255 rather than 256: one slot is reserved for every text symbol the pattern
    /// does not contain, and the kernels index a 256-entry table by the symbol.
    /// </remarks>
    private const int DenseAlphabet = 255;

    /// <summary>The reserved slot, whose equality mask no pattern symbol ever sets.</summary>
    private const char Unmatched = (char)DenseAlphabet;

    /// <summary>Probe table size: a power of two, twice the alphabet it must hold.</summary>
    private const int SlotCapacity = 512;

    /// <summary>The free-slot marker.</summary>
    /// <remarks>
    /// Zero, and keys are stored as <c>symbol + 1</c> so no code point produces it.
    /// That makes an unwritten table already correct -- <c>stackalloc</c> zeroes,
    /// and nothing here disables <c>localsinit</c> -- so the 512-entry fill a
    /// sentinel of -1 would need does not happen at all.
    /// </remarks>
    private const int Empty = 0;

    /// <summary>
    /// Attempts to compute the Levenshtein distance between <paramref name="pattern"/>
    /// and <paramref name="text"/> using the single-word algorithm.
    /// </summary>
    /// <returns>
    /// <c>true</c> and the distance; <c>false</c> only when <paramref name="pattern"/> is
    /// empty. A character above U+00FF refused here until #302 gave the kernel a side table,
    /// and a pattern longer than 64 goes to <see cref="TryBlocked"/> rather than to the DP.
    /// </returns>
    public static bool TryDistance(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text, out int distance)
    {
        distance = 0;
        int m = pattern.Length;
        if (m == 0)
        {
            return false;
        }

        return m <= 64
            ? TrySingleWord(pattern, text, out distance)
            : TryBlocked(pattern, text, out distance);
    }

    /// <summary>The same computation over code points, renamed into a dense alphabet.</summary>
    /// <returns>
    /// <c>true</c> and the distance; <c>false</c> when the pattern is empty or
    /// holds more than <see cref="DenseAlphabet"/> distinct code points, and the
    /// caller must fall back to the DP.
    /// </returns>
    /// <remarks>
    /// The pattern's distinct code points are numbered <c>0..k-1</c> and every text
    /// symbol it lacks becomes the one free slot -- already the all-zero mask an
    /// unmatched character gets. See <c>docs/decisions/0004</c> (#208).
    /// </remarks>
    public static bool TryDistance(ReadOnlySpan<int> pattern, ReadOnlySpan<int> text, out int distance)
    {
        distance = 0;
        int m = pattern.Length;
        if (m == 0)
        {
            return false;
        }

        char[] patternRented = ArrayPool<char>.Shared.Rent(m);
        char[] textRented = ArrayPool<char>.Shared.Rent(Math.Max(1, text.Length));
        try
        {
            Span<char> renamedPattern = patternRented.AsSpan(0, m);
            Span<char> renamedText = textRented.AsSpan(0, text.Length);

            Span<int> keys = stackalloc int[SlotCapacity];
            Span<byte> slots = stackalloc byte[SlotCapacity];

            int distinct = 0;
            for (int i = 0; i < m; i++)
            {
                // A negative symbol would store Empty and read back free forever. Callers
                // decode code points, so refusing keeps that true in Release too.
                if (pattern[i] < 0)
                {
                    return false;
                }

                int probe = Probe(keys, pattern[i]);
                if (keys[probe] == Empty)
                {
                    if (distinct == DenseAlphabet)
                    {
                        return false; // more symbols than the dense alphabet holds
                    }
                    keys[probe] = pattern[i] + 1;
                    slots[probe] = (byte)distinct;
                    distinct++;
                }
                renamedPattern[i] = (char)slots[probe];
            }

            for (int j = 0; j < text.Length; j++)
            {
                int probe = Probe(keys, text[j]);
                renamedText[j] = keys[probe] == Empty ? Unmatched : (char)slots[probe];
            }

            return TryDistance((ReadOnlySpan<char>)renamedPattern, (ReadOnlySpan<char>)renamedText, out distance);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(patternRented);
            ArrayPool<char>.Shared.Return(textRented);
        }
    }

    /// <summary>The slot index of <paramref name="symbol"/>, occupied or free.</summary>
    /// <remarks>
    /// Linear probing terminates only because a free slot is guaranteed: at most
    /// <see cref="DenseAlphabet"/> of <see cref="SlotCapacity"/> entries are ever
    /// occupied, the next distinct symbol being refused rather than stored. The
    /// multiply is Knuth's, because code points cluster hard -- an emoji pattern
    /// lives inside U+1F300..U+1FAFF, and masking those low bits alone would pile
    /// every symbol into a few slots.
    /// </remarks>
    private static int Probe(Span<int> keys, int symbol)
    {
        // Stored as symbol + 1, so a negative symbol from the text reads as free
        // rather than colliding: every stored key is at least 1.
        int stored = symbol + 1;
        int index = (int)(((uint)symbol * 2654435761u) >> 23) & (SlotCapacity - 1);
        while (keys[index] != Empty && keys[index] != stored)
        {
            index = (index + 1) & (SlotCapacity - 1);
        }
        return index;
    }

    private static bool TrySingleWord(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text, out int distance)
    {
        distance = 0;
        int m = pattern.Length;

        // Peq[c] has bit i set where pattern[i] == c; a 256-entry table covers Latin-1, and a
        // pattern that leaves it takes the wide path rather than the DP (#302).

        // Not cleared, for the reason Empty above gives: stackalloc zeroes already,
        // so a Clear was a second 2 KB memset on a call whose work is O(n) (#208).
        Span<ulong> peq = stackalloc ulong[256];
        for (int i = 0; i < m; i++)
        {
            char c = pattern[i];
            if (c > 0xFF)
            {
                return TrySingleWordWide(pattern, text, out distance);
            }
            peq[c] |= 1UL << i;
        }

        distance = Scan(peq, default, default, m, text);
        return true;
    }

    /// <summary>The kernel for a pattern that leaves Latin-1, which carries a side table beside the dense one.</summary>
    /// <remarks>
    /// Its own method because a <c>stackalloc</c> zeroes on entry to the method holding it whether
    /// or not its branch is taken (#301), and a Latin-1 pattern must not be charged the side
    /// table's 1.25 KB. The dense table is rebuilt here rather than handed over, the abandoned one
    /// belonging to a frame this call does not share.
    /// </remarks>
    private static bool TrySingleWordWide(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text, out int distance)
    {
        Span<ulong> peq = stackalloc ulong[256];
        Span<char> keys = stackalloc char[WideAlphabet.Capacity];
        Span<ulong> masks = stackalloc ulong[WideAlphabet.Capacity];

        WideAlphabet.Fill(pattern, peq, keys, masks);
        distance = Scan(peq, keys, masks, pattern.Length, text);
        return true;
    }

    /// <summary>One machine word of Myers' recurrence, over the pattern's equality table.</summary>
    /// <remarks>Inlined rather than called: this is the hot loop, and a call here costs measurably.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Scan(
        ReadOnlySpan<ulong> peq, ReadOnlySpan<char> keys, ReadOnlySpan<ulong> masks,
        int m, ReadOnlySpan<char> text)
    {
        ulong vp = m == 64 ? ulong.MaxValue : (1UL << m) - 1;
        ulong vn = 0;
        int score = m;
        ulong highBit = 1UL << (m - 1);

        for (int j = 0; j < text.Length; j++)
        {
            char tc = text[j];
            ulong eq = tc <= 0xFF ? peq[tc] : WideAlphabet.Lookup(keys, masks, tc);

            ulong xv = eq | vn;
            ulong xh = (((eq & vp) + vp) ^ vp) | eq;
            ulong ph = vn | ~(xh | vp);
            ulong mh = vp & xh;

            if ((ph & highBit) != 0)
            {
                score++;
            }
            else if ((mh & highBit) != 0)
            {
                score--;
            }

            ph = (ph << 1) | 1UL;
            mh <<= 1;
            vp = mh | ~(xv | ph);
            vn = ph & xv;
        }

        return score;
    }

    /// <summary>
    /// The blocked (multi-word) variant, for patterns longer than one machine word.
    /// </summary>
    /// <remarks>
    /// The bit vectors span <c>⌈m/64⌉</c> words with horizontal deltas carried
    /// between them — the only real difference from <see cref="TrySingleWord"/>;
    /// only the last word's bit at <c>(m-1) mod 64</c> moves the score. See
    /// <c>docs/decisions/0004-levenshtein-myers-backlog.md</c> for the measured cost.
    /// </remarks>
    private static bool TryBlocked(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text, out int distance)
    {
        distance = 0;
        int m = pattern.Length;
        int blocks = (m + 63) / 64;

        // Peq is 256 x blocks: one equality mask per Latin-1 character per word.
        int slots = WideAlphabet.CapacityFor(m);
        int peqLength = (256 + slots) * blocks;
        ulong[] peqRented = ArrayPool<ulong>.Shared.Rent(peqLength);
        char[] keysRented = ArrayPool<char>.Shared.Rent(slots);
        ulong[] vpRented = ArrayPool<ulong>.Shared.Rent(blocks);
        ulong[] vnRented = ArrayPool<ulong>.Shared.Rent(blocks);
        try
        {
            Span<ulong> peq = peqRented.AsSpan(0, peqLength);
            Span<ulong> vp = vpRented.AsSpan(0, blocks);
            Span<ulong> vn = vnRented.AsSpan(0, blocks);
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
                    row = 256 + k;
                }
                peq[(row * blocks) + (i >> 6)] |= 1UL << (i & 63);
            }

            for (int b = 0; b < blocks; b++)
            {
                vp[b] = ulong.MaxValue;
                vn[b] = 0;
            }

            int score = m;
            ulong lastBit = 1UL << ((m - 1) & 63);
            int last = blocks - 1;

            for (int j = 0; j < text.Length; j++)
            {
                char tc = text[j];
                int peqBase = tc <= 0xFF
                    ? tc * blocks
                    : WideAlphabet.BlockBase(keys, tc, blocks, 256);

                // D[i][0] = i, so the horizontal delta entering the first word is +1.
                ulong hp = 1UL;
                ulong hn = 0UL;

                for (int b = 0; b < blocks; b++)
                {
                    ulong eq = peqBase >= 0 ? peq[peqBase + b] : 0UL;
                    ulong pv = vp[b];
                    ulong mv = vn[b];

                    ulong xv = eq | mv;
                    eq |= hn;
                    ulong xh = (((eq & pv) + pv) ^ pv) | eq;

                    ulong ph = mv | ~(xh | pv);
                    ulong mh = pv & xh;

                    if (b == last)
                    {
                        if ((ph & lastBit) != 0)
                        {
                            score++;
                        }
                        else if ((mh & lastBit) != 0)
                        {
                            score--;
                        }
                    }

                    // Bit 63 leaves this word and enters the next.
                    ulong hpOut = ph >> 63;
                    ulong hnOut = mh >> 63;

                    ph = (ph << 1) | hp;
                    mh = (mh << 1) | hn;

                    vp[b] = mh | ~(xv | ph);
                    vn[b] = ph & xv;

                    hp = hpOut;
                    hn = hnOut;
                }
            }

            distance = score;
            return true;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(keysRented);
            ArrayPool<ulong>.Shared.Return(peqRented);
            ArrayPool<ulong>.Shared.Return(vpRented);
            ArrayPool<ulong>.Shared.Return(vnRented);
        }
    }
}
