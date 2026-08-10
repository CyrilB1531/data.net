namespace DataNet.Metrics.Internal;

/// <summary>
/// The averaged weighted percentile at 50 %, which is what scikit-learn's
/// <c>median_absolute_error</c> takes when it is given sample weights.
/// </summary>
/// <remarks>
/// It is not "the value at the halfway point". On four uniformly weighted
/// residuals scikit-learn returns the mean of the two middle values, so the
/// rule averages two percentiles: the first whose cumulative weight reaches
/// half the total, and the one just past the last that stays at or below it.
/// Where the two coincide — every odd count, and every lopsided weighting — the
/// average is that single value, so there is no separate branch for it.
/// </remarks>
internal static class WeightedPercentile
{
    /// <summary>The median of <paramref name="values"/> under <paramref name="weights"/>.</summary>
    /// <param name="values">The values. Sorted in place; pass a copy if the caller still needs the order.</param>
    /// <param name="weights">One weight per value, or empty for weight 1 each.</param>
    public static double Median(double[] values, double[] weights)
    {
        if (weights.Length == 0)
        {
            Array.Sort(values);
            return Average(values, null);
        }

        // Array.Sort(keys, items) sorts by the FIRST array. The residuals are
        // the sort key here, so values must lead and weights must follow —
        // Array.Sort(weights, values) would sort by weight instead, which is
        // not the rule scikit-learn implements.
        Array.Sort(values, weights);
        return Average(values, weights);
    }

    private static double Average(double[] values, double[]? weights)
    {
        double total = 0.0;
        for (int i = 0; i < values.Length; i++)
        {
            total += weights is null ? 1.0 : weights[i];
        }

        double half = total / 2.0;
        int lower = values.Length - 1;
        int upper = 0;
        double cumulative = 0.0;
        bool lowerFound = false;

        for (int i = 0; i < values.Length; i++)
        {
            cumulative += weights is null ? 1.0 : weights[i];
            if (!lowerFound && cumulative >= half)
            {
                lower = i;
                lowerFound = true;
            }
            if (cumulative <= half)
            {
                upper = i + 1;
            }
        }

        if (upper >= values.Length)
        {
            upper = values.Length - 1;
        }

        return (values[lower] + values[upper]) / 2.0;
    }
}
