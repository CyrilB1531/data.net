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

    // Against scipy 1.18.0's kstwo.sf(d, n) (task-8-report.md): rows exercise the Durbin/Birnbaum
    // dispatch by n*d^2, the Pelz-Good band (fix-round-2), and the fix-round-3 seam/regression cases.
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
    [InlineData(140.0, 0.4, 9.968909167860116e-21)]
    [InlineData(140.0, 0.1690298509457033, 0.0005762076570011542)]
    [InlineData(140.0, 0.1690318509457033, 0.0005760965560341838)]
    [InlineData(200.0, 0.10487988481701516, 0.022759335835360828)]
    [InlineData(200.0, 0.10488188481701516, 0.02275522917915266)]
    [InlineData(500.0, 0.019865667767585377, 0.98697646335848)]
    [InlineData(500.0, 0.019865867767585376, 0.9869750179585457)]
    [InlineData(140.0, 0.1, 0.11353657290090946)]
    [InlineData(141.0, 0.1, 0.11128445516467944)]
    public void FiniteTwoSidedSf_matches_scipy_kstwo(double n, double d, double expected)
    {
        double actual = Kolmogorov.FiniteTwoSidedSf(n, d);
        double relative = Math.Abs(actual - expected) / expected;
        Assert.True(relative <= 1e-9, $"FiniteTwoSidedSf({n}, {d}) = {actual}, expected {expected}.");
    }

    // Fix-round-1: n=201 was past the removed LargeSampleThreshold=200 fallback -- scipy's
    // kstwo.sf gives 0.009498083878988563, the old Sf-fallback gave 0.010352291673092562 (flips alpha=0.01).
    [Fact]
    public void FiniteTwoSidedSf_no_longer_falls_back_past_the_former_threshold()
    {
        double actual = Kolmogorov.FiniteTwoSidedSf(201.0, 0.114428);
        double relative = Math.Abs(actual - 0.009498083878988563) / 0.009498083878988563;

        Assert.True(relative <= 1e-9, $"FiniteTwoSidedSf(201, 0.114428) = {actual}.");
    }

    // Fix-round-1: d < 0.5 with n*d^2 = 27 (past DirectSurvivalThreshold) still drove the old
    // d>=0.5 criterion into 1-DurbinCdf's collapse -- scipy's kstwo.sf(0.3, 300) is 1.92786406567219e-24.
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

    // Fix-round-2: n=131, n*d^2=2.206 past DirectSurvivalThreshold -- applied at every n by
    // fix-round-1, wrongly taking the direct approximation (1.10e-6 off) here; scipy gives 0.022036162383739684.
    [Fact]
    public void FiniteTwoSidedSf_stays_exact_through_n_140_regardless_of_n_times_d_squared_below_the_ceiling()
    {
        double actual = Kolmogorov.FiniteTwoSidedSf(131.0, 0.1297709923664122);
        double relative = Math.Abs(actual - 0.022036162383739684) / 0.022036162383739684;

        Assert.True(relative <= 1e-9, $"FiniteTwoSidedSf(131, 0.1297709923664122) = {actual}.");
    }

    // long-comment: 1.44e-15 is not a rough approximation of the true
    //     3.36e-32 -- it is exactly 2^-51, a double's cancellation floor for
    //     1 - x once x has rounded to 1.0. That distinction is the reason
    //     ExactRouteCeiling exists, so it needs to survive here, not just in
    //     task-8-report.md's fix-round-3 transcript.
    // Fix-round-3's own defect: fix-round-2's exact route for n <= 140 (the fact above) had no
    // upper bound, reaching the same 1-CDF collapse from underneath n=140 instead of above it.
    // Delete-and-confirm: removing ExactRouteCeiling (routing every n<=140 through DurbinCdf
    // regardless of n*d^2, fix-round-2's shipped behaviour) reproduces the same 1.44e-15.
    [Fact]
    public void FiniteTwoSidedSf_does_not_collapse_past_the_exact_route_ceiling_below_n_140()
    {
        double actual = Kolmogorov.FiniteTwoSidedSf(140.0, 0.495);
        double relative = Math.Abs(actual - 3.3586697257991026e-32) / 3.3586697257991026e-32;

        Assert.True(relative <= 1e-6, $"FiniteTwoSidedSf(140, 0.495) = {actual}.");
    }

    // DirectSurvivalThreshold's seam (n > 140), pinned separately from ExactRouteCeiling's
    // (rows 5-6): delete-and-confirm reproduces the identical collapse shape (task-8-report.md).
    [Fact]
    public void FiniteTwoSidedSf_does_not_collapse_past_direct_survival_threshold_above_n_140()
    {
        double actual = Kolmogorov.FiniteTwoSidedSf(200.0, 0.45);
        double expected = 1.7611929039602656e-37;
        double relative = Math.Abs(actual - expected) / expected;

        Assert.True(relative <= 1e-6, $"FiniteTwoSidedSf(200, 0.45) = {actual}.");
    }

    // Row 7 seam (n*d^2 >= UnderflowThreshold): both sides underflow to an honest, exact 0.0
    // in scipy itself too -- confirms crossing it changes nothing observable, not a bug.
    [Theory]
    [InlineData(2000.0, 0.4301161633521313)]
    [InlineData(2000.0, 0.4301163633521313)]
    public void FiniteTwoSidedSf_is_exactly_zero_at_the_underflow_threshold(double n, double d)
    {
        Assert.Equal(0.0, Kolmogorov.FiniteTwoSidedSf(n, d));
    }
}
