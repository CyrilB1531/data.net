namespace Lodestar.Metrics.Internal;

/// <summary>
/// The averaged weighted percentile at 50 %, which is what scikit-learn's
/// <c>median_absolute_error</c> takes when it is given sample weights.
/// </summary>
/// <remarks>
/// Not "the value at the halfway point": <see cref="Average"/> always averages
/// two order statistics, coinciding on one when they land on the same index,
/// chosen within scikit-learn's own epsilon tolerance rather than exactly at
/// half. See docs/decisions/0024 for the measured divergence that produces.
/// </remarks>
internal static class WeightedPercentile
{
    /// <summary>
    /// numpy's machine epsilon, <c>np.finfo(np.float64).eps</c> — the tolerance
    /// scikit-learn allows the cumulative weight to overshoot the halfway point
    /// by before it stops averaging. See docs/decisions/0024.
    /// </summary>
    /// <remarks>
    /// Not <see cref="double.Epsilon"/>, the smallest positive subnormal and 292
    /// orders of magnitude smaller. .NET has no built-in constant for this.
    /// </remarks>
    private const double MachineEpsilon = 2.220446049250313e-16;

    /// <summary>The median of <paramref name="values"/> under <paramref name="weights"/>.</summary>
    /// <param name="values">The values. Sorted in place; pass a copy if the caller still needs the order.</param>
    /// <param name="weights">One weight per value, or empty for weight 1 each.</param>
    public static double Median(double[] values, double[] weights)
    {
        if (weights.Length == 0)
        {
            return MedianUnweighted(values);
        }

        // Array.Sort(keys, items) sorts by the first array, so values (the sort
        // key) must lead weights — the reverse call would sort by weight instead.
        Array.Sort(values, weights);
        return Average(values, weights);
    }

    /// <summary>
    /// The unweighted median without a full sort. <see cref="Average"/> stays the
    /// single place that decides which order statistic(s) the median needs — this
    /// only selects those positions with quickselect and then defers to it, the
    /// same way the weighted path defers to it after <see cref="Array.Sort(Array)"/>.
    /// </summary>
    private static double MedianUnweighted(double[] values)
    {
        int n = values.Length;
        MedianIndices(n, out int lower, out int upper);

        QuickSelect(values, 0, n - 1, lower);

        if (upper != lower)
        {
            // The partition invariant already leaves values[lower..] at or above
            // the order statistic, so the other middle value is just its minimum.
            int minIndex = lower + 1;
            for (int i = lower + 2; i < n; i++)
            {
                if (values[i] < values[minIndex])
                {
                    minIndex = i;
                }
            }

            if (minIndex != upper)
            {
                Swap(values, upper, minIndex);
            }
        }

        return Average(values, null);
    }

    /// <summary>
    /// Partially orders <c>values[from..to]</c> so that <c>values[k]</c> holds the
    /// value that would sit at index <paramref name="k"/> if the range were fully
    /// sorted, with everything to its left <c>&lt;=</c> it and everything to its
    /// right <c>&gt;=</c> it. Everything else in the range is left unordered.
    /// </summary>
    private static void QuickSelect(double[] values, int from, int to, int k)
    {
        // Below this width, a full sort is cheap and sidesteps the edge cases a
        // three-point pivot has on ranges that barely hold three positions.
        const int InsertionCutoff = 12;

        int width = to - from + 1;
        if (width <= InsertionCutoff)
        {
            Array.Sort(values, from, width);
            return;
        }

        // Median-of-three still degrades to O(n^2) on adversarial input (organ
        // pipe, many repeats); this budget bounds it. See docs/decisions/0025.
        int budget = (2 * FloorLog2(width)) + 4;

        while (true)
        {
            width = to - from + 1;
            if (width <= InsertionCutoff)
            {
                Array.Sort(values, from, width);
                return;
            }

            if (budget-- <= 0)
            {
                Array.Sort(values, from, width);
                return;
            }

            int pivotIndex = Partition(values, from, to);
            if (k == pivotIndex)
            {
                return;
            }

            if (k < pivotIndex)
            {
                to = pivotIndex - 1;
            }
            else
            {
                from = pivotIndex + 1;
            }
        }
    }

    /// <summary>Lomuto partition of <c>values[from..to]</c> around a median-of-three pivot.</summary>
    private static int Partition(double[] values, int from, int to)
    {
        int mid = from + ((to - from) / 2);

        // The middle of the two endpoints and the midpoint becomes the pivot,
        // defeating the sorted/reverse-sorted input a first-or-last pivot fails on.
        if (values[mid] < values[from])
        {
            Swap(values, from, mid);
        }

        if (values[to] < values[from])
        {
            Swap(values, from, to);
        }

        // This single, oppositely-phrased check reaches the same ordering as
        // pairing a check with an unconditional swap, without the wasted pair.
        if (values[mid] < values[to])
        {
            Swap(values, mid, to);
        }

        double pivot = values[to];
        int storeIndex = from;
        for (int i = from; i < to; i++)
        {
            // Unconditional swap, then advance by the comparison, not a branch —
            // see docs/decisions/0025. `value` must be read before the swap.
            double value = values[i];
            values[i] = values[storeIndex];
            values[storeIndex] = value;
            storeIndex += value < pivot ? 1 : 0;
        }

        Swap(values, storeIndex, to);
        return storeIndex;
    }

    /// <summary>Floor of log2, for a strictly positive <paramref name="value"/>.</summary>
    private static int FloorLog2(int value)
    {
        int bits = 0;
        while (value > 1)
        {
            value >>= 1;
            bits++;
        }

        return bits;
    }

    private static void Swap(double[] values, int i, int j)
    {
        (values[i], values[j]) = (values[j], values[i]);
    }

    /// <summary>
    /// The pair of order-statistic indices the median needs when every weight is
    /// 1: the cumulative count crosses half the total at <c>(n - 1) / 2</c>, and
    /// the last index still at or under half is <c>n / 2</c>. The single place
    /// that derives that pair — <see cref="Average"/>'s weighted-cumulative loop
    /// collapses to it when <c>weights</c> is <see langword="null"/>, and
    /// <see cref="MedianUnweighted"/> selects for the same pair before sorting —
    /// so the weighted and unweighted paths cannot silently drift apart.
    /// </summary>
    private static void MedianIndices(int n, out int lower, out int upper)
    {
        lower = (n - 1) / 2;
        upper = n / 2;
    }

    private static double Average(double[] values, double[]? weights)
    {
        if (weights is null)
        {
            MedianIndices(values.Length, out int lower, out int upper);
            return (values[lower] + values[upper]) / 2.0;
        }

        double total = 0.0;
        for (int i = 0; i < values.Length; i++)
        {
            total += weights[i];
        }

        double half = total / 2.0;
        int weightedLower = values.Length - 1;
        int weightedUpper = 0;
        double cumulative = 0.0;
        bool lowerFound = false;

        for (int i = 0; i < values.Length; i++)
        {
            cumulative += weights[i];
            if (!lowerFound && cumulative >= half)
            {
                weightedLower = i;
                lowerFound = true;
            }
            // Within one epsilon of half, not exactly at or below it: see
            // docs/decisions/0024 for why an exact test picks the wrong pair.
            if (cumulative - half <= MachineEpsilon)
            {
                weightedUpper = i + 1;
            }
        }

        if (weightedUpper >= values.Length)
        {
            weightedUpper = values.Length - 1;
        }

        return (values[weightedLower] + values[weightedUpper]) / 2.0;
    }
}
