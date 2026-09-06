namespace Lodestar.Stats;

/// <summary>A test statistic and the p-value that goes with it.</summary>
/// <remarks>
/// Eight of the ten families return exactly this, because eight of the ten
/// scipy calls return exactly this — measured, not assumed. The three that
/// carry more have their own record below rather than making the other eight
/// pay for fields they would leave empty.
/// </remarks>
/// <param name="Statistic">The test statistic, on whichever scale the family defines.</param>
/// <param name="PValue">The probability of a statistic at least this extreme under the null.</param>
public sealed record TestResult(double Statistic, double PValue);

/// <summary>A t-test's result: the statistic, the p-value and the degrees of freedom.</summary>
/// <param name="Statistic">The t statistic.</param>
/// <param name="PValue">The p-value on the requested tail.</param>
/// <param name="Df">
/// The degrees of freedom. Integral for Student and for the paired and
/// one-sample tests; fractional for Welch, whose Satterthwaite denominator is
/// not a count of anything.
/// </param>
public sealed record TTestResult(double Statistic, double PValue, double Df)
{
    /// <summary>The quantity the test compared: a mean, or a difference of means.</summary>
    /// <remarks>
    /// Internal rather than public. It exists so a later confidence-interval
    /// method can be added to the result instead of a second call that
    /// re-derives everything, and scipy keeps it hidden on its own result for
    /// the same reason.
    /// </remarks>
    internal double Estimate { get; init; }

    /// <summary>The standard error of <see cref="Estimate"/>. Internal, as above.</summary>
    internal double StandardError { get; init; }

    /// <summary>Which tail was tested, which decides whether an interval is half-open.</summary>
    internal Alternative Alternative { get; init; }

    /// <summary>The confidence interval for the difference this test measured.</summary>
    /// <remarks>
    /// A method, not a property, because it takes a level -- scipy exposes it
    /// the same way via <c>TtestResult.confidence_interval</c>; a named tuple
    /// avoids a public record for what is only a two-double carrier. A
    /// one-sided interval is half-open, not narrower: it says nothing about how
    /// far the difference could be, so the far bound is an infinity rather than a number.
    /// </remarks>
    /// <param name="level">The confidence level, strictly between 0 and 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="level"/> is NaN or outside <c>(0, 1)</c>.
    /// </exception>
    public (double Low, double High) ConfidenceInterval(double level = 0.95)
    {
        if (double.IsNaN(level) || level <= 0.0 || level >= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level), level, "The confidence level must lie strictly inside (0, 1).");
        }

        // A one-sided test spends its whole error budget on one side, so the tail
        // is 1 - level rather than half of it, and the other bound is infinite.
        double tail = Alternative == Alternative.TwoSided ? (1.0 - level) / 2.0 : 1.0 - level;
        double half = Internal.Beta.StudentQuantile(tail, Df) * StandardError;

        return Alternative switch
        {
            Alternative.TwoSided => (Estimate - half, Estimate + half),
            Alternative.Greater => (Estimate - half, double.PositiveInfinity),
            Alternative.Less => (double.NegativeInfinity, Estimate + half),
            // CA2208: Alternative is a field, not a parameter here -- a bad
            // value stored on the result is a broken invariant, not a bad call.
            _ => throw new InvalidOperationException($"Unrecognised alternative: {Alternative}."),
        };
    }
}

/// <summary>A contingency-table chi-square result.</summary>
/// <param name="Statistic">The chi-square statistic.</param>
/// <param name="PValue">The upper-tail p-value.</param>
/// <param name="Dof">The degrees of freedom, <c>(rows - 1) * (columns - 1)</c>.</param>
/// <param name="ExpectedFrequencies">
/// The table expected under independence, row-major, same shape as the input.
/// </param>
// CA1819 (properties should not return arrays), S2368 (no jagged-array constructor
// parameters): the expected table mirrors the shape of the caller's own input
// table, itself double[][] because that is how chi2_contingency takes it. Wrapping
// one side and not the other buys no safety, only a conversion at the boundary.
#pragma warning disable CA1819, S2368
public sealed record Chi2ContingencyResult(
    double Statistic, double PValue, int Dof, double[][] ExpectedFrequencies);
#pragma warning restore CA1819, S2368

/// <summary>A two-sample Kolmogorov-Smirnov result.</summary>
/// <param name="Statistic">The supremum distance between the two empirical distributions.</param>
/// <param name="PValue">The p-value on the requested tail.</param>
/// <param name="StatisticLocation">The observed value at which that supremum is attained.</param>
/// <param name="StatisticSign">
/// <c>+1</c> when the first sample's empirical distribution exceeds the second's
/// at that point, <c>-1</c> when it falls below.
/// </param>
public sealed record KsResult(
    double Statistic, double PValue, double StatisticLocation, int StatisticSign);
