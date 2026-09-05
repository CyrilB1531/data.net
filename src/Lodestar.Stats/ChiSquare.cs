using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>Pearson's chi-square: goodness of fit, and independence in a contingency table.</summary>
public static class ChiSquare
{
    /// <summary>Tests observed counts against an expected distribution.</summary>
    /// <param name="observed">The observed counts; at least two categories.</param>
    /// <param name="expected">
    /// The expected counts, which must sum to the observed total. Omit them for
    /// a uniform expectation, which is what <c>scipy.stats.chisquare</c> does
    /// with <c>f_exp=None</c>.
    /// </param>
    /// <returns>The statistic and the upper-tail p-value.</returns>
    /// <exception cref="ArgumentException">
    /// Fewer than two categories, mismatched lengths, a non-positive expectation,
    /// or expectations that do not sum to the observations.
    /// </exception>
    public static TestResult GoodnessOfFit(
        ReadOnlySpan<double> observed, ReadOnlySpan<double> expected = default)
    {
        if (observed.Length < 2)
        {
            throw new ArgumentException(
                $"A goodness-of-fit test needs at least two categories; got {observed.Length}.",
                nameof(observed));
        }

        double observedTotal = 0.0;
        for (int i = 0; i < observed.Length; i++)
        {
            observedTotal += observed[i];
        }

        double[] target = expected.IsEmpty
            ? UniformExpectation(observed.Length, observedTotal)
            : ExplicitExpectation(observed.Length, expected, observedTotal);

        double statistic = 0.0;
        for (int i = 0; i < observed.Length; i++)
        {
            double deviation = observed[i] - target[i];
            statistic += deviation * deviation / target[i];
        }

        int dof = observed.Length - 1;
        return new TestResult(statistic, Gamma.RegularizedQ(dof / 2.0, statistic / 2.0));
    }

    private static double[] UniformExpectation(int categories, double observedTotal)
    {
        double[] target = new double[categories];
        double uniform = observedTotal / categories;
        for (int i = 0; i < target.Length; i++)
        {
            target[i] = uniform;
        }

        return target;
    }

    private static double[] ExplicitExpectation(
        int observedLength, ReadOnlySpan<double> expected, double observedTotal)
    {
        if (expected.Length != observedLength)
        {
            throw new ArgumentException(
                $"There are {observedLength} observations and {expected.Length} expectations.",
                nameof(expected));
        }

        double[] target = new double[expected.Length];
        double expectedTotal = 0.0;
        for (int i = 0; i < expected.Length; i++)
        {
            if (expected[i] <= 0.0)
            {
                throw new ArgumentException(
                    $"Expectation {i} is {expected[i]}; the statistic divides by it.",
                    nameof(expected));
            }

            target[i] = expected[i];
            expectedTotal += expected[i];
        }

        // scipy refuses the same way: an expectation summing elsewhere is not a
        // distribution over these categories, so the p-value would mean nothing.
        if (Math.Abs(expectedTotal - observedTotal) > 1e-8 * Math.Abs(observedTotal))
        {
            throw new ArgumentException(
                $"The expectations sum to {expectedTotal} and the observations to {observedTotal}.",
                nameof(expected));
        }

        return target;
    }

