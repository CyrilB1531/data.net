using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>Student's and Welch's t-tests: independent, paired and one-sample.</summary>
/// <remarks>
/// Arrays in, a statistic and a p-value out; every entry point is static.
/// <see cref="Independent"/> defaults to <see cref="Variance.Welch"/>, where
/// <c>scipy.stats.ttest_ind</c> defaults to Student's <c>equal_var=True</c>:
/// pooling is only correct when the two populations share a variance, so the
/// safer default costs a word at the call site rather than a wrong answer.
/// </remarks>
public static class TTest
{
    /// <summary>The two-sample t-test on independent samples.</summary>
    /// <param name="a">The first sample; at least two values.</param>
    /// <param name="b">The second sample; at least two values.</param>
    /// <param name="alternative">Which tail the p-value covers.</param>
    /// <param name="variance">Whether to pool the two variances.</param>
    /// <returns>The statistic, the p-value and the degrees of freedom.</returns>
    /// <exception cref="ArgumentException">Either sample holds fewer than two values.</exception>
    public static TTestResult Independent(
        ReadOnlySpan<double> a,
        ReadOnlySpan<double> b,
        Alternative alternative = Alternative.TwoSided,
        Variance variance = Variance.Welch)
    {
        RequireAtLeastTwo(a, nameof(a));
        RequireAtLeastTwo(b, nameof(b));

        (double meanA, double varianceA) = MeanAndVariance(a);
        (double meanB, double varianceB) = MeanAndVariance(b);
        int n = a.Length;
        int m = b.Length;

        double standardError;
        double df;
        if (variance == Variance.Equal)
        {
            double pooled = (((n - 1) * varianceA) + ((m - 1) * varianceB)) / (n + m - 2);
            standardError = Math.Sqrt(pooled * ((1.0 / n) + (1.0 / m)));
            df = n + m - 2;
        }
        else
        {
            double termA = varianceA / n;
            double termB = varianceB / m;
            standardError = Math.Sqrt(termA + termB);

            // Welch-Satterthwaite. The denominator divides by n-1 and m-1, which
            // is why both samples must hold at least two values.
            double numerator = (termA + termB) * (termA + termB);
            df = numerator / ((termA * termA / (n - 1)) + (termB * termB / (m - 1)));
        }

        double difference = meanA - meanB;
        return Build(difference / standardError, difference, standardError, df, alternative);
    }

    /// <summary>The paired t-test: a one-sample test on the differences.</summary>
    /// <param name="a">The first measurement of each pair.</param>
    /// <param name="b">The second measurement of each pair, in the same order.</param>
    /// <param name="alternative">Which tail the p-value covers.</param>
    /// <returns>The statistic, the p-value and the degrees of freedom.</returns>
    /// <exception cref="ArgumentException">
    /// The samples differ in length, or hold fewer than two pairs.
    /// </exception>
    public static TTestResult Paired(
        ReadOnlySpan<double> a,
        ReadOnlySpan<double> b,
        Alternative alternative = Alternative.TwoSided)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException(
                $"A paired test needs the same number of values in both samples; got {a.Length} and {b.Length}.",
                nameof(b));
        }

        RequireAtLeastTwo(a, nameof(a));

        double[] differences = new double[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            differences[i] = a[i] - b[i];
        }

        return OneSample(differences, 0.0, alternative);
    }

    /// <summary>The one-sample t-test against a stated population mean.</summary>
    /// <param name="sample">The sample; at least two values.</param>
    /// <param name="populationMean">The mean the null hypothesis states.</param>
    /// <param name="alternative">Which tail the p-value covers.</param>
    /// <returns>The statistic, the p-value and the degrees of freedom.</returns>
    /// <exception cref="ArgumentException"><paramref name="sample"/> holds fewer than two values.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="populationMean"/> is NaN or infinite.
    /// </exception>
    public static TTestResult OneSample(
        ReadOnlySpan<double> sample,
        double populationMean,
        Alternative alternative = Alternative.TwoSided)
    {
        RequireAtLeastTwo(sample, nameof(sample));

        if (double.IsNaN(populationMean) || double.IsInfinity(populationMean))
        {
            throw new ArgumentOutOfRangeException(
                nameof(populationMean), populationMean, "The population mean must be finite.");
        }

        (double mean, double variance) = MeanAndVariance(sample);
        double standardError = Math.Sqrt(variance / sample.Length);
        double statistic = (mean - populationMean) / standardError;

        // A confidence interval centres on the sample mean, not the statistic's
        // offset from populationMean: scipy brackets the population mean itself.
        return Build(statistic, mean, standardError, sample.Length - 1, alternative);
    }

    private static void RequireAtLeastTwo(ReadOnlySpan<double> values, string name)
    {
        if (values.Length < 2)
        {
            throw new ArgumentException(
                $"A t-test needs at least two values; got {values.Length}.", name);
        }
    }

    // The sample variance, n-1 denominator, in two passes: the sum-of-squares
    // shortcut cancels catastrophically once the mean dominates the spread -- a real corpus case.
    private static (double Mean, double Variance) MeanAndVariance(ReadOnlySpan<double> values)
    {
        double sum = 0.0;
        for (int i = 0; i < values.Length; i++)
        {
            sum += values[i];
        }

        double mean = sum / values.Length;

        double squares = 0.0;
        for (int i = 0; i < values.Length; i++)
        {
            double deviation = values[i] - mean;
            squares += deviation * deviation;
        }

        return (mean, squares / (values.Length - 1));
    }

    // estimate is what a confidence interval centres on; it is not always the
    // statistic's numerator -- OneSample's is offset by a value the interval ignores.
    private static TTestResult Build(
        double statistic, double estimate, double standardError, double df, Alternative alternative)
    {
        double pValue = PValue(statistic, df, alternative);

        return new TTestResult(statistic, pValue, df)
        {
            Estimate = estimate,
            StandardError = standardError,
            Alternative = alternative,
        };
    }

    internal static double PValue(double statistic, double df, Alternative alternative)
    {
        if (double.IsNaN(statistic))
        {
            return double.NaN;
        }

        return alternative switch
        {
            // Twice the tail at |t|, not one minus the tail at -t: the latter
            // cancels to zero exactly where the former is 1e-53.
            Alternative.TwoSided => 2.0 * Beta.StudentSf(Math.Abs(statistic), df),
            Alternative.Greater => Beta.StudentSf(statistic, df),
            Alternative.Less => Beta.StudentSf(-statistic, df),
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };
    }
}
