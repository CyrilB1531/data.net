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
    // table a few megabytes and the walk under a second. Auto's own
    // threshold does NOT bound n*m by itself: the exact branch only needs
    // the *smaller* sample at or under eight, so n=8, m=10000 still
    // qualifies and would build a 6.4 GB table if let through unguarded --
    // Auto is bounded below as well, falling back to asymptotic rather than
    // refusing, since a caller who never asked for Exact must not be
    // handed an exception over it.
    private const long MaxExactProduct = 20_000;

    /// <summary>Compares two independent samples by their ranks.</summary>
    /// <param name="x">The first sample; at least one value.</param>
    /// <param name="y">The second sample; at least one value.</param>
    /// <param name="alternative">Which tail the p-value covers.</param>
    /// <param name="continuity">
    /// Whether the normal approximation gets the half-unit correction. Ignored
    /// on the exact branch, where there is nothing to approximate.
    /// </param>
    /// <param name="method">
    /// Exact, asymptotic, or chosen by sample size and ties. Past the same
    /// size bound <see cref="ExactMethod.Exact"/> is refused for,
    /// <see cref="ExactMethod.Auto"/> falls back to asymptotic rather than
    /// building the table -- it never throws.
    /// </param>
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
        bool wantsExact = method switch
        {
            ExactMethod.Exact => true,
            ExactMethod.Asymptotic => false,
            _ => !ties && (n <= AutoExactThreshold || m <= AutoExactThreshold),
        };

        bool tableTooLarge = (long)n * m > MaxExactProduct;
        if (method == ExactMethod.Exact && tableTooLarge)
        {
            throw new ArgumentOutOfRangeException(
                nameof(method),
                method,
                $"Exact Mann-Whitney needs x.Length * y.Length <= {MaxExactProduct}; " +
                $"got {n} * {m} = {(long)n * m}. Pass ExactMethod.Asymptotic instead.");
        }

        // Auto never throws: past the bound it falls back to asymptotic
        // instead, since nothing the caller wrote asked for an exact answer.
        bool exact = wantsExact && !tableTooLarge;

        double pValue = exact
            ? ExactPValue(u, n, m, alternative)
            : AsymptoticPValue(u, n, m, pooled, alternative, continuity);

        return new TestResult(u, pValue);
    }

    // long-comment: this reads as an unmotivated detour from "compute atMost
    // and atLeast" without the corpus history behind why it changed shape.
    // Measured against scipy 1.18.0: the exact branch never calls a CDF, only
    // a survival function, truncating the chosen statistic toward zero first.
    // Less reads U2's survival rather than U1's CDF; the null distribution's
    // symmetry makes the two equivalent when untied, but under ties the
    // truncation and the choice of which U feeds the lookup both matter,
    // and no corpus case exercised that (task-6-report.md, fix round 1,
    // Finding 2).
    private static double ExactPValue(double u, int n, int m, Alternative alternative)
    {
        double[] counts = RankDistributions.MannWhitneyCounts(n, m);
        double total = 0.0;
        for (int i = 0; i < counts.Length; i++)
        {
            total += counts[i];
        }

        double u1 = u;
        double u2 = (n * m) - u;

        (double chosen, double factor) = alternative switch
        {
            Alternative.Greater => (u1, 1.0),
            Alternative.Less => (u2, 1.0),
            Alternative.TwoSided => (Math.Max(u1, u2), 2.0),
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };

        double survival = SurvivalAtLeast(counts, chosen);
        return Math.Min(1.0, factor * survival / total);
    }

    // (int) truncates toward zero, matching numpy's astype(int64) on the
    // always-nonnegative U scipy feeds it here.
    private static double SurvivalAtLeast(double[] counts, double k)
    {
        int floor = Math.Max(0, (int)k);
        double sum = 0.0;
        for (int i = floor; i < counts.Length; i++)
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
