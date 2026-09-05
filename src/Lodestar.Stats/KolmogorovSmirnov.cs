using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>The two-sample Kolmogorov-Smirnov test.</summary>
/// <remarks>
/// <b>Two-sample only.</b> The one-sample test compares a sample against a named
/// distribution, which means passing a cumulative distribution function; this
/// package has no distributions namespace to pass one from, and inventing one to
/// serve a single test is a second package's worth of surface.
/// </remarks>
public static class KolmogorovSmirnov
{
    // scipy takes the exact branch while the lattice stays small; above this the
    // table costs more than the asymptotic answer is worth.
    private const long AutoExactLimit = 10_000;

    /// <summary>Compares two samples by the largest gap between their empirical distributions.</summary>
    /// <param name="a">The first sample; at least one value.</param>
    /// <param name="b">The second sample; at least one value.</param>
    /// <param name="alternative">
    /// <see cref="Alternative.TwoSided"/> takes the largest gap in either
    /// direction; the one-sided values take the largest gap in one.
    /// </param>
    /// <param name="method">Exact, asymptotic, or chosen by the sample sizes.</param>
    /// <returns>The distance, the p-value, where the distance was reached and its sign.</returns>
    /// <exception cref="ArgumentException">Either sample is empty.</exception>
    public static KsResult TwoSample(
        ReadOnlySpan<double> a,
        ReadOnlySpan<double> b,
        Alternative alternative = Alternative.TwoSided,
        ExactMethod method = ExactMethod.Auto)
    {
        if (a.Length == 0)
        {
            throw new ArgumentException("The first sample is empty.", nameof(a));
        }
        if (b.Length == 0)
        {
            throw new ArgumentException("The second sample is empty.", nameof(b));
        }

        int n = a.Length;
        int m = b.Length;

        double[] sortedA = a.ToArray();
        double[] sortedB = b.ToArray();
        Array.Sort(sortedA);
        Array.Sort(sortedB);

        (double statistic, double location, int sign) = Statistic(sortedA, sortedB, alternative);

        bool exact = method switch
        {
            ExactMethod.Exact => true,
            ExactMethod.Asymptotic => false,
            _ => (long)n * m <= AutoExactLimit,
        };

        double pValue = exact
            ? ExactPValue(statistic, n, m, alternative)
            : AsymptoticPValue(statistic, n, m, alternative);

        return new KsResult(statistic, Math.Min(1.0, Math.Max(0.0, pValue)), location, sign);
    }

    // Walks every distinct value across BOTH samples, not just until one of
    // them is exhausted: the one-sided supremum can be attained past that
    // point, where the shorter sample's empirical CDF has already reached 1
    // and the other's is still climbing toward it. Stopping early is how a
    // sample whose distribution never falls below the other's (D- = 0) still
    // reported a nonzero location and the wrong sign -- the true zero is only
    // reached at the last pooled value, which an early exit never visits
    // (task-8-report.md, unbalanced and well-separated corpus cases).
    //
    // Both one-sided statistics are tracked unconditionally, not only the one
    // the caller asked for: two-sided needs whichever of the two is larger,
    // and scipy's own tie-break (first occurrence wins, via argmin/argmax)
    // only falls out correctly if both running maxima are kept across the
    // whole walk.
    private static (double Statistic, double Location, int Sign) Statistic(
        double[] sortedA, double[] sortedB, Alternative alternative)
    {
        (double above, double aboveLocation, double below, double belowLocation) = Walk(sortedA, sortedB);

        // scipy clips only D- this way; D+ needs no clip, since the final
        // pooled value always contributes a difference of exactly 0, which
        // keeps its running maximum non-negative on its own.
        below = Math.Min(1.0, Math.Max(0.0, below));

        return Select(alternative, above, aboveLocation, below, belowLocation);
    }

    private static (double Above, double AboveLocation, double Below, double BelowLocation) Walk(
        double[] sortedA, double[] sortedB)
    {
        int n = sortedA.Length;
        int m = sortedB.Length;

        double above = double.NegativeInfinity;
        double aboveLocation = double.NaN;
        double below = double.NegativeInfinity;
        double belowLocation = double.NaN;

        int i = 0;
        int j = 0;
        while (i < n || j < m)
        {
            double value = NextValue(sortedA, sortedB, i, j);
            i = AdvancePast(sortedA, i, value);
            j = AdvancePast(sortedB, j, value);

            double difference = ((double)i / n) - ((double)j / m);
            if (difference > above)
            {
                above = difference;
                aboveLocation = value;
            }
            if (-difference > below)
            {
                below = -difference;
                belowLocation = value;
            }
        }

        return (above, aboveLocation, below, belowLocation);
    }

