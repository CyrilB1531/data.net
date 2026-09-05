namespace Lodestar.Stats.Internal;

/// <summary>The complementary error function, and the standard normal's upper tail.</summary>
/// <remarks>
/// Built on the regularized incomplete gamma rather than on a rational
/// approximation of its own: erfc(x) = Q(1/2, x^2) for x >= 0 is an identity,
/// not a fit, so the accuracy this reaches in the far tail is the accuracy
/// <see cref="Gamma"/> already has to have for the chi-square tests.
/// </remarks>
internal static class Normal
{
    internal static double Erfc(double x)
    {
        if (double.IsNaN(x))
        {
            return double.NaN;
        }

        // erfc(-x) = 2 - erfc(x). Reflecting rather than evaluating at a negative
        // argument keeps the identity above valid, since Q takes x^2 either way.
        if (x < 0.0)
        {
            return 2.0 - Erfc(-x);
        }

        // Exact sentinel: erfc(0) = 1 is the definition, not an approximation
        // that could have accumulated rounding error to compare against.
#pragma warning disable S1244
        return x == 0.0 ? 1.0 : Gamma.RegularizedQ(0.5, x * x);
#pragma warning restore S1244
    }

    /// <summary>The standard normal's upper tail: P(Z &gt; z).</summary>
    internal static double Sf(double z) => 0.5 * Erfc(z / Math.Sqrt(2.0));

    /// <summary>The z with <c>P(Z &gt; z) = p</c>: the inverse of <see cref="Sf"/>.</summary>
    /// <remarks>
    /// By bisection, for the reason <c>Beta.StudentQuantile</c> gives: one
    /// approximation to keep right instead of two that must agree.
    /// </remarks>
    internal static double Quantile(double p)
    {
        if (double.IsNaN(p) || p <= 0.0 || p >= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(p), p, "The tail probability must lie strictly inside (0, 1).");
        }

        // S1244: an exact half is the distribution's own symmetric point, not a
        // value that drifted there by rounding -- Sf(0) is exactly 0.5.
#pragma warning disable S1244
        if (p == 0.5)
#pragma warning restore S1244
        {
            return 0.0;
        }

        // Symmetric about zero: p > 1/2 reduces to its mirror 1 - p < 1/2 -- the bracket
        // below only widens correctly for the small-tail side (p=0.975 unreduced wrongly satisfies at high=1.0).
        return p > 0.5 ? -BisectUpperTail(1.0 - p) : BisectUpperTail(p);
    }

    // p is already known to lie in (0, 0.5); the caller's symmetry reduction is
    // what keeps that promise for the other half of the domain.
    private static double BisectUpperTail(double p)
    {
        double high = 1.0;
        while (Sf(high) > p && high < 1e10)
        {
            high *= 2.0;
        }

        double low = -high;
        for (int i = 0; i < 200; i++)
        {
            double middle = 0.5 * (low + high);

            // S1244: bisection's own fixed-point test -- middle stops moving
            // once it lands on one of its bounds.
#pragma warning disable S1244
            if (middle == low || middle == high)
#pragma warning restore S1244
            {
                break;
            }

            if (Sf(middle) > p)
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
}
