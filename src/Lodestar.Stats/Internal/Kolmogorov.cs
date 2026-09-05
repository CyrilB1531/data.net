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

    // Above this effective sample size the finite-sample distribution and its
    // n -> infinity limit (Sf above) already agree past the tolerance this
    // package holds itself to, and the matrix cost below stops paying for
    // itself: a few hundred squared is already a comfortable fraction of a
    // second, and a few thousand squared is not.
    private const int LargeSampleThreshold = 200;

    private const int ScaleBits = 128;
    private static readonly double ScaleUp = PowerOfTwo(ScaleBits);
    private static readonly double ScaleDown = PowerOfTwo(-ScaleBits);

    // Math.ScaleB does not exist on netstandard2.0; multiplying by 2.0 (or
    // 0.5) is exact at every step -- it only ever moves the exponent bits, the
    // mantissa is untouched -- so a fixed number of exact steps stands in for
    // it without pulling in System.Numerics or a bit-twiddled ldexp.
    private static double PowerOfTwo(int exponent)
    {
        double factor = exponent >= 0 ? 2.0 : 0.5;
        double result = 1.0;
        for (int i = 0; i < Math.Abs(exponent); i++)
        {
            result *= factor;
        }

        return result;
    }

    private static double ScaleByPowerOfTwo(double value, int exponent)
    {
        if (exponent == 0)
        {
            return value;
        }

        double factor = exponent > 0 ? 2.0 : 0.5;
        double result = value;
        for (int i = 0; i < Math.Abs(exponent); i++)
        {
            result *= factor;
        }

        return result;
    }

    /// <summary>The two-sided <b>finite-sample</b> Kolmogorov tail: P(D_n &gt; d), scipy's <c>kstwo.sf</c>.</summary>
    /// <remarks>
    /// Distinct from <see cref="Sf"/>, which is that statistic's n -&gt;
    /// infinity limit (scipy's <c>kstwobign</c>): measured against
    /// <c>tests/oracles/stats_ks.json</c>'s <c>method="asymp"</c> cases, the two
    /// disagree by orders of magnitude at the sample sizes a two-sample test
    /// actually produces (n = 3, d = 0.7 gives 0.054 here against 0.106 from the
    /// n -&gt; infinity limit), so the asymptotic branch needs this one, not
    /// <see cref="Sf"/>, except where <paramref name="n"/> is large enough that
    /// the two have converged. <paramref name="n"/> is rounded, not truncated:
    /// it arrives as a two-sample test's effective size n1*n2/(n1+n2), which is
    /// rarely integral.
    /// </remarks>
    internal static double FiniteTwoSidedSf(double n, double d)
    {
        if (double.IsNaN(d))
        {
            return double.NaN;
        }
        if (d <= 0.0)
        {
            return 1.0;
        }
        if (d >= 1.0)
        {
            return 0.0;
        }

        int count = Math.Max(1, (int)Math.Round(n, MidpointRounding.ToEven));
        if (count > LargeSampleThreshold)
        {
            return Sf(Math.Sqrt(n) * d);
        }

        // d >= 0.5 evaluated directly through the one-sided survival formula,
        // never through 1 - (a CDF near 1): DurbinCdf(200, 0.9) rounds to
        // exactly 1.0 in a double, and 1 minus that is an exact 0.0 where the
        // true tail is still representable (task-8-report.md, well-separated
        // corpus case).
        return d >= 0.5
            ? Math.Min(1.0, 2.0 * SmirnovSf(count, d))
            : 1.0 - DurbinCdf(count, d);
    }

    // Birnbaum's closed form for the one-sided finite-sample tail P(D_n+ > d):
    // d * sum_{j=0}^{floor(n(1-d))} C(n,j) (j/n+d)^(j-1) (1-d-j/n)^(n-j).
    // Evaluated in log space -- C(n,j) alone overflows a double well before n
    // reaches LargeSampleThreshold.
    private static double SmirnovSf(int n, double d)
    {
        int jMax = (int)Math.Floor(n * (1.0 - d));
        double sum = 0.0;

        for (int j = 0; j <= jMax; j++)
        {
            double a = (j / (double)n) + d;
            double b = 1.0 - d - (j / (double)n);
            double logTerm = LogChoose(n, j) + ((j - 1) * Math.Log(a)) + ((n - j) * Math.Log(b));
            sum += Math.Exp(logTerm);
        }

        return d * sum;
    }

    private static double LogChoose(int n, int k) =>
        Gamma.LogGamma(n + 1) - Gamma.LogGamma(k + 1) - Gamma.LogGamma(n - k + 1);

    // Durbin's (1968) matrix method for P(D_n <= d), in the computationally
    // efficient form Marsaglia, Tsang and Wang (2003) gave it: write d as
    // (k-h)/n, build a (2k-1)-square transition matrix from h, and read the
    // answer off row k of n!/n^n * H^n. No reference implementation is
    // transcribed (ADR 0003); H^n is repeated squaring with the same
    // ScaleUp/ScaleDown rescaling this file already uses for the series sum.
    private static double DurbinCdf(int n, double d)
    {
        double nd = n * d;
        if (nd <= 0.5)
        {
            return 0.0;
        }

        int k = (int)Math.Ceiling(nd);
        double h = k - nd;
        int size = (2 * k) - 1;

        double[][] transition = BuildTransitionMatrix(size, h);
        (double mantissa, int exponent) = RaiseToPower(transition, n, k);

        for (int i = 1; i <= n; i++)
        {
            mantissa = i * mantissa / n;
            if (Math.Abs(mantissa) < ScaleDown)
            {
                mantissa *= ScaleUp;
                exponent -= ScaleBits;
            }
        }

        double probability = ScaleByPowerOfTwo(mantissa, exponent);
        return Math.Min(1.0, Math.Max(0.0, probability));
    }

    // v is H's first column and last row; w is H's remaining diagonal bands.
    // v[j] = (1 - h^(j+1)) / (j+1)!, except the final entry, which folds in the
    // boundary term a plain geometric tail would miss; w[j] = 1/(j+1)!.
    private static double[][] BuildTransitionMatrix(int size, double h)
    {
        double[] v = new double[size];
        double[] w = new double[size];
        double factorial = 1.0;
        for (int j = 1; j <= size; j++)
        {
            w[j - 1] = factorial;
            factorial /= j;
            v[j - 1] = (1.0 - Math.Pow(h, j)) * factorial;
        }

        double tail = Math.Pow(Math.Max((2.0 * h) - 1.0, 0.0), size) - (2.0 * Math.Pow(h, size));
        v[size - 1] = (1.0 + tail) * factorial;

        double[][] matrix = NewMatrix(size);
        for (int column = 1; column < size; column++)
        {
            for (int row = column - 1; row < size; row++)
            {
                matrix[row][column] = w[row - column + 1];
            }
        }
        for (int row = 0; row < size; row++)
        {
            matrix[row][0] = v[row];
        }
        // Overwrites whatever the band loop above left in the last row: every
        // column of that row is v, reversed, not the shifted-diagonal pattern
        // the rest of the matrix carries.
        for (int column = 0; column < size; column++)
        {
            matrix[size - 1][column] = v[size - 1 - column];
        }

        return matrix;
    }

    // H^n by repeated squaring, holding the running power's own scale
    // separately from H's -- the two drift apart because the power only
    // accumulates a factor on the iterations where n's corresponding bit is set.
    private static (double Mantissa, int Exponent) RaiseToPower(double[][] matrix, int n, int k)
    {
        int size = matrix.Length;
        double[][] power = Identity(size);
        double[][] square = matrix;
        int exponent = 0;
        int squareExponent = 0;
        int remaining = n;

        while (remaining > 0)
        {
            if ((remaining & 1) != 0)
            {
                power = Multiply(power, square);
                exponent += squareExponent;
            }

            square = Multiply(square, square);
            squareExponent *= 2;
            if (Math.Abs(square[k - 1][k - 1]) > ScaleUp)
            {
                Rescale(square, ScaleUp);
                squareExponent += ScaleBits;
            }

            remaining >>= 1;
        }

        return (power[k - 1][k - 1], exponent);
    }

    private static double[][] NewMatrix(int size)
    {
        double[][] matrix = new double[size][];
        for (int row = 0; row < size; row++)
        {
            matrix[row] = new double[size];
        }

        return matrix;
    }

    private static double[][] Identity(int size)
    {
        double[][] identity = NewMatrix(size);
        for (int i = 0; i < size; i++)
        {
            identity[i][i] = 1.0;
        }

        return identity;
    }

    private static double[][] Multiply(double[][] left, double[][] right)
    {
        int size = left.Length;
        double[][] result = NewMatrix(size);
        for (int row = 0; row < size; row++)
        {
            for (int inner = 0; inner < size; inner++)
            {
                double value = left[row][inner];

                // S1244: an exact zero here is a sparsity check, not a
                // tolerance comparison -- H is built with genuine exact zeros
                // off its populated bands, and skipping them is what keeps
                // the O(size^3) product from wasting work on terms that add
                // nothing.
#pragma warning disable S1244
                if (value == 0.0)
#pragma warning restore S1244
                {
                    continue;
                }

                double[] resultRow = result[row];
                double[] rightRow = right[inner];
                for (int column = 0; column < size; column++)
                {
                    resultRow[column] += value * rightRow[column];
                }
            }
        }

        return result;
    }

    private static void Rescale(double[][] matrix, double factor)
    {
        for (int row = 0; row < matrix.Length; row++)
        {
            double[] matrixRow = matrix[row];
            for (int column = 0; column < matrixRow.Length; column++)
            {
                matrixRow[column] /= factor;
            }
        }
    }
}
