namespace Lodestar.Stats;

/// <summary>Adjusting a family of p-values for the number of tests in it.</summary>
/// <remarks>
/// Twenty tests at the five-percent level produce one significant result by
/// chance alone. These three rules answer that, and they answer different
/// questions: Bonferroni controls the chance of <i>any</i> false positive,
/// while the Benjamini rules control the expected <i>proportion</i> of false
/// positives among the results called significant.
///
/// Each returns adjusted p-values in the input's own order, so an adjusted
/// value can be compared against the level the caller already had in mind.
/// </remarks>
public static class MultipleComparisons
{
    /// <summary>Multiplies each p-value by the family size, clamped at one.</summary>
    /// <param name="pValues">The family; at least one value, each in <c>[0, 1]</c>.</param>
    /// <returns>The adjusted p-values, in the input's order.</returns>
    /// <exception cref="ArgumentException"><paramref name="pValues"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A value is NaN or outside <c>[0, 1]</c>.</exception>
    public static double[] Bonferroni(ReadOnlySpan<double> pValues)
    {
        Validate(pValues);

        double[] adjusted = new double[pValues.Length];
        for (int i = 0; i < pValues.Length; i++)
        {
            adjusted[i] = Math.Min(1.0, pValues[i] * pValues.Length);
        }

        return adjusted;
    }

    /// <summary>The Benjamini-Hochberg step-up procedure.</summary>
    /// <param name="pValues">The family; at least one value, each in <c>[0, 1]</c>.</param>
    /// <returns>The adjusted p-values, in the input's order.</returns>
    /// <exception cref="ArgumentException"><paramref name="pValues"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A value is NaN or outside <c>[0, 1]</c>.</exception>
    public static double[] BenjaminiHochberg(ReadOnlySpan<double> pValues) =>
        StepUp(pValues, factor: 1.0);

    /// <summary>The Benjamini-Yekutieli procedure, valid under any dependence.</summary>
    /// <remarks>
    /// Benjamini-Hochberg assumes the tests are independent or positively
    /// dependent. Yekutieli's correction drops that assumption at the price of a
    /// harmonic-sum factor, so its adjusted values are never smaller.
    /// </remarks>
    /// <param name="pValues">The family; at least one value, each in <c>[0, 1]</c>.</param>
    /// <returns>The adjusted p-values, in the input's order.</returns>
    /// <exception cref="ArgumentException"><paramref name="pValues"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A value is NaN or outside <c>[0, 1]</c>.</exception>
    public static double[] BenjaminiYekutieli(ReadOnlySpan<double> pValues)
    {
        Validate(pValues);

        double harmonic = 0.0;
        for (int i = 1; i <= pValues.Length; i++)
        {
            harmonic += 1.0 / i;
        }

        return StepUp(pValues, harmonic);
    }

    private static double[] StepUp(ReadOnlySpan<double> pValues, double factor)
    {
        Validate(pValues);

        int n = pValues.Length;
        int[] order = new int[n];
        double[] sorted = new double[n];
        for (int i = 0; i < n; i++)
        {
            order[i] = i;
            sorted[i] = pValues[i];
        }

        Array.Sort(sorted, order);

        double[] adjusted = new double[n];
        double running = 1.0;

        // Walking down from the largest and keeping the running minimum is what
        // makes the result monotone: without it a p-value could be adjusted below
        // a smaller one, and the ordering the caller reads would be a lie.
        for (int rank = n; rank >= 1; rank--)
        {
            double scaled = sorted[rank - 1] * n * factor / rank;
            running = Math.Min(running, scaled);
            adjusted[order[rank - 1]] = Math.Min(1.0, running);
        }

        return adjusted;
    }

    private static void Validate(ReadOnlySpan<double> pValues)
    {
        if (pValues.Length == 0)
        {
            throw new ArgumentException("The family of p-values is empty.", nameof(pValues));
        }

        for (int i = 0; i < pValues.Length; i++)
        {
            if (double.IsNaN(pValues[i]) || pValues[i] < 0.0 || pValues[i] > 1.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pValues), pValues[i], $"p-value {i} is not a probability.");
            }
        }
    }
}
