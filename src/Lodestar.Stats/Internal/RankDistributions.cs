namespace Lodestar.Stats.Internal;

/// <summary>The exact null distributions of the two rank statistics.</summary>
/// <remarks>
/// Both are counted by dynamic programming rather than by enumerating the
/// arrangements: the signed-rank distribution for n = 25 has 2^25 assignments
/// against 325 table entries.
/// The counts are doubles because they exceed a long: the signed-rank total for
/// n = 60 is 2^60. Only ratios against the total are ever taken, so a 53-bit
/// mantissa loses nothing a p-value compared at 1e-9 relative can see.
/// </remarks>
internal static class RankDistributions
{
    /// <summary>
    /// How many arrangements of two samples of size <paramref name="n"/> and
    /// <paramref name="m"/> give each value of the Mann-Whitney U statistic.
    /// </summary>
    internal static double[] MannWhitneyCounts(int n, int m)
    {
        if (n < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(n), n, "A sample size cannot be negative.");
        }
        if (m < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(m), m, "A sample size cannot be negative.");
        }

        // f(i, j, u): arrangements of i of the first sample and j of the second with
        // statistic u, rolling the i dimension to keep the table at (m+1) x (n*m+1) rather than cubing it.
        int max = n * m;

        // CA1814 (prefer jagged arrays): the table is rectangular by
        // construction and rebuilt wholesale every i, so a jagged array would
        // add m+1 row allocations to every one of the n iterations for no
        // benefit -- nothing here is ragged.
#pragma warning disable CA1814
        double[,] previous = new double[m + 1, max + 1];
        for (int j = 0; j <= m; j++)
        {
            previous[j, 0] = 1.0;
        }

        for (int i = 1; i <= n; i++)
        {
            double[,] current = new double[m + 1, max + 1];
            current[0, 0] = 1.0;

            for (int j = 1; j <= m; j++)
            {
                for (int u = 0; u <= max; u++)
                {
                    // Either the next largest value comes from the first sample, which adds
                    // j to the statistic, or from the second, which adds nothing.
                    double fromFirst = u >= j ? previous[j, u - j] : 0.0;
                    current[j, u] = fromFirst + current[j - 1, u];
                }
            }

            previous = current;
        }
#pragma warning restore CA1814

        double[] counts = new double[max + 1];
        for (int u = 0; u <= max; u++)
        {
            counts[u] = previous[m, u];
        }

        return counts;
    }

    /// <summary>
    /// How many sign assignments of the ranks <c>1..n</c> give each value of the
    /// positive-rank sum W.
    /// </summary>
    internal static double[] SignedRankCounts(int n)
    {
        if (n < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(n), n, "A sample size cannot be negative.");
        }

        int max = n * (n + 1) / 2;
        double[] counts = new double[max + 1];
        counts[0] = 1.0;

        // Multiplying by (1 + x^rank) one rank at a time, in place: descending
        // so a rank is never counted twice within its own pass.
        int reach = 0;
        for (int rank = 1; rank <= n; rank++)
        {
            reach += rank;
            for (int w = reach; w >= rank; w--)
            {
                counts[w] += counts[w - rank];
            }
        }

        return counts;
    }
}
