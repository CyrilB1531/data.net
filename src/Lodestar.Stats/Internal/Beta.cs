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
        double tail = 0.5 * RegularizedIncomplete(df / 2.0, 0.5, df / (df + (t * t)));
        return t >= 0.0 ? tail : 1.0 - tail;
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
