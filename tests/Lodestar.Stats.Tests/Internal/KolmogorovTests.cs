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
}
