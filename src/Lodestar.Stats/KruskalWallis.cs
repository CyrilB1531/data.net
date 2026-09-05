using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>The Kruskal-Wallis H test: the rank-based k-sample comparison.</summary>
/// <remarks>
/// What <see cref="OneWayAnova"/> is to <see cref="TTest.Independent"/>, this is
/// to <see cref="MannWhitney"/>: on two groups it agrees with Mann-Whitney's
/// asymptotic two-sided p-value.
/// </remarks>
public static class KruskalWallis
{
    /// <summary>Compares two or more groups by their ranks in the pooled sample.</summary>
    /// <param name="groups">The groups; at least two, each holding at least one value.</param>
    /// <returns>The H statistic and the upper-tail p-value.</returns>
    /// <exception cref="ArgumentException">
    /// Fewer than two groups, an empty group, or a pooled sample in which every value is tied.
    /// </exception>
    // S2368: same reasoning as OneWayAnova.Test -- groups mirrors
    // scipy.stats.kruskal's own one-array-per-group shape.
#pragma warning disable S2368
    public static TestResult Test(params double[][] groups)
#pragma warning restore S2368
    {
        Guard.NotNull(groups);

        if (groups.Length < 2)
        {
            throw new ArgumentException(
                $"Kruskal-Wallis needs at least two groups; got {groups.Length}.", nameof(groups));
        }

        int total = ValidatedTotal(groups);
        double[] pooled = Pool(groups, total);
        double[] ranks = Ranks.Average(pooled);

        double weighted = WeightedRankSum(groups, ranks);
        double h = (12.0 / (total * (total + 1.0)) * weighted) - (3.0 * (total + 1.0));

        // Every value tied leaves nothing: with t = n (one tie group spanning the whole
        // sample), 1 - (t^3-t)/(n^3-n) is exactly 0, not merely close to it.
        double tieCorrection = 1.0 -
            (Ranks.TieCorrection(pooled) / (((double)total * total * total) - total));

        if (tieCorrection <= 0.0)
        {
            throw new ArgumentException(
                "Every value in the pooled sample is tied, so the ranks carry no information.",
                nameof(groups));
        }

        h /= tieCorrection;
        double dof = groups.Length - 1;

        return new TestResult(h, Gamma.RegularizedQ(dof / 2.0, h / 2.0));
    }

    private static int ValidatedTotal(double[][] groups)
    {
        int total = 0;
        for (int g = 0; g < groups.Length; g++)
        {
            if (groups[g] is not { Length: > 0 })
            {
                throw new ArgumentException($"Group {g} is empty.", nameof(groups));
            }

            total += groups[g].Length;
        }

        return total;
    }

    private static double[] Pool(double[][] groups, int total)
    {
        double[] pooled = new double[total];
        int offset = 0;
        for (int g = 0; g < groups.Length; g++)
        {
            groups[g].CopyTo(pooled, offset);
            offset += groups[g].Length;
        }

        return pooled;
    }

    private static double WeightedRankSum(double[][] groups, double[] ranks)
    {
        double weighted = 0.0;
        int offset = 0;
        for (int g = 0; g < groups.Length; g++)
        {
            double sum = 0.0;
            for (int i = 0; i < groups[g].Length; i++)
            {
                sum += ranks[offset + i];
            }

            weighted += sum * sum / groups[g].Length;
            offset += groups[g].Length;
        }

        return weighted;
    }
}