    // S1244: a tie group is defined by literally equal input values, not
    // nearby ones -- the empirical CDF only steps at an exact observation.
    private static int AdvancePast(double[] sorted, int index, double value)
    {
#pragma warning disable S1244
        while (index < sorted.Length && sorted[index] == value)
#pragma warning restore S1244
        {
            index++;
        }

        return index;
    }

    private static double NextValue(double[] sortedA, double[] sortedB, int i, int j)
    {
        if (i < sortedA.Length && j < sortedB.Length)
        {
            return Math.Min(sortedA[i], sortedB[j]);
        }

        return i < sortedA.Length ? sortedA[i] : sortedB[j];
    }

    private static (double, double, int) Select(
        Alternative alternative, double above, double aboveLocation, double below, double belowLocation) =>
        alternative switch
        {
            Alternative.Less => (below, belowLocation, -1),
            Alternative.Greater => (above, aboveLocation, 1),
            Alternative.TwoSided => below > above
                ? (below, belowLocation, -1)
                : (above, aboveLocation, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };

    // The exact tail counts the lattice paths from (0,0) to (n,m) that step
    // outside the d-boundary at some point, normalising by C(i+j,i) as it
    // goes so the running value stays in [0,1] where the raw counts reach
    // C(n+m,n) and overflow a double at a few hundred values each.
    //
    // Tracks the escaped mass directly rather than the complementary mass
    // that stays inside: the two solve the identical recurrence (normalising
    // commutes with the complement), but a well separated pair drives the
    // inside mass to within 1e-17 of exactly 1.0, and 1.0 minus that collapsed
    // to an exact 0.0 where the true p-value was still representable --
    // reverting to the inside/1-inside form and rerunning the well-separated
    // corpus case reproduces that exact collapse (task-8-report.md).
    private static double ExactPValue(double d, int n, int m, Alternative alternative)
    {
        double bound = d - (0.5 / ((double)n * m));

        // One row at a time: escaped[i][*] depends only on escaped[i-1][*] and
        // its own already-filled entries, so the full (n+1)x(m+1) table is
        // never needed at once.
        double[] row = new double[m + 1];
        for (int i = 0; i <= n; i++)
        {
            row = FillRow(i, n, m, bound, alternative, row);
        }

        return row[m];
    }

    private static double[] FillRow(
        int i, int n, int m, double bound, Alternative alternative, double[] previousRow)
    {
        double[] row = new double[m + 1];
        for (int j = 0; j <= m; j++)
        {
            if (i == 0 && j == 0)
            {
                continue;
            }

            if (IsOutside(i, j, n, m, bound, alternative))
            {
                row[j] = 1.0;
                continue;
            }

            double fromLeft = i > 0 ? previousRow[j] : 0.0;
            double fromBelow = j > 0 ? row[j - 1] : 0.0;
            row[j] = ((fromLeft * i) + (fromBelow * j)) / (i + j);
        }

        return row;
    }

    private static bool IsOutside(int i, int j, int n, int m, double bound, Alternative alternative)
    {
        double difference = ((double)i / n) - ((double)j / m);
        return alternative switch
        {
            Alternative.Less => -difference >= bound,
            Alternative.Greater => difference >= bound,
            Alternative.TwoSided => Math.Abs(difference) >= bound,
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };
    }

    private static double AsymptoticPValue(double d, int n, int m, Alternative alternative)
    {
        double effective = (double)n * m / (n + m);

        // Two-sided reads the finite-sample Kolmogorov tail directly; scipy
        // measurably disagrees with the n -> infinity limit at the sample
        // sizes a two-sample test actually produces (Kolmogorov.FiniteTwoSidedSf's
        // own remarks have the numbers). One-sided has no finite-sample
        // closed form as simple as the limit's exp(-2 en d^2) -- Hodges'
        // (1958) eqn. 5.3 correction below is what scipy's own asymp branch
        // evaluates instead.
        return alternative == Alternative.TwoSided
            ? Kolmogorov.FiniteTwoSidedSf(effective, d)
            : OneSidedAsymptoticPValue(d, n, m);
    }

    private static double OneSidedAsymptoticPValue(double d, int n, int m)
    {
        double larger = Math.Max(n, m);
        double smaller = Math.Min(n, m);
        double effective = larger * smaller / (larger + smaller);
        double z = Math.Sqrt(effective) * d;

        double correction = 2.0 * z * (larger + (2.0 * smaller)) /
            (Math.Sqrt(larger * smaller * (larger + smaller)) * 3.0);

        return Math.Exp((-2.0 * z * z) - correction);
    }
}
