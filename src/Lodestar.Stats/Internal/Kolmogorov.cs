namespace Lodestar.Stats.Internal;

/// <summary>The Kolmogorov distribution's upper tail.</summary>
/// <remarks>
/// Q(lambda) = 2 * sum_{k>=1} (-1)^{k-1} exp(-2 k^2 lambda^2). The terms fall
/// off as exp(-2 k^2 lambda^2), so the series converges in a handful of terms
/// for every lambda a two-sample KS test produces; the loop stops on the term's
/// own magnitude rather than on a fixed count.
/// </remarks>
internal static class Kolmogorov
{
    private const int MaxTerms = 200;
    private const double Epsilon = 1e-17;

    internal static double Sf(double lambda)
    {
        if (double.IsNaN(lambda))
        {
            return double.NaN;
        }
        if (lambda <= 0.0)
        {
            return 1.0;
        }

        double factor = -2.0 * lambda * lambda;
        double sum = 0.0;
        double sign = 1.0;

        for (int k = 1; k <= MaxTerms; k++)
        {
            double term = Math.Exp(factor * k * k);
            sum += sign * term;
            sign = -sign;

            if (term < Epsilon)
            {
                break;
            }
        }

        double q = 2.0 * sum;

        // Can overshoot at very small lambda, where the true value is already 1 to
        // every digit a double holds; cannot undershoot, since decreasing terms keep every partial sum non-negative.
        return q > 1.0 ? 1.0 : q;
    }
}
