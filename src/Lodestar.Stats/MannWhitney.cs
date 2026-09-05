using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>The Mann-Whitney U test: do two independent samples come from one distribution?</summary>
/// <remarks>
/// The rank-based counterpart to <see cref="TTest.Independent"/>: it assumes
/// nothing about the shape of either distribution, only that a value from one
/// can be compared with a value from the other.
/// </remarks>
public static class MannWhitney
{
    // Measured against scipy 1.18.0: Auto is exact whenever the smaller
    // sample holds eight or fewer values, not when both do.
    private const int AutoExactThreshold = 8;

    // long-comment: the bound below is a measured performance ceiling, not an
    // arbitrary round number, and a reviewer should be able to see the
    // measurement without leaving the source.
    // RankDistributions.MannWhitneyCounts(n, m) rebuilds an (m+1) x (n*m+1)
    // table on each of n outer iterations -- O(n^2 * m^2). At n=m=200 that
    // walks 1.6 billion entries and allocates 64 MB per outer iteration, 200
    // times over: tens of seconds under heavy GC pressure. 20,000 keeps the
    // table a few megabytes and the walk under a second, while sitting far
    // above anything Auto itself can reach (Auto's own threshold caps n*m
    // at 63, well inside the bound).
    private const long MaxExactProduct = 20_000;

    /// <summary>Compares two independent samples by their ranks.</summary>
    /// <param name="x">The first sample; at least one value.</param>
    /// <param name="y">The second sample; at least one value.</param>
    /// <param name="alternative">Which tail the p-value covers.</param>
    /// <param name="continuity">
    /// Whether the normal approximation gets the half-unit correction. Ignored
    /// on the exact branch, where there is nothing to approximate.
    /// </param>
    /// <param name="method">Exact, asymptotic, or chosen by sample size and ties.</param>
    /// <returns>U for the first sample, and the p-value.</returns>
    /// <exception cref="ArgumentException">Either sample is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="method"/> is <see cref="ExactMethod.Exact"/> and
    /// <c>x.Length * y.Length</c> exceeds 20,000 — the exact table's build cost
    /// grows with the square of that product, so a request that large is
    /// refused rather than run for tens of seconds. Pass
    /// <see cref="ExactMethod.Asymptotic"/> instead.
    /// </exception>
    public static TestResult Test(
        ReadOnlySpan<double> x,
        ReadOnlySpan<double> y,
        Alternative alternative = Alternative.TwoSided,
        Continuity continuity = Continuity.Applied,
        ExactMethod method = ExactMethod.Auto)
    {
        if (x.Length == 0)
        {
            throw new ArgumentException("The first sample is empty.", nameof(x));
        }
        if (y.Length == 0)
        {
            throw new ArgumentException("The second sample is empty.", nameof(y));
        }

        int n = x.Length;
        int m = y.Length;

        double[] pooled = new double[n + m];
        x.CopyTo(pooled);
        y.CopyTo(pooled.AsSpan(n));

        double[] ranks = Ranks.Average(pooled);
        double rankSumX = 0.0;
        for (int i = 0; i < n; i++)
        {
            rankSumX += ranks[i];
        }

        // U counts the pairs (xi, yj) with xi > yj, recovered from the rank sum
        // by subtracting the ranks x would hold if it sorted first.
        double u = rankSumX - (n * (n + 1) / 2.0);

        bool ties = Ranks.HasTies(pooled);
        bool exact = method switch
        {
            ExactMethod.Exact => true,
            ExactMethod.Asymptotic => false,
            _ => !ties && (n <= AutoExactThreshold || m <= AutoExactThreshold),
        };

        if (method == ExactMethod.Exact && (long)n * m > MaxExactProduct)
        {
            throw new ArgumentOutOfRangeException(
                nameof(method),
                method,
                $"Exact Mann-Whitney needs x.Length * y.Length <= {MaxExactProduct}; " +
                $"got {n} * {m} = {(long)n * m}. Pass ExactMethod.Asymptotic instead.");
        }

        double pValue = exact
            ? ExactPValue(u, n, m, alternative)
            : AsymptoticPValue(u, n, m, pooled, alternative, continuity);

        return new TestResult(u, pValue);
    }

    private static double ExactPValue(double u, int n, int m, Alternative alternative)
    {
        double[] counts = RankDistributions.MannWhitneyCounts(n, m);
        double total = 0.0;
        for (int i = 0; i < counts.Length; i++)
        {
            total += counts[i];
        }

        double atMost = CumulativeAtMost(counts, u);
        double atLeast = total - CumulativeAtMost(counts, u - 1);

        return alternative switch
        {
            Alternative.Less => atMost / total,
            Alternative.Greater => atLeast / total,
            // Twice the smaller tail, clamped: a discrete distribution's two
            // one-sided p-values can exceed one when doubled at the centre.
            Alternative.TwoSided => Math.Min(1.0, 2.0 * Math.Min(atMost, atLeast) / total),
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };
    }

    private static double CumulativeAtMost(double[] counts, double u)
    {
        double sum = 0.0;
        for (int i = 0; i < counts.Length && i <= u; i++)
        {
            sum += counts[i];
        }

        return sum;
    }

    private static double AsymptoticPValue(
        double u,
        int n,
        int m,
        ReadOnlySpan<double> pooled,
        Alternative alternative,
        Continuity continuity)
    {
        double total = n + m;
        double mean = n * m / 2.0;

        // The tie correction shrinks the variance: tied values carry less
        // information about the ordering than distinct ones do.
        double tieTerm = Ranks.TieCorrection(pooled) / (total * (total - 1.0));
        double variance = n * m / 12.0 * (total + 1.0 - tieTerm);
        double deviation = u - mean;

        double correction = continuity == Continuity.Applied ? 0.5 : 0.0;
        double z = alternative switch
        {
            Alternative.Less => (deviation + correction) / Math.Sqrt(variance),
            Alternative.Greater => (deviation - correction) / Math.Sqrt(variance),
            Alternative.TwoSided => (Math.Abs(deviation) - correction) / Math.Sqrt(variance),
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };

        return alternative switch
        {
            // Sf(-z), not 1 - Sf(z): the far tail is exactly where 1 - (a
            // value near 1) cancels the bits a corpus case at 1e-14 needs.
            Alternative.Less => Normal.Sf(-z),
            Alternative.Greater => Normal.Sf(z),
            Alternative.TwoSided => Math.Min(1.0, 2.0 * Normal.Sf(z)),
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };
    }
}
