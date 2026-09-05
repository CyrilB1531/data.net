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

    // long-comment: this is the complete dispatch table (task-8-report.md's
    //     fix-round-3), not a per-bound note -- fix-round-1 shipped because
    //     one cutoff was applied outside its branch, fix-round-2 shipped
    //     because the branch it was restored to had no upper bound of its
    //     own. Both were a single bound read out of context; this comment
    //     exists so the next change sees every bound scipy has, together.
    //
    // Every threshold below, and the branch order itself, is read from
    // scipy's own _ksstats.py _kolmogn (n of type integer, x in (0, 1)),
    // borrowed under ADR 0003 (scipy is BSD-3, an explicitly permitted
    // behavioural reference) rather than tuned in this file. Its full
    // decision table, computing the survival probability (scipy's cdf=False,
    // what kstwo.sf asks for) rather than the CDF:
    //
    //   t = n * d
    //   1. t <= 0.5                          -> SF = 1 (Ruben-Gambino)
    //   2. 0.5 < t <= 1                      -> exact closed form (Ruben-Gambino)
    //   3. t >= n - 1                        -> SF = 2*(1-d)^n (Ruben-Gambino)
    //   4. d >= 0.5                          -> SF = 2*smirnov(n,d), exact
    //   5. n <= 140, n*d^2 <= 4              -> exact (DMTW to 0.754693, Pomeranz to 4)
    //   6. n <= 140, n*d^2 > 4               -> SF = 2*smirnov(n,d) (Miller's approximation)
    //   7. n > 140, n*d^2 >= 370             -> SF = 0 (underflows a double outright)
    //   8. n > 140, 2.2 <= n*d^2 < 370       -> SF = 2*smirnov(n,d) (direct; scipy's own approximation past 2.2, see below)
    //   9. n > 140, n*d^2 < 2.2, n*d^1.5 <= 1.4  -> exact (DMTW)
    //  10. n > 140, n*d^2 < 2.2, n*d^1.5 > 1.4   -> Pelz-Good asymptotic expansion
    //
    // Rows 1-3 are not separate code paths here: this file has one exact
    // method (DurbinCdf) rather than scipy's three (Ruben-Gambino's closed
    // forms, DMTW, Pomeranz), and DurbinCdf already produces their answer.
    // Row 1 is DurbinCdf's own `nd <= 0.5 -> cdf = 0` guard, the same
    // condition. Row 2: at n*d in (0.5, 1], DurbinCdf's k is always 1 (a
    // trivial 1x1 matrix), and the algebra its matrix reduces to at k = 1 --
    // (2t-1)^n scaled by n!/n^n -- is Ruben-Gambino's own closed form,
    // verified by derivation and cross-checked against scipy directly (n=2,
    // d=0.49: both give exactly 0.5392), not merely close by coincidence.
    // Row 3 needs t >= n - 1, i.e. d >= (n-1)/n; for every n >= 2 that is
    // already >= 0.5, so it is a subset of row 4, already handled before
    // n*d^2 is even computed. (n = 1 forces t >= n - 1 unconditionally,
    // since n - 1 = 0 -- verified directly against every d, matched by
    // DurbinCdf's own row-1 guard there too.) Rows 5-6 are this round's
    // fix: fix-round-2 restored row 5 but let it run unbounded into row 6's
    // territory, where 1 - DurbinCdf collapses the identical way row 8's
    // direct formula exists to prevent -- FiniteTwoSidedSf(140, 0.495) gave
    // 1.44e-15 against scipy's 3.36e-32, a floor at 2^-51, not an answer
    // (task-8-report.md's fix-round-3 sweep has the full measurement).
    private const int LargeSampleBranch = 140;
    private const double UnderflowThreshold = 370.0;
    private const double DirectSurvivalThreshold = 2.2;
    private const double ExactRouteCeiling = 4.0;
    private const int ExactMatrixCap = 100_000;
    private const double ExactSlopeThreshold = 1.4;

    private const int ScaleBits = 128;
    private static readonly double ScaleUp = PowerOfTwo(ScaleBits);
    private static readonly double ScaleDown = PowerOfTwo(-ScaleBits);

    // Math.ScaleB does not exist on netstandard2.0; repeated multiply/divide by
    // 2.0 is exact (only ever moves the exponent bits), standing in for it here.
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
    /// Distinct from <see cref="Sf"/> (scipy's <c>kstwobign</c>, the n -&gt; infinity limit): the two
    /// disagree by percent-level amounts even at n in the hundreds, narrowing only as 1/sqrt(n) -- no
    /// realistic n closes the gap. Reproduces scipy's own dispatch (decision table above the threshold
    /// constants), including its approximated bands, since parity with scipy is the contract, not a
    /// more accurate value scipy does not return. <paramref name="n"/> is rounded, not truncated -- it
    /// is a two-sample test's effective size n1*n2/(n1+n2), rarely integral.
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

        // Row 4 (see the dispatch table above): exact, since D_n+ >= d and
        // D_n- >= d cannot both hold once d >= 0.5.
        if (d >= 0.5)
        {
            return Math.Min(1.0, 2.0 * SmirnovSf(count, d));
        }

        // Rows 5-6 (d < 0.5, n <= 140): DurbinCdf's exact route is safe only up to
        // n*d^2 = 4 -- past it, 1 - DurbinCdf collapses the way row 8 exists to prevent.
        if (count <= LargeSampleBranch)
        {
            double smallSampleDSquared = count * d * d;
            return smallSampleDSquared <= ExactRouteCeiling
                ? 1.0 - DurbinCdf(count, d)
                : Math.Min(1.0, 2.0 * SmirnovSf(count, d));
        }

        // Row 7: n > 140 from here.
        double countDSquared = count * d * d;
        if (countDSquared >= UnderflowThreshold)
        {
            return 0.0;
        }

        // Row 8: DurbinCdf(200, 0.9) rounds to exactly 1.0, so 1 - DurbinCdf collapses
        // to 0 where the true tail is still representable -- use the direct formula instead.
        if (countDSquared >= DirectSurvivalThreshold)
        {
            return Math.Min(1.0, 2.0 * SmirnovSf(count, d));
        }

        // Rows 9-10 (n*d^2 < 2.2): scipy's exact DMTW route is capped at n*d^1.5 <= 1.4;
        // past it, Pelz-Good -- parity with what scipy returns, not a more accurate number.
        return count <= ExactMatrixCap && count * Math.Pow(d, 1.5) <= ExactSlopeThreshold
            ? 1.0 - DurbinCdf(count, d)
            : 1.0 - PelzGoodCdf(count, d);
    }

    // Birnbaum's closed form, P(D_n+ > d) = d*sum_j C(n,j)(j/n+d)^(j-1)(1-d-j/n)^(n-j),
    // evaluated in log space since C(n,j) alone overflows a double at the sizes this hits.
    private static double SmirnovSf(int n, double d)
    {
        int jMax = (int)Math.Floor(n * (1.0 - d));
        double sum = 0.0;

        for (int j = 0; j <= jMax; j++)
        {
            double a = (j / (double)n) + d;

            // b can land a few ULPs below 0 purely from rounding in j/n, never because the
            // true probability went negative; Math.Log of that is NaN, unlike a genuine 0's -Infinity.
            double b = Math.Max(0.0, 1.0 - d - (j / (double)n));

            double logTerm = LogChoose(n, j) + ((j - 1) * Math.Log(a)) + ((n - j) * Math.Log(b));
            sum += Math.Exp(logTerm);
        }

        return d * sum;
    }

    private static double LogChoose(int n, int k) =>
        Gamma.LogGamma(n + 1) - Gamma.LogGamma(k + 1) - Gamma.LogGamma(n - k + 1);

    private const double MinLog = -708.0;
    private const double PiSquared = Math.PI * Math.PI;
    private const double PiFour = PiSquared * PiSquared;
    private const double PiSix = PiSquared * PiFour;
    private static readonly double Sqrt2Pi = Math.Sqrt(2.0 * Math.PI);
    private static readonly double Sqrt3 = Math.Sqrt(3.0);

    // long-comment: attribution CONTRIBUTING.md's Licensing and provenance
    //     requires travel with the code, not only live in a report -- this
    //     structure was read from scipy's own _kolmogn_PelzGood (ADR 0003
    //     permits scipy, BSD-3, as a behavioural reference) and that has to
    //     be visible here, not merely in task-8-report.md's fix-round-2.
    // Pelz & Good (1976): transforms the Li-Chien/Korolyuk large-n asymptotic
    // expansion of the two-sided one-sample Kolmogorov CDF into a form that
    // converges quickly for the small z = sqrt(n)*d this method is reached at
    // -- scipy's own fallback once DurbinCdf's n*d^1.5 <= 1.4 cap is exceeded.
    // Deliberately less accurate than DurbinCdf would be here: an asymptotic
    // expansion, not an exact computation, because this package's contract is
    // parity with what scipy actually returns, not a more accurate number it
    // does not. The mathematics is Pelz & Good's, published; the four-term
    // expansion's structure (k[0..3], its coefficients, and the convergence
    // sums in <see cref="PelzGoodConvergenceSums"/>) follows scipy's own
    // _kolmogn_PelzGood.
    private static double PelzGoodCdf(int n, double d)
    {
        double z = Math.Sqrt(n) * d;
        double zSquared = z * z;

        double qLog = -PiSquared / 8.0 / zSquared;
        if (qLog < MinLog)
        {
            return 0.0;
        }

        double q = Math.Exp(qLog);
        double[] k = PelzGoodExpansion(z, zSquared, q);

        (double k2Correction, double k3Correction) = PelzGoodConvergenceSums(z, zSquared);
        k[2] += k2Correction;
        k[3] += k3Correction;

        double sqrtN = Math.Sqrt(n);
        double[] powersOfN = [1.0, sqrtN, n, n * sqrtN];
        double sum = 0.0;
        for (int i = 0; i < k.Length; i++)
        {
            sum += k[i] / powersOfN[i];
        }

        return sum;
    }

    // The four-term k[0..3] sum over odd m = 2*step-1, folding in one term per
    // iteration from the largest step down, rescaling by q^(8*step) each time.
    private static double[] PelzGoodExpansion(double z, double zSquared, double q)
    {
        double zFour = zSquared * zSquared;
        double zSix = zSquared * zFour;

        double k1A = -zSquared;
        double k1B = PiSquared / 4.0;
        double k2A = (6.0 * zSix) + (2.0 * zFour);
        double k2B = ((2.0 * zFour) - (5.0 * zSquared)) * PiSquared / 4.0;
        double k2C = PiFour * (1.0 - (2.0 * zSquared)) / 16.0;
        double k3D = PiSix * (5.0 - (30.0 * zSquared)) / 64.0;
        double k3C = PiFour * ((-60.0 * zSquared) + (212.0 * zFour)) / 16.0;
        double k3B = PiSquared * ((135.0 * zFour) - (96.0 * zSix)) / 4.0;
        double k3A = (-30.0 * zSix) - (90.0 * zSquared * zSix);

        double[] k = new double[4];
        int maxStep = (int)Math.Ceiling(16.0 * z / Math.PI);
        for (int step = maxStep; step >= 1; step--)
        {
            double m = (2.0 * step) - 1.0;
            double mSquared = m * m;
            double mFour = mSquared * mSquared;
            double mSix = mSquared * mFour;
            double qPower = Math.Pow(q, 8 * step);

            k[0] = (k[0] * qPower) + 1.0;
            k[1] = (k[1] * qPower) + k1A + (k1B * mSquared);
            k[2] = (k[2] * qPower) + k2A + (k2B * mSquared) + (k2C * mFour);
            k[3] = (k[3] * qPower) + k3A + (k3B * mSquared) + (k3C * mFour) + (k3D * mSix);
        }

        for (int i = 0; i < k.Length; i++)
        {
            k[i] *= q;
            k[i] *= Sqrt2Pi;
        }

        double zSeven = zSix * z;
        double zTen = zSix * zFour;
        k[0] /= z;
        k[1] /= 6.0 * zFour;
        k[2] /= 72.0 * zSeven;
        k[3] /= 6480.0 * zTen;

        return k;
    }

    // A second, directly-summed pair of series (k[2], k[3] only) that
    // <see cref="PelzGoodExpansion"/>'s transform does not fold in on its own -- a different q (base).
    private static (double K2, double K3) PelzGoodConvergenceSums(double z, double zSquared)
    {
        double zThree = zSquared * z;
        double zSix = zThree * zThree;
        double q = Math.Exp(-PiSquared / 2.0 / zSquared);
        double sqrt3Z = Sqrt3 * z;

        int maxStep = (int)Math.Ceiling(16.0 * z / Math.PI);
        double k2Extra = 0.0;
        double k3Extra = 0.0;
        for (int step = maxStep; step >= 1; step--)
        {
            double stepSquared = (double)step * step;
            double stepPi = Math.PI * step;
            double qPower = Math.Pow(q, stepSquared);

            k2Extra += stepSquared * qPower;
            k3Extra += (sqrt3Z + stepPi) * (sqrt3Z - stepPi) * stepSquared * qPower;
        }

        k2Extra *= PiSquared * Sqrt2Pi / (-36.0 * zThree);
        k3Extra *= PiSquared * Sqrt2Pi / (216.0 * zSix);

        return (k2Extra, k3Extra);
    }

    // long-comment: fix-round-1's finding 4 caught a wrong provenance claim
    //     here ("no reference implementation transcribed") -- this is the
    //     corrected version, and the distinction it draws (mathematics vs.
    //     scaling-implementation detail) is exactly what was wrong before,
    //     so it needs to stay explicit rather than collapse back to a claim
    //     a future edit cannot check against ADR 0003.
    // Durbin's (1968) matrix method for P(D_n <= d), in the computationally
    // efficient form Marsaglia, Tsang and Wang (2003) gave it: write d as
    // (k-h)/n, build a (2k-1)-square transition matrix from h, raise it to
    // the n-th power and read the k-th diagonal entry, scaled by the
    // falling-factorial correction the loop below applies. The mathematics
    // is theirs, published. ADR 0003 permits scipy (BSD-3) as a behavioural
    // reference, and this implementation leans on it past the mathematics too:
    // ScaleBits = 128 is scipy's own choice (its _E128/_EP128,
    // _ksstats.py:73-75), not a value out of Marsaglia-Tsang-Wang, and the
    // magnitude check below that triggers a rescale is scipy's placement of
    // it, not independently derived -- both are scaling-implementation
    // detail, not the mathematics itself.
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

    // v is H's first column and last row (v[j] = (1-h^(j+1))/(j+1)!, final entry folds
    // in the boundary term a plain geometric tail would miss); w[t] = 1/t! is the rest.
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
        // Overwrites the band loop's last row: every column there is v, reversed,
        // not the shifted-diagonal pattern the rest of the matrix carries.
        for (int column = 0; column < size; column++)
        {
            matrix[size - 1][column] = v[size - 1 - column];
        }

        return matrix;
    }

    // H^n by repeated squaring, holding the running power's scale separately from
    // H's -- they drift apart since the power only scales on bits of n that are set.
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
