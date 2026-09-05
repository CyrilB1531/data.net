namespace Lodestar.Stats.Internal;

/// <summary>The log-gamma function and the two regularized incomplete gammas.</summary>
/// <remarks>
/// Lanczos (1964) for the log-gamma; the series-below / continued-fraction-above
/// split, evaluated by modified Lentz (1976), for the incomplete pair. No
/// reference implementation is transcribed (ADR 0003). The upper tail
/// <c>Q</c> is a chi-square p-value: with <c>a = dof/2</c>, <c>x = statistic/2</c>,
/// <c>Q(a, x)</c> is the probability of a statistic at least this large.
/// </remarks>
internal static class Gamma
{
    // Lanczos g = 7, nine coefficients; the pair is not free -- mixing this
    // table with coefficients tuned for a different g loses eight digits silently.
    private const double LanczosG = 7.0;

    private static readonly double[] LanczosCoefficients =
    [
        0.99999999999980993,
        676.5203681218851,
        -1259.1392167224028,
        771.32342877765313,
        -176.61502916214059,
        12.507343278686905,
        -0.13857109526572012,
        9.9843695780195716e-6,
        1.5056327351493116e-7,
    ];

    private const int MaxIterations = 300;
    private const double Epsilon = 3e-16;

    // Smallest positive normal double: Lentz divides by the running denominator,
    // so a zero one is nudged here instead of producing an infinity that never recovers.
    private const double Tiny = 1e-300;

    internal static double LogGamma(double x)
    {
        if (double.IsNaN(x) || x <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(x), x, "The log-gamma function is defined here for x > 0 only.");
        }

        // Reflection: Gamma(x)Gamma(1-x) = pi / sin(pi x). Below 0.5 the Lanczos
        // sum loses precision, and above it the reflection would.
        if (x < 0.5)
        {
            return Math.Log(Math.PI / Math.Abs(Math.Sin(Math.PI * x))) - LogGamma(1.0 - x);
        }

        double z = x - 1.0;
        double series = LanczosCoefficients[0];
        for (int i = 1; i < LanczosCoefficients.Length; i++)
        {
            series += LanczosCoefficients[i] / (z + i);
        }

        double t = z + LanczosG + 0.5;
        return (0.5 * Math.Log(2.0 * Math.PI)) + ((z + 0.5) * Math.Log(t)) - t + Math.Log(series);
    }

    internal static double RegularizedP(double a, double x)
    {
        Validate(a, x);

        // Exact sentinel: x is validated to be non-negative above, and P(a, 0)
        // is 0 by definition, not a limit that rounding could have missed.
#pragma warning disable S1244
        if (x == 0.0)
#pragma warning restore S1244
        {
            return 0.0;
        }

        return x < a + 1.0 ? SeriesP(a, x) : 1.0 - ContinuedFractionQ(a, x);
    }

    internal static double RegularizedQ(double a, double x)
    {
        Validate(a, x);

        // Exact sentinel: same reasoning as RegularizedP, Q(a, 0) = 1 by definition.
#pragma warning disable S1244
        if (x == 0.0)
#pragma warning restore S1244
        {
            return 1.0;
        }

        return x < a + 1.0 ? 1.0 - SeriesP(a, x) : ContinuedFractionQ(a, x);
    }

    private static void Validate(double a, double x)
    {
        if (double.IsNaN(a) || a <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(a), a, "The shape must be positive.");
        }
        if (double.IsNaN(x) || x < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "The argument must not be negative.");
        }
    }

    // P(a, x) = x^a e^-x / Gamma(a) * sum_{n>=0} x^n / (a(a+1)...(a+n)).
    private static double SeriesP(double a, double x)
    {
        double term = 1.0 / a;
        double sum = term;
        for (int n = 1; n <= MaxIterations; n++)
        {
            term *= x / (a + n);
            sum += term;
            if (Math.Abs(term) < Math.Abs(sum) * Epsilon)
            {
                break;
            }
        }

        return sum * Math.Exp((a * Math.Log(x)) - x - LogGamma(a));
    }

    // Q(a, x) = x^a e^-x / Gamma(a) * 1/(x+1-a - 1(1-a)/(x+3-a - 2(2-a)/(x+5-a - ...))),
    // evaluated by modified Lentz.
    private static double ContinuedFractionQ(double a, double x)
    {
        double b = x + 1.0 - a;
        double c = 1.0 / Tiny;
        double d = 1.0 / b;
        double h = d;

        for (int i = 1; i <= MaxIterations; i++)
        {
            double an = -i * (i - a);
            b += 2.0;

            d = (an * d) + b;
            if (Math.Abs(d) < Tiny)
            {
                d = Tiny;
            }

            c = b + (an / c);
            if (Math.Abs(c) < Tiny)
            {
                c = Tiny;
            }

            d = 1.0 / d;
            double delta = d * c;
            h *= delta;

            if (Math.Abs(delta - 1.0) < Epsilon)
            {
                break;
            }
        }

        return Math.Exp((a * Math.Log(x)) - x - LogGamma(a)) * h;
    }
}
