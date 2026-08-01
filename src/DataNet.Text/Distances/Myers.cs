namespace DataNet.Text.Distances;

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
        if (m == 0 || m > 64)
        {
            return false;
        }

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
}
