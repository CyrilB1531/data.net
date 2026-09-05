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
}
