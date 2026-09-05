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
    // (task-8-report.md): the DirectSurvivalThreshold/UnderflowThreshold
    // cases below exercise the Durbin matrix path or the Birnbaum closed
    // form depending on n * d^2, not d alone -- see the fix-round-1 section
    // of task-8-report.md for why d alone was wrong. The last five rows are
    // fix-round-2's finding 3: n > 140, n * d^2 < 2.2, n * d^1.5 > 1.4 is
    // the band scipy's own kstwo.sf hands off to the Pelz-Good asymptotic
    // expansion in, and PelzGoodCdf is what these exercise.
    [Theory]
    [InlineData(2.0, 0.4, 0.82)]
    [InlineData(3.0, 1.0 / 3.0, 0.7777777777777778)]
    [InlineData(3.0, 0.7, 0.054)]
    [InlineData(20.0, 0.9, 2.0006866455077592e-20)]
    [InlineData(146.0, 0.122011, 0.023677600444463653)]
    [InlineData(149.0, 0.07135223654394825, 0.41473880155231346)]
    [InlineData(200.0, 0.06, 0.45015844138021865)]
    [InlineData(500.0, 0.03, 0.7473166700457021)]
    [InlineData(1000.0, 0.02, 0.8108971656895577)]
    public void FiniteTwoSidedSf_matches_scipy_kstwo(double n, double d, double expected)
    {
        double actual = Kolmogorov.FiniteTwoSidedSf(n, d);
        double relative = Math.Abs(actual - expected) / expected;
        Assert.True(relative <= 1e-9, $"FiniteTwoSidedSf({n}, {d}) = {actual}, expected {expected}.");
    }

    // Fix-round-1, finding 1: n = 201 is what KolmogorovSmirnov.TwoSample
    // computes as the effective sample size for two n1 = m1 = 402 samples,
    // one past the LargeSampleThreshold = 200 this method used to fall back
    // to Sf at. scipy's kstwo.sf(0.114428, 201) is 0.009498083878988563; the
    // old fallback returned Sf(sqrt(201) * 0.114428) = 0.010352291673092562
    // instead, a large enough gap to flip a decision at alpha = 0.01. n = 201,
    // d = 0.114428 lands in this method's exact route (n * d^2 = 2.63, past
    // DirectSurvivalThreshold), so this is still the exact value, though the
    // method as a whole is no longer exact everywhere past n = 140 -- see
    // PelzGoodCdf's own facts below for the band where it isn't.
    [Fact]
    public void FiniteTwoSidedSf_no_longer_falls_back_past_the_former_threshold()
    {
        double actual = Kolmogorov.FiniteTwoSidedSf(201.0, 0.114428);
        double relative = Math.Abs(actual - 0.009498083878988563) / 0.009498083878988563;

        Assert.True(relative <= 1e-9, $"FiniteTwoSidedSf(201, 0.114428) = {actual}.");
    }

    // Fix-round-1, finding 2: d < 0.5 with n large enough still drove
    // 1 - DurbinCdf into the same collapse the d >= 0.5 guard was meant to
    // avoid (n * d^2 = 27, comfortably past DirectSurvivalThreshold = 2.2,
    // even though d itself is below 0.5). scipy's kstwo.sf(0.3, 300) is
    // 1.92786406567219e-24; the old d >= 0.5 criterion took the 1 - CDF
    // route here and lost the tail entirely.
    [Fact]
    public void FiniteTwoSidedSf_does_not_collapse_below_0_5_when_n_times_d_squared_is_large()
    {
        double actual = Kolmogorov.FiniteTwoSidedSf(300.0, 0.3);
        double relative = Math.Abs(actual - 1.92786406567219e-24) / 1.92786406567219e-24;

        Assert.True(relative <= 1e-9, $"FiniteTwoSidedSf(300, 0.3) = {actual}.");
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

    // Fix-round-2, finding 1: n = 131, d = 0.1297709923664122 has
    // n * d^2 = 2.206, past DirectSurvivalThreshold (2.2) -- fix-round-1
    // applied that threshold at every n, so this used to take the direct
    // survival-formula approximation (Miller's, worst measured 1.10e-6 off)
    // instead of the exact route n <= 140 keeps available (Pomeranz in
    // scipy, DurbinCdf here). scipy's kstwo.sf(0.1297709923664122, 131) is
    // 0.022036162383739684.
    [Fact]
    public void FiniteTwoSidedSf_stays_exact_through_n_140_regardless_of_n_times_d_squared()
    {
        double actual = Kolmogorov.FiniteTwoSidedSf(131.0, 0.1297709923664122);
        double relative = Math.Abs(actual - 0.022036162383739684) / 0.022036162383739684;

        Assert.True(relative <= 1e-9, $"FiniteTwoSidedSf(131, 0.1297709923664122) = {actual}.");
    }

}
