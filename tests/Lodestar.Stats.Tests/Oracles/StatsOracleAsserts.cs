using Xunit;

namespace Lodestar.Stats.Tests.Oracles;

/// <summary>The two comparisons a hypothesis-test corpus needs, and why they differ.</summary>
/// <remarks>
/// A statistic lives on a scale the data sets, so the repository's 1e-9
/// absolute tolerance is the right one for it. A p-value does not: measured on
/// ordinary corpus cases it reaches 7.85e-26 for a t-test and 2.38e-53 for an
/// ANOVA, and at 1e-9 absolute an implementation returning 0.0 would pass every
/// one of them. The tail is exactly where a hand-written incomplete beta goes
/// wrong, so the tail is compared relatively.
/// </remarks>
internal static class StatsOracleAsserts
{
    private const double Tolerance = 1e-9;

    internal static void Statistic(double expected, double actual, string caseName)
    {
        if (double.IsNaN(expected))
        {
            Assert.True(double.IsNaN(actual), $"{caseName}: expected NaN, got {actual}.");
            return;
        }

        // Fisher's odds ratio is infinite when a diagonal is zero, and a one-sided
        // confidence bound is half-open: both must match sign and infinitude exactly.
        if (double.IsInfinity(expected))
        {
            // S1244: an infinite odds ratio has no tolerance band to fall
            // within -- it is a sentinel, and only the same sentinel matches it.
#pragma warning disable S1244
            Assert.True(
                actual == expected,
                $"{caseName}: expected {expected}, got {actual}.");
#pragma warning restore S1244
            return;
        }

        Assert.True(
            Math.Abs(expected - actual) <= Tolerance,
            $"{caseName}: statistic {actual} is not within {Tolerance} of {expected}.");
    }

    internal static void PValue(double expected, double actual, string caseName)
    {
        if (double.IsNaN(expected))
        {
            Assert.True(double.IsNaN(actual), $"{caseName}: expected NaN, got {actual}.");
            return;
        }

        // S1244: an exact zero has no relative neighbourhood, so it is the one
        // value compared absolutely -- and only an exact zero satisfies it.
#pragma warning disable S1244
        if (expected == 0.0)
        {
            Assert.True(actual == 0.0, $"{caseName}: expected an exact zero, got {actual}.");
            return;
        }
#pragma warning restore S1244

        double relative = Math.Abs(expected - actual) / Math.Abs(expected);
        Assert.True(
            relative <= Tolerance,
            $"{caseName}: p-value {actual} differs from {expected} by {relative} relative, " +
            $"which exceeds {Tolerance}.");
    }

    internal static void Vector(double[] expected, double[] actual, string caseName)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            PValue(expected[i], actual[i], $"{caseName}[{i}]");
        }
    }
}
