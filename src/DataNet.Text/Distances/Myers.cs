using System.Buffers;

namespace DataNet.Text.Distances;


// SonarLint S3776: cognitive complexity. TryBlocked is a transcription of
// Hyyro's blocked formulation — the nested loop and its carry threading ARE the
// algorithm, and splitting them would break the one-to-one reading against the
// paper that makes a bit-manipulation kernel auditable at all. It is also the
// hot path: helper calls here cost measurably.
#pragma warning disable S3776
/// <summary>
/// Myers' bit-parallel edit-distance algorithm (single machine word).
/// </summary>
/// <remarks>
/// <para>
/// Computes the Levenshtein distance in O(n·⌈m/w⌉) rather than the DP's O(n·m),
/// following Hyyrö's formulation. This single-word variant applies when the
/// shorter operand (the "pattern") has length ≤ 64 and lies within Latin-1, which
/// covers the overwhelmingly common short-string case (names, identifiers) with
/// zero allocation.
/// </para>
/// <para>
/// It is a self-contained implementation of the published algorithm (Myers, JACM
/// 1999; Hyyrö 2003), not a transcription of any library source. The multi-word
/// (blocked) variant for long patterns is tracked in
/// <c>docs/decisions/0004-levenshtein-myers-backlog.md</c>.
/// </para>
/// </remarks>
internal static class Myers
{
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

    private static bool TrySingleWord(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text, out int distance)
    {
        distance = 0;
        int m = pattern.Length;

        // Peq[c] has bit i set where pattern[i] == c. A 256-entry table keeps the
        // pattern within Latin-1; any text character ≥ 256 cannot occur in such a
        // pattern, so its equality mask is simply zero.
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
    /// <para>
    /// The bit vectors span <c>⌈m/64⌉</c> words, and the horizontal deltas are
    /// carried from each word into the next, which is the only real difference from
    /// the single-word formulation. Cost is <c>O(n·⌈m/64⌉)</c> against the DP's
    /// <c>O(n·m)</c>: at a 512-character pattern that is 64 machine operations
    /// replaced by one.
    /// </para>
    /// <para>
    /// Only the last block's bit at position <c>(m-1) mod 64</c> moves the score;
    /// bits above it in that word are never read, so leaving them set costs nothing
    /// — carries propagate upward only.
    /// </para>
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
