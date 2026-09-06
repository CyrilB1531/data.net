using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>The Shapiro-Wilk test for normality, by Royston's AS R94.</summary>
/// <remarks>
/// Written from Royston's 1995 published description and its polynomial constants (Applied
/// Statistics 44:547-551), not from any implementation of it (ADR 0003). The transform that
/// turns the statistic into a p-value is fitted for <c>3 &lt;= n &lt;= 5000</c>; outside that
/// range this refuses rather than extrapolating a number a reader would take at face value --
/// scipy warns and answers anyway.
/// </remarks>
public static class ShapiroWilk
{
    private const int MinimumSample = 3;
    private const int MaximumSample = 5000;

    // Royston's polynomial coefficients, ascending. The first two correct the
    // last two Blom weights; the rest carry the normalising transform.
    private static readonly double[] WeightCorrectionLast =
        [0.0, 0.221157, -0.147981, -2.071190, 4.434685, -2.706056];

    private static readonly double[] WeightCorrectionSecondLast =
        [0.0, 0.042981, -0.293762, -1.752461, 5.682633, -3.582633];

    private static readonly double[] SmallMu = [0.5440, -0.39978, 0.025054, -6.714e-4];
    private static readonly double[] SmallSigma = [1.3822, -0.77857, 0.062767, -0.0020322];
    private static readonly double[] LargeMu = [-1.5861, -0.31082, -0.083751, 0.0038915];
    private static readonly double[] LargeSigma = [-0.4803, -0.082676, 0.0030302];

    /// <summary>Tests whether a sample could have come from a normal distribution.</summary>
    /// <param name="sample">The sample; between 3 and 5000 values, not all equal.</param>
    /// <returns>Royston's W statistic and its p-value.</returns>
    /// <exception cref="ArgumentException">
    /// Fewer than 3 or more than 5000 values, or every value identical.
    /// </exception>
    public static TestResult Test(ReadOnlySpan<double> sample)
    {
        int n = sample.Length;
        if (n < MinimumSample || n > MaximumSample)
        {
            throw new ArgumentException(
                $"Royston's approximation covers {MinimumSample} to {MaximumSample} values; got {n}.",
                nameof(sample));
        }

        double[] sorted = sample.ToArray();
        Array.Sort(sorted);

        double[] weights = Weights(n);

        double mean = 0.0;
        for (int i = 0; i < n; i++)
        {
            mean += sorted[i];
        }
        mean /= n;

        double numerator = 0.0;
        double denominator = 0.0;
        for (int i = 0; i < n; i++)
        {
            numerator += weights[i] * sorted[i];
            double deviation = sorted[i] - mean;
            denominator += deviation * deviation;
        }

        if (denominator <= 0.0)
        {
            throw new ArgumentException(
                "Every value in the sample is identical, so there is no shape to test.",
                nameof(sample));
        }

        double w = numerator * numerator / denominator;

        return new TestResult(w, PValue(w, n));
    }

    // Royston's weights: Blom scores rescaled to unit length, the largest one (or two,
    // above n = 5) replaced by his polynomial corrections, the rest rescaled to match.
    private static double[] Weights(int n)
    {
        double[] blom = new double[n];
        double sumSquares = 0.0;
        for (int i = 0; i < n; i++)
        {
            // Blom's plotting position, through the *upper*-tail inverse, so the
            // sign is negated to put the smallest score first.
            double p = ((i + 1) - 0.375) / (n + 0.25);
            blom[i] = -Normal.Quantile(p);
            sumSquares += blom[i] * blom[i];
        }

        double norm = Math.Sqrt(sumSquares);
        double u = 1.0 / Math.Sqrt(n);

        double[] weights = new double[n];
        int corrected = n > 5 ? 2 : 1;

        double top = (blom[n - 1] / norm) + Polynomial(WeightCorrectionLast, u);
        weights[n - 1] = top;
        weights[0] = -top;

        double replacedRaw = blom[n - 1] * blom[n - 1];
        double replacedCorrected = top * top;

        if (corrected == 2)
        {
            double second = (blom[n - 2] / norm) + Polynomial(WeightCorrectionSecondLast, u);
            weights[n - 2] = second;
            weights[1] = -second;

            replacedRaw += blom[n - 2] * blom[n - 2];
            replacedCorrected += second * second;
        }

        // What the corrected weights left for the rest to share -- dividing by its square
        // root keeps the whole vector unit length after two entries were replaced.
        double remaining =
            (sumSquares - (2.0 * replacedRaw)) / (1.0 - (2.0 * replacedCorrected));
        double scale = Math.Sqrt(remaining);

        for (int i = corrected; i < n - corrected; i++)
        {
            weights[i] = blom[i] / scale;
        }

        return weights;
    }

    private static double PValue(double w, int n)
    {
        if (n == 3)
        {
            // Royston gives the n = 3 case in closed form: the null distribution
            // of W is exactly known there, so no transform is fitted.
            double p = 1.909859 * (Math.Asin(Math.Sqrt(w)) - 1.047198);
            return Math.Min(1.0, Math.Max(0.0, p));
        }

        double logN = Math.Log(n);
        double y = Math.Log(1.0 - w);

        double mu;
        double sigma;
        if (n <= 11)
        {
            double gamma = -2.273 + (0.459 * n);
            mu = Polynomial(SmallMu, n);
            sigma = Math.Exp(Polynomial(SmallSigma, n));

            // Below twelve, W is transformed through gamma first; above it, the
            // transform is in log n instead.
            y = -Math.Log(gamma - y);
        }
        else
        {
            mu = Polynomial(LargeMu, logN);
            sigma = Math.Exp(Polynomial(LargeSigma, logN));
        }

        return Normal.Sf((y - mu) / sigma);
    }

    private static double Polynomial(double[] coefficients, double x)
    {
        double result = 0.0;
        for (int i = coefficients.Length - 1; i >= 0; i--)
        {
            result = (result * x) + coefficients[i];
        }

        return result;
    }
}
