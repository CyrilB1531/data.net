using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>One-way analysis of variance: do several groups share one mean?</summary>
/// <remarks>
/// The k-sample generalisation of <see cref="TTest.Independent"/> with
/// <see cref="Variance.Equal"/>: on two groups the F statistic is the square of
/// Student's t, and the two p-values agree.
/// </remarks>
public static class OneWayAnova
{
    /// <summary>Compares the means of two or more groups.</summary>
    /// <param name="groups">The groups; at least two, each holding at least one value.</param>
    /// <returns>The F statistic and the upper-tail p-value.</returns>
    /// <exception cref="ArgumentException">
    /// Fewer than two groups, an empty group, or no group holding more than one value.
    /// </exception>
    // S2368: groups arrives from the caller already in this shape -- that is
    // how scipy.stats.f_oneway takes its samples, one array per group.
    // Wrapping it buys no safety, only a conversion at the boundary.
#pragma warning disable S2368
    public static TestResult Test(params double[][] groups)
#pragma warning restore S2368
    {
        Guard.NotNull(groups);

        if (groups.Length < 2)
        {
            throw new ArgumentException(
                $"An analysis of variance needs at least two groups; got {groups.Length}.",
                nameof(groups));
        }

        (int total, double grandSum) = ValidatedTotals(groups);

        if (total <= groups.Length)
        {
            throw new ArgumentException(
                "The within-group degrees of freedom are zero: every group holds one value.",
                nameof(groups));
        }

        double grandMean = grandSum / total;
        (double between, double within) = SumsOfSquares(groups, grandMean);

        double dfBetween = groups.Length - 1;
        double dfWithin = total - groups.Length;

        // within = 0 while between > 0 (every group internally constant, but
        // not at the same constant) makes this +Infinity rather than a NaN --
        // FisherSf(+Infinity, ...) is already exact and returns 0.0, the
        // mathematically honest answer for a perfect, noiseless separation.
        // Deleting neither branch changes that: it falls out of ordinary IEEE
        // division, not a guard written to catch it.
        double statistic = (between / dfBetween) / (within / dfWithin);

        return new TestResult(statistic, Beta.FisherSf(statistic, dfBetween, dfWithin));
    }

    private static (int Total, double GrandSum) ValidatedTotals(double[][] groups)
    {
        int total = 0;
        double grandSum = 0.0;
        for (int g = 0; g < groups.Length; g++)
        {
            if (groups[g] is not { Length: > 0 })
            {
                throw new ArgumentException($"Group {g} is empty.", nameof(groups));
            }

            for (int i = 0; i < groups[g].Length; i++)
            {
                grandSum += groups[g][i];
                total++;
            }
        }

        return (total, grandSum);
    }

    private static (double Between, double Within) SumsOfSquares(double[][] groups, double grandMean)
    {
        double between = 0.0;
        double within = 0.0;
        for (int g = 0; g < groups.Length; g++)
        {
            double sum = 0.0;
            for (int i = 0; i < groups[g].Length; i++)
            {
                sum += groups[g][i];
            }

            double mean = sum / groups[g].Length;
            double deviation = mean - grandMean;
            between += groups[g].Length * deviation * deviation;

            for (int i = 0; i < groups[g].Length; i++)
            {
                double residual = groups[g][i] - mean;
                within += residual * residual;
            }
        }

        return (between, within);
    }
}
