namespace DataNet.Metrics.Internal;

/// <summary>
/// The averaged weighted percentile at 50 %, which is what scikit-learn's
/// <c>median_absolute_error</c> takes when it is given sample weights.
/// </summary>
/// <remarks>
/// <para>
/// It is not "the value at the halfway point". On four uniformly weighted
/// residuals scikit-learn returns the mean of the two middle values, so the
/// rule averages two percentiles: the first whose cumulative weight reaches
/// half the total, and the one just past the last that comes <em>within one
/// machine epsilon</em> of it. Where the two coincide — every odd count, and
/// every lopsided weighting — the average is that single value, so there is no
/// separate branch for it.
/// </para>
/// <para>
/// The epsilon is scikit-learn's own: it compares its <c>fraction_above</c>
/// against <c>np.finfo(np.float64).eps</c> rather than against zero, and that
/// tolerance is load-bearing. On a uniform fractional weight — <c>[0.1] × 10</c>,
/// or numpy's own <c>np.ones(n) / n</c> — the cumulative sum overshoots half the
/// total by a few units in the last place, and an exact test then takes one
/// order statistic where scikit-learn averages two. Measured, that is the
/// difference between 4.5 and 4.0 on the residuals <c>0…9</c>.
/// </para>
/// <para>
/// It does <em>not</em> follow that a uniform weight always reproduces the
/// unweighted median. Where the overshoot is wider than an epsilon it does not,
/// in scikit-learn either: <c>[0.7] × 10</c> over those same residuals gives
/// 5.0 on this path against 4.5 on the unweighted one. Both are reproduced,
/// which is the point — the tolerance is a width, not a licence to average
/// whenever the weights happen to be equal.
/// </para>
/// </remarks>
internal static class WeightedPercentile
{
    /// <summary>
    /// numpy's machine epsilon, <c>np.finfo(np.float64).eps</c> — the tolerance
    /// scikit-learn allows the cumulative weight to overshoot the halfway point
    /// by before it stops averaging.
    /// </summary>
    /// <remarks>
    /// Absolute, not relative, because scikit-learn's is absolute. It is not
    /// <see cref="double.Epsilon"/>, which is the smallest positive subnormal
    /// and 292 orders of magnitude smaller; and .NET has no built-in constant
    /// for machine epsilon, which is why this is written out — the same value,
    /// for a different reason, as the clamp in
    /// <see cref="MeanAbsolutePercentageError"/>.
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

        // Array.Sort(keys, items) sorts by the FIRST array. The residuals are
        // the sort key here, so values must lead and weights must follow —
        // Array.Sort(weights, values) would sort by weight instead, which is
        // not the rule scikit-learn implements.
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
            // QuickSelect leaves values[lower] holding the correct order
            // statistic with everything to its right at or above it (the
            // partition invariant), and upper is always lower + 1 when the
            // count is even (an odd count takes the branch above instead).
            // So the other middle value is just the minimum of that
            // remainder, found with one linear scan instead of a second
            // full selection.
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

        // Median-of-three pivoting still degrades to O(n^2) on adversarial
        // input (e.g. an organ-pipe sequence, or many repeats of one value).
        // Introselect bounds the damage: once partitioning has run more than a
        // budget proportional to log2(n), fall back to sorting whatever range
        // remains, turning the worst case into O(n log n) — the same guarantee
        // that backs numpy's own introselect-based median.
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

        // Order the two endpoints and the midpoint so the middle value of the
        // three becomes the pivot, then move it to `to`. This is what defeats
        // already-sorted and reverse-sorted input, which drive a plain
        // first-or-last pivot straight to its O(n^2) worst case.
        if (values[mid] < values[from])
        {
            Swap(values, from, mid);
        }

        if (values[to] < values[from])
        {
            Swap(values, from, to);
        }

        // An earlier version paired an "is values[to] the smaller one" check
        // with an unconditional swap that followed it unconditionally — two
        // swaps of the same pair of positions cancel exactly whenever the
        // check fired, since swapping twice is the identity. This single,
        // oppositely-phrased check reaches the same result without ever
        // performing that wasted pair of swaps.
        if (values[mid] < values[to])
        {
            Swap(values, mid, to);
        }

        double pivot = values[to];
        int storeIndex = from;
        for (int i = from; i < to; i++)
        {
            // Unconditional swap, conditional advance. This looks wrong and is not:
            // storeIndex always points at the first slot not yet known to hold a
            // value below the pivot, so when values[i] is not below it either, the
            // two positions hold interchangeable values and the swap is a no-op in
            // meaning if not in memory. The comparison then advances the index by
            // one or zero, which the JIT emits as a setcc rather than a branch --
            // and that branch is the point: it is taken about half the time on the
            // data this metric sees, which is the worst case for a predictor.
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
    /// the last index still at or under half is <c>n / 2</c>. This is the single
    /// place that derives that pair — <see cref="Average"/>'s weighted-cumulative
    /// loop collapses to exactly these two indices when <c>weights</c> is
    /// <see langword="null"/>, and <see cref="MedianUnweighted"/> needs the same
    /// pair before selection to know which ranks to quickselect for. Keeping the
    /// arithmetic here, rather than in both places, is what stops the weighted
    /// and unweighted paths from silently drifting apart.
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
            // Within one machine epsilon of half, not exactly at or below it.
            // scikit-learn's test is `fraction_above > eps`, where
            // fraction_above is cdf[i] - half; anything that fails it averages.
            // Writing this as `cumulative <= half` instead refuses to average
            // whenever the cumulative sum overshoots by one unit in the last
            // place, which is what every uniform fractional weight does.
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
