namespace Lodestar.Stats.Internal;

/// <summary>Mid-ranks, and the tie-correction term the rank tests share.</summary>
/// <remarks>
/// The ranks come back indexed by the input's own positions rather than sorted,
/// because every caller here sums the ranks belonging to one of two interleaved
/// samples and would otherwise have to invert the ordering itself.
/// </remarks>
internal static class Ranks
{
    internal static double[] Average(ReadOnlySpan<double> values)
    {
        if (values.Length == 0)
        {
            throw new ArgumentException("Cannot rank an empty sample.", nameof(values));
        }

        int n = values.Length;
        int[] order = new int[n];
        double[] sorted = new double[n];
        for (int i = 0; i < n; i++)
        {
            order[i] = i;
            sorted[i] = values[i];
        }

        Array.Sort(sorted, order);

        double[] ranks = new double[n];
        int start = 0;
        while (start < n)
        {
            int end = start;

            // S1244: a tie group is defined by literally equal input values, not
            // nearby ones -- a tolerance would merge distinct values into a false
            // tie or split a true one.
#pragma warning disable S1244
            while (end + 1 < n && sorted[end + 1] == sorted[start])
#pragma warning restore S1244
            {
                end++;
            }

            // Ranks are 1-based, so the group spanning positions [start, end]
            // occupies ranks start+1 .. end+1 and every member takes their mean.
            double shared = (start + end + 2) / 2.0;
            for (int i = start; i <= end; i++)
            {
                ranks[order[i]] = shared;
            }

            start = end + 1;
        }

        return ranks;
    }

    internal static double TieCorrection(ReadOnlySpan<double> values)
    {
        if (values.Length == 0)
        {
            return 0.0;
        }

        double[] sorted = values.ToArray();
        Array.Sort(sorted);

        double correction = 0.0;
        int start = 0;
        while (start < sorted.Length)
        {
            int end = start;

            // S1244: same reasoning as Average -- a tie group is defined by
            // literally equal input values.
#pragma warning disable S1244
            while (end + 1 < sorted.Length && sorted[end + 1] == sorted[start])
#pragma warning restore S1244
            {
                end++;
            }

            double t = end - start + 1;
            correction += (t * t * t) - t;
            start = end + 1;
        }

        return correction;
    }

    internal static bool HasTies(ReadOnlySpan<double> values)
    {
        if (values.Length < 2)
        {
            return false;
        }

        double[] sorted = values.ToArray();
        Array.Sort(sorted);

        // S1244: same reasoning as Average -- a tie is defined by literally
        // equal input values.
#pragma warning disable S1244
        for (int i = 1; i < sorted.Length; i++)
        {
            if (sorted[i] == sorted[i - 1])
            {
                return true;
            }
        }
#pragma warning restore S1244

        return false;
    }

    // Checked ahead of Average, never inside it: NaN sorts to the front under
    // Array.Sort and would otherwise take a finite rank rather than none.
    internal static bool HasNaN(ReadOnlySpan<double> values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (double.IsNaN(values[i]))
            {
                return true;
            }
        }

        return false;
    }
}
