using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>Fisher's exact test on a 2x2 contingency table.</summary>
/// <remarks>
/// Exact rather than asymptotic: the p-value is a sum of hypergeometric
/// probabilities over the tables with the same margins, so it is right at any
/// sample size, where the chi-square approximation needs the cells to be large.
/// </remarks>
public static class FisherExact
{
    // Two tables differing only in the last bits are the same table here; a bare
    // <= would include or exclude one by rounding. scipy guards it the same way.
    private const double ProbabilityTolerance = 1e-7;

    // Computed once rather than at every comparison; not a compile-time constant
    // since Math.Log is not constant-evaluable in C#.
    private static readonly double LogProbabilityTolerance = Math.Log(1.0 + ProbabilityTolerance);

    // long-comment: the bound below is a measured performance ceiling, not an
    // arbitrary round number, and a reviewer should be able to see the
    // measurement without leaving the source.
    // The k-loop below is O(highest - lowest + 1), which is bounded by the
    // table's total: a table with unbounded margins would enumerate for
    // however long that takes rather than fail fast, and every term underflows
    // to a harmless exact 0.0 in the far tail rather than corrupting the sum
    // (verified in Python at total = 1,000,000, a 450,001-wide range: the sum
    // over the range still lands at 1.0000000008, task-7-report.md). 1,000,000
    // keeps the loop itself comfortably sub-second while leaving two clear
    // orders of magnitude below where the margin additions (rowOne = a + b,
    // columnOne = a + c) computed in `int` after the guard could themselves
    // overflow.
    private const long MaxTableTotal = 1_000_000;

    /// <summary>Tests a 2x2 table for association.</summary>
    /// <param name="table">The counts, as two rows of two.</param>
    /// <param name="alternative">Which tail the p-value covers.</param>
    /// <returns>
    /// The conditional odds ratio — <c>PositiveInfinity</c> when the second
    /// diagonal is zero, <c>NaN</c> when both diagonals are — and the p-value.
    /// </returns>
    /// <exception cref="ArgumentException">The table is not 2x2.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A count is negative, or the table's total exceeds 1,000,000 — the exact
    /// enumeration costs O(total), so a table past that bound is refused rather
    /// than run for however long that takes. <see cref="ChiSquare.Contingency"/>
    /// is the asymptotic alternative at that scale.
    /// </exception>
    // S2368: the table arrives from the caller already in this shape -- that is
    // how scipy.stats.fisher_exact takes it. Wrapping it buys no safety, only a
    // conversion at the boundary (same reasoning as ChiSquare.Contingency's
    // suppression).
#pragma warning disable S2368
    public static TestResult Test(int[][] table, Alternative alternative = Alternative.TwoSided)
#pragma warning restore S2368
    {
        Guard.NotNull(table);

        if (table.Length != 2 || table[0] is not { Length: 2 } || table[1] is not { Length: 2 })
        {
            throw new ArgumentException(
                "Fisher's exact test here is the 2x2 test; give it two rows of two.", nameof(table));
        }

        int a = table[0][0];
        int b = table[0][1];
        int c = table[1][0];
        int d = table[1][1];

        if (a < 0 || b < 0 || c < 0 || d < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(table), "Counts must be non-negative.");
        }

        double oddsRatio = OddsRatio(a, b, c, d);
        int total = ValidatedTotal(table, a, b, c, d);
        int rowOne = a + b;
        int columnOne = a + c;

        double pValue = EnumeratePValue(a, rowOne, columnOne, total, alternative);
        return new TestResult(oddsRatio, pValue);
    }

    // b == 0 || c == 0, not b * c == 0: both are unbounded non-negative ints,
    // and the product overflows well before either factor reaches a million.
    private static double OddsRatio(int a, int b, int c, int d)
    {
        if (b != 0 && c != 0)
        {
            return (double)a * d / ((double)b * c);
        }

        return a == 0 || d == 0 ? double.NaN : double.PositiveInfinity;
    }

    // S1172: table is read only through nameof below, so the thrown exception
    // names the public parameter the caller actually passed rather than one of
    // this private helper's own.
#pragma warning disable S1172
    private static int ValidatedTotal(int[][] table, int a, int b, int c, int d)
#pragma warning restore S1172
    {
        long totalLong = (long)a + b + c + d;
        if (totalLong > MaxTableTotal)
        {
            throw new ArgumentOutOfRangeException(
                nameof(table),
                totalLong,
                $"Fisher's exact enumerates every table sharing these margins, an O(total) " +
                $"cost; a table summing to {totalLong} exceeds the {MaxTableTotal} it is " +
                "refused past. Use ChiSquare.Contingency instead, which is asymptotic and " +
                "appropriate at that scale.");
        }

        return (int)totalLong;
    }

    private static double EnumeratePValue(
        int a, int rowOne, int columnOne, int total, Alternative alternative)
    {
        // With the margins fixed, the table is determined by its top-left cell,
        // which ranges over the values that leave every other cell non-negative.
        int lowest = Math.Max(0, columnOne - (total - rowOne));
        int highest = Math.Min(rowOne, columnOne);

        double observedLog = LogHypergeometricProbability(a, rowOne, columnOne, total);

        double pValue = 0.0;
        for (int k = lowest; k <= highest; k++)
        {
            double logProbability = LogHypergeometricProbability(k, rowOne, columnOne, total);

            // Compared in log space: probability <= observed * (1 + tol) would admit
            // only the tables that also underflowed once every probability here does.
            bool include = alternative switch
            {
                Alternative.Less => k <= a,
                Alternative.Greater => k >= a,
                Alternative.TwoSided => logProbability <= observedLog + LogProbabilityTolerance,
                _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
            };

            if (include)
            {
                pValue += Math.Exp(logProbability);
            }
        }

        return Math.Min(1.0, pValue);
    }

    // C(rowOne, k) C(total - rowOne, columnOne - k) / C(total, columnOne), through
    // log-gamma: the binomials overflow a double well before the counts do.
    private static double LogHypergeometricProbability(int k, int rowOne, int columnOne, int total) =>
        LogChoose(rowOne, k) +
        LogChoose(total - rowOne, columnOne - k) -
        LogChoose(total, columnOne);

    private static double LogChoose(int n, int k)
    {
        if (k < 0 || k > n)
        {
            return double.NegativeInfinity;
        }

        return Gamma.LogGamma(n + 1) - Gamma.LogGamma(k + 1) - Gamma.LogGamma(n - k + 1);
    }
}
