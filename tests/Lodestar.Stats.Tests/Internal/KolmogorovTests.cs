using Lodestar.Stats.Internal;
using Xunit;

namespace Lodestar.Stats.Tests.Internal;

/// <summary>The Kolmogorov distribution's upper tail.</summary>
public sealed class KolmogorovTests
{
    [Fact]
    public void Sf_is_one_at_and_below_zero()
    {
        Assert.Equal(1.0, Kolmogorov.Sf(0.0));
        Assert.Equal(1.0, Kolmogorov.Sf(-1.0));
    }

    [Theory]
    // Q(lambda) = 2 * sum_{k>=1} (-1)^{k-1} exp(-2 k^2 lambda^2), compared here
    // against scipy.stats.kstwobign.sf, which agrees with the series to every digit.
    [InlineData(0.5, 0.9639452436648751)]
    [InlineData(1.0, 0.26999967167735456)]
    [InlineData(1.36, 0.049485876755377876)]
    [InlineData(2.0, 0.0006709252557796953)]
    public void Sf_matches_the_series(double lambda, double expected)
    {
        Assert.Equal(expected, Kolmogorov.Sf(lambda), 1e-14);
    }

    [Fact]
    public void Sf_clamps_to_one_at_lambda_0_1()
    {
        // The true value differs from 1.0 by ~7e-49, below a double's precision
        // there -- scipy.stats.kstwobign.sf(0.1) also returns exactly 1.0.
        Assert.Equal(1.0, Kolmogorov.Sf(0.1));
    }

    [Fact]
    public void Sf_is_strictly_monotone_decreasing()
    {
        // Starting past 0.1 keeps every step strict: at 0.1 itself Sf clamps to
        // exactly 1.0, which the fact above covers separately.
        double previous = Kolmogorov.Sf(0.2);
        for (double lambda = 0.3; lambda < 4.0; lambda += 0.1)
        {
            double current = Kolmogorov.Sf(lambda);
            Assert.True(current < previous, $"Q({lambda}) = {current} did not fall below {previous}.");
            previous = current;
        }
    }

    // Against scipy 1.18.0's kstwo.sf(d, n), independently reproduced in Python
    // for KolmogorovSmirnov.TwoSample's own asymp corpus cases
    // (task-8-report.md): d < 0.5 exercises the Durbin matrix path, d >= 0.5
    // the Birnbaum closed form.
    [Theory]
    [InlineData(2.0, 0.4, 0.82)]
    [InlineData(3.0, 1.0 / 3.0, 0.7777777777777778)]
    [InlineData(3.0, 0.7, 0.054)]
    [InlineData(20.0, 0.9, 2.0006866455077592e-20)]
    public void FiniteTwoSidedSf_matches_scipy_kstwo(double n, double d, double expected)
    {
        double actual = Kolmogorov.FiniteTwoSidedSf(n, d);
        double relative = Math.Abs(actual - expected) / expected;
        Assert.True(relative <= 1e-9, $"FiniteTwoSidedSf({n}, {d}) = {actual}, expected {expected}.");
    }

    [Fact]
    public void FiniteTwoSidedSf_falls_back_to_the_n_to_infinity_limit_past_its_threshold()
    {
        // n = 300 is past LargeSampleThreshold (200): the matrix path is
        // skipped entirely in favour of Sf(sqrt(n) * d), so this is exact
        // agreement with Sf, not merely close to it.
        double expected = Kolmogorov.Sf(Math.Sqrt(300.0) * 0.1);

        Assert.Equal(expected, Kolmogorov.FiniteTwoSidedSf(300.0, 0.1));
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    public void FiniteTwoSidedSf_is_one_at_and_below_zero(double d)
    {
        Assert.Equal(1.0, Kolmogorov.FiniteTwoSidedSf(5.0, d));
    }

    [Fact]
    public void FiniteTwoSidedSf_is_zero_at_and_above_one()
    {
        Assert.Equal(0.0, Kolmogorov.FiniteTwoSidedSf(5.0, 1.0));
        Assert.Equal(0.0, Kolmogorov.FiniteTwoSidedSf(5.0, 1.5));
    }
}
