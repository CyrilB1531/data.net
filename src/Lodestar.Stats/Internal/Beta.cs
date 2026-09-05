namespace Lodestar.Stats.Internal;

/// <summary>The regularized incomplete beta, and the Student and Fisher tails it carries.</summary>
/// <remarks>
/// One continued fraction serves three families: a t-test's p-value is a
/// Student tail, an ANOVA's is a Fisher tail, and both are the incomplete beta
/// under a change of variable. Written from the published description and
/// evaluated by modified Lentz (1976); no reference implementation is
/// transcribed (ADR 0003).
/// </remarks>
internal static class Beta
{
    private const int MaxIterations = 300;
    private const double Epsilon = 3e-16;
    private const double Tiny = 1e-300;

    internal static double RegularizedIncomplete(double a, double b, double x)
    {
        if (double.IsNaN(a) || a <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(a), a, "The first shape must be positive.");
        }
        if (double.IsNaN(b) || b <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(b), b, "The second shape must be positive.");
        }
        if (double.IsNaN(x) || x < 0.0 || x > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "The argument must lie in [0, 1].");
        }

        // Exact sentinels, not a rounding-error-prone comparison: x is validated
        // to [0, 1] above, and 0/1 are the interval's own closed endpoints.
#pragma warning disable S1244
        if (x == 0.0 || x == 1.0)
#pragma warning restore S1244
        {
            return x;
        }

        double front = Math.Exp(
            Gamma.LogGamma(a + b) - Gamma.LogGamma(a) - Gamma.LogGamma(b) +
            (a * Math.Log(x)) + (b * Math.Log(1.0 - x)));

        // The fraction converges quickly only on the side of the distribution's
        // mode; past it the reflection is the fast branch, not a fallback. The
        // swapped (b, a) below is the reflection identity I_x(a,b) = 1 - I_{1-x}(b,a),
        // not a copy-paste slip.
#pragma warning disable S2234
        return x < (a + 1.0) / (a + b + 2.0)
            ? front / (a * ContinuedFraction(a, b, x))
            : 1.0 - (front / (b * ContinuedFraction(b, a, 1.0 - x)));
#pragma warning restore S2234
    }

    /// <summary>The upper tail of Student's t distribution: P(T &gt; t).</summary>
    internal static double StudentSf(double t, double df)
    {
        if (double.IsNaN(t))
        {
            return double.NaN;
        }

        // I_{df/(df+t^2)}(df/2, 1/2) is twice the tail beyond |t|, so half of it
        // is the tail on one side and the sign says which side we are on.
        double tSquared = t * t;
        double denominator = df + tSquared;
        double x = df / denominator;

        // long-comment: this is the fix for a measured far-tail collapse, not
        //     routine commentary -- reflecting unconditionally returned exactly
        //     0.0 at ordinary df once t was large enough (df = 200, t = 10 is
        //     BetaTests's non-regression case). A residual remains above
        //     roughly df = 1.5e9 -- about 1.5 billion observations through
        //     TTest's n1 + n2 - 2 -- where RegularizedIncomplete itself
        //     saturates to exactly 1.0, which no branch choice here repairs.
        // Direct evaluation of I_x(df/2, 1/2) is what the far-tail case needs:
        // reflecting through I_x(a,b) = 1 - I_{1-x}(b,a) trades one cancellation
        // for another one downstream -- 1.0 minus a value that is itself within
        // a few ULPs of 1 collapses to exactly 0.0 once the true tail is below
        // roughly 1e-16, which is reachable at ordinary df once t is large
        // enough (df = 200, t = 10 already gets there). Reflection earns its
        // keep only in the opposite regime, where x itself has already lost the
        // precision direct evaluation needs: IsDirectlyAccurate below detects
        // that by comparing the naive 1.0 - x against the complement computed
        // by division, rather than by guessing a df or magnitude cutoff.
        double complement = tSquared / denominator;
        double tail = 0.5 * (IsDirectlyAccurate(x, complement)
            ? RegularizedIncomplete(df / 2.0, 0.5, x)
            : 1.0 - RegularizedIncomplete(0.5, df / 2.0, complement));
        return t >= 0.0 ? tail : 1.0 - tail;
    }

    // x <= 0.5 (t^2 >= df) is always safe. Past that, direct evaluation is
    // accurate exactly when 1 - x still recovers the true (divided) complement.
    private static bool IsDirectlyAccurate(double x, double complement)
    {
        if (x <= 0.5)
        {
            return true;
        }

        double naiveComplement = 1.0 - x;
        return Math.Abs(naiveComplement - complement) <= complement * 1e-9;
    }

    /// <summary>The upper tail of the F distribution: P(F &gt; f).</summary>
    internal static double FisherSf(double f, double dfn, double dfd)
    {
        if (double.IsNaN(f))
        {
            return double.NaN;
        }
        if (f <= 0.0)
        {
            return 1.0;
        }

        return RegularizedIncomplete(dfd / 2.0, dfn / 2.0, dfd / (dfd + (dfn * f)));
    }

    /// <summary>The t with <c>P(T &gt; t) = p</c>: the inverse of <see cref="StudentSf"/>.</summary>
    /// <remarks>
    /// By bisection on a strictly decreasing function rather than by a rational
    /// approximation of its own. Fifty-odd halvings reach the last bit of a
    /// double, the bracket is found by doubling rather than assumed, and there
    /// is no second approximation to keep in agreement with the tail.
    /// </remarks>
    internal static double StudentQuantile(double p, double df)
    {
        if (double.IsNaN(p) || p <= 0.0 || p >= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(p), p, "The tail probability must lie strictly inside (0, 1).");
        }
        if (double.IsNaN(df) || df <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(df), df, "The degrees of freedom must be positive.");
        }

        // S1244: an exact half is the distribution's own symmetric point, not a
        // value that drifted there by rounding -- StudentSf(0, df) is exactly
        // 0.5 for every df, so the shortcut is correct only at that literal.
