using System.Buffers;

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
/// <c>O(n·⌈m/w⌉)</c> against the DP's <c>O(n·m)</c> (Myers 1999; Hyyrö 2003),
/// restricted to Latin-1 patterns; remaining backlog in
/// <c>docs/decisions/0004-levenshtein-myers-backlog.md</c>.
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

    /// <summary>The free-slot marker. No code point is negative.</summary>
    private const int Empty = -1;

    /// <summary>
    /// Attempts to compute the Levenshtein distance between <paramref name="pattern"/>
    /// and <paramref name="text"/> using the single-word algorithm.
    /// </summary>
    /// <returns>
    /// <c>true</c> and the distance when the fast path applies; <c>false</c> when
    /// the caller must fall back to the DP (pattern empty, longer than 64, or
    /// containing a character above U+00FF).
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
            keys.Fill(Empty);

            int distinct = 0;
            for (int i = 0; i < m; i++)
            {
                int probe = Probe(keys, pattern[i]);
                if (keys[probe] == Empty)
                {
                    if (distinct == DenseAlphabet)
                    {
                        return false; // more symbols than the dense alphabet holds
                    }
                    keys[probe] = pattern[i];
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
    /// 255 of 512 entries are ever occupied, the 256th distinct symbol being
    /// refused rather than stored. The multiply is Knuth's, because code points
    /// cluster hard -- an emoji pattern lives inside U+1F300..U+1FAFF, and masking
    /// those low bits alone would pile every symbol into a few slots.
    /// </remarks>
    private static int Probe(Span<int> keys, int symbol)
    {
        int index = (int)(((uint)symbol * 2654435761u) >> 23) & (SlotCapacity - 1);
        while (keys[index] != Empty && keys[index] != symbol)
        {
            index = (index + 1) & (SlotCapacity - 1);
        }
        return index;
    }

    private static bool TrySingleWord(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text, out int distance)
    {
        distance = 0;
        int m = pattern.Length;

        // Peq[c] has bit i set where pattern[i] == c; a 256-entry table covers Latin-1,
        // so any text character above it has an all-zero (no-match) mask.
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

        ulong vp = m == 64 ? ulong.MaxValue : (1UL << m) - 1;
        ulong vn = 0;
        int score = m;
        ulong highBit = 1UL << (m - 1);

        for (int j = 0; j < text.Length; j++)
        {
            char tc = text[j];
            ulong eq = tc <= 0xFF ? peq[tc] : 0UL;

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

        distance = score;
        return true;
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
        int peqLength = 256 * blocks;
        ulong[] peqRented = ArrayPool<ulong>.Shared.Rent(peqLength);
        ulong[] vpRented = ArrayPool<ulong>.Shared.Rent(blocks);
        ulong[] vnRented = ArrayPool<ulong>.Shared.Rent(blocks);
        try
        {
            Span<ulong> peq = peqRented.AsSpan(0, peqLength);
            Span<ulong> vp = vpRented.AsSpan(0, blocks);
            Span<ulong> vn = vnRented.AsSpan(0, blocks);
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
                vp[b] = ulong.MaxValue;
                vn[b] = 0;
            }

            int score = m;
            ulong lastBit = 1UL << ((m - 1) & 63);
            int last = blocks - 1;

            for (int j = 0; j < text.Length; j++)
            {
                char tc = text[j];
                int peqBase = tc <= 0xFF ? tc * blocks : -1;

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
            ArrayPool<ulong>.Shared.Return(peqRented);
            ArrayPool<ulong>.Shared.Return(vpRented);
            ArrayPool<ulong>.Shared.Return(vnRented);
        }
    }
}