    /// <summary>Tests a contingency table for independence of its two factors.</summary>
    /// <param name="table">The observed counts, row-major and rectangular.</param>
    /// <param name="continuity">
    /// Whether to apply Yates's correction. It is defined for 2x2 tables only,
    /// so asking for it on any other shape changes nothing — the same rule
    /// <c>scipy.stats.chi2_contingency</c> follows with <c>correction=True</c>.
    /// </param>
    /// <returns>The statistic, the p-value, the degrees of freedom and the expected table.</returns>
    /// <exception cref="ArgumentException">
    /// The table is empty, ragged, holds a negative count, or has a zero row or column total.
    /// </exception>
    // S2368: the table arrives from the caller already in this shape -- that is
    // how scipy.stats.chi2_contingency takes it, and how Chi2ContingencyResult
    // hands the expected table back. Wrapping one side and not the other buys
    // no safety, only a conversion at the boundary (same reasoning as
    // Chi2ContingencyResult's own suppression in TestResult.cs).
#pragma warning disable S2368
    public static Chi2ContingencyResult Contingency(
        double[][] table, Continuity continuity = Continuity.Applied)
#pragma warning restore S2368
    {
        Guard.NotNull(table);

        if (table.Length < 2 || table[0] is null || table[0].Length < 2)
        {
            throw new ArgumentException(
                "A contingency table needs at least two rows and two columns.", nameof(table));
        }

        int rows = table.Length;
        int columns = table[0].Length;

        (double[] rowTotals, double[] columnTotals, double total) =
            ComputeMarginals(table, rows, columns);
        ValidateMarginals(table, rowTotals, columnTotals);

        double[][] expected = ComputeExpected(rowTotals, columnTotals, total, rows, columns);
        bool yates = continuity == Continuity.Applied && rows == 2 && columns == 2;
        double statistic = ComputeStatistic(table, expected, rows, columns, yates);

        int dof = (rows - 1) * (columns - 1);
        double pValue = Gamma.RegularizedQ(dof / 2.0, statistic / 2.0);

        return new Chi2ContingencyResult(statistic, pValue, dof, expected);
    }

    private static (double[] RowTotals, double[] ColumnTotals, double Total) ComputeMarginals(
        double[][] table, int rows, int columns)
    {
        double[] rowTotals = new double[rows];
        double[] columnTotals = new double[columns];
        double total = 0.0;

        for (int i = 0; i < rows; i++)
        {
            if (table[i] is null || table[i].Length != columns)
            {
                throw new ArgumentException($"Row {i} is not {columns} wide.", nameof(table));
            }

            for (int j = 0; j < columns; j++)
            {
                double value = table[i][j];
                if (value < 0.0 || double.IsNaN(value))
                {
                    throw new ArgumentException(
                        $"Cell [{i}][{j}] is {value}; counts must be non-negative.", nameof(table));
                }

                rowTotals[i] += value;
                columnTotals[j] += value;
                total += value;
            }
        }

        return (rowTotals, columnTotals, total);
    }

    // A zero marginal makes the expectation zero, which the statistic divides
    // by: the factor has a level nothing was observed at, and the table needs
    // that level dropped before the test means anything. S1244: a marginal
    // that is exactly zero is the sentinel the guard exists for, not a value
    // with a tolerance band -- summing only non-negative cells, it cannot land
    // near zero without landing on it. S1172: table is read only through the
    // nameof calls below, so the thrown exception names the public parameter
    // the caller actually passed rather than one of this helper's own.
#pragma warning disable S1244, S1172
    private static void ValidateMarginals(double[][] table, double[] rowTotals, double[] columnTotals)
    {
        for (int i = 0; i < rowTotals.Length; i++)
        {
            if (rowTotals[i] == 0.0)
            {
                throw new ArgumentException($"Row {i} totals zero.", nameof(table));
            }
        }

        for (int j = 0; j < columnTotals.Length; j++)
        {
            if (columnTotals[j] == 0.0)
            {
                throw new ArgumentException($"Column {j} totals zero.", nameof(table));
            }
        }
    }
#pragma warning restore S1244, S1172

    private static double[][] ComputeExpected(
        double[] rowTotals, double[] columnTotals, double total, int rows, int columns)
    {
        double[][] expected = new double[rows][];
        for (int i = 0; i < rows; i++)
        {
            expected[i] = new double[columns];
            for (int j = 0; j < columns; j++)
            {
                expected[i][j] = rowTotals[i] * columnTotals[j] / total;
            }
        }

        return expected;
    }

    private static double ComputeStatistic(
        double[][] table, double[][] expected, int rows, int columns, bool yates)
    {
        double statistic = 0.0;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                double deviation = Math.Abs(table[i][j] - expected[i][j]);

                // Yates moves the observation half a unit toward the expectation,
                // never past it, or a table agreeing within half a count would overshoot.
                if (yates)
                {
                    deviation = Math.Max(0.0, deviation - 0.5);
                }

                statistic += deviation * deviation / expected[i][j];
            }
        }

        return statistic;
    }
}