#pragma warning disable S1244
        if (p == 0.5)
#pragma warning restore S1244
        {
            return 0.0;
        }

        // Symmetric about zero, so p > 1/2 reduces to its mirror 1 - p < 1/2:
        // BisectUpperTail's bracket only ever grows on the positive side.
        return p > 0.5
            ? -BisectUpperTail(1.0 - p, df)
            : BisectUpperTail(p, df);
    }

    // p is already known to lie in (0, 0.5); the caller's symmetry reduction is
    // what keeps that promise for the other half of the domain.
    private static double BisectUpperTail(double p, double df)
    {
        // Widen until bracketed, by doubling: a Cauchy tail (df = 1) at
        // p = 1e-300 needs a bound near 1e300, so a fixed one would fail there.
        double high = 1.0;
        while (StudentSf(high, df) > p && high < 1e300)
        {
            high *= 2.0;
        }

        double low = -high;
        for (int i = 0; i < 200; i++)
        {
            double middle = 0.5 * (low + high);

            // S1244: this is the bisection's own fixed-point test, not a
            // tolerance check -- middle stops moving once it lands on one of
            // its bounds, and a range comparison would spin the remaining
            // iterations for no further precision.
#pragma warning disable S1244
            if (middle == low || middle == high)
#pragma warning restore S1244
            {
                break;
            }

            if (StudentSf(middle, df) > p)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return 0.5 * (low + high);
    }

    // CF = 1 + d1/(1 + d2/(1 + ...)) from Abramowitz & Stegun 26.5.8, evaluated
    // directly by modified Lentz (b0 = 1, every later b_i = 1) rather than a reciprocal restatement.
    private static double ContinuedFraction(double a, double b, double x)
    {
        double c = 1.0;
        double d = 0.0;
        double h = 1.0;

        for (int i = 1; i <= MaxIterations; i++)
        {
            int m = i / 2;

            // The numerators alternate between the two forms with the term's
            // parity; folding both into one loop keeps the recurrence's state in one place.
            double numerator = i % 2 == 0
                ? m * (b - m) * x / ((a + (2 * m) - 1.0) * (a + (2 * m)))
                : -(a + m) * (a + b + m) * x / ((a + (2 * m)) * (a + (2 * m) + 1.0));

            d = 1.0 + (numerator * d);
            if (Math.Abs(d) < Tiny)
            {
                d = Tiny;
            }
            d = 1.0 / d;

            c = 1.0 + (numerator / c);
            if (Math.Abs(c) < Tiny)
            {
                c = Tiny;
            }

            double delta = c * d;
            h *= delta;

            if (Math.Abs(delta - 1.0) < Epsilon)
            {
                break;
            }
        }

        return h;
    }
}
