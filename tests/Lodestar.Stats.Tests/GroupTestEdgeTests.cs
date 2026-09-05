using System.Linq;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>What the three multi-sample tests refuse.</summary>
public sealed class GroupTestEdgeTests
{
    [Fact]
    public void Anova_refuses_fewer_than_two_groups()
    {
        Assert.Throws<ArgumentException>(() => OneWayAnova.Test([1.0, 2.0, 3.0]));
    }

    [Fact]
    public void Anova_refuses_an_empty_group()
    {
        Assert.Throws<ArgumentException>(
            () => OneWayAnova.Test([1.0, 2.0], [], [3.0, 4.0]));
    }

    [Fact]
    public void Anova_of_identical_groups_has_no_between_group_variance()
    {
        TestResult result = OneWayAnova.Test([1.0, 2.0, 3.0], [1.0, 2.0, 3.0]);

        Assert.Equal(0.0, result.Statistic, 1e-12);
        Assert.Equal(1.0, result.PValue, 1e-12);
    }

    // The other half of the division the brief warns about: not "identical
    // groups make the numerator zero" (the fact above), but "identical
    // *within* every group makes the denominator zero". No guard throws here
    // -- between / dfBetween divided by 0.0 / dfWithin is ordinary IEEE
    // +Infinity, and Beta.FisherSf(+Infinity, ...) is already exact there,
    // returning 0.0. Verified by probe before writing this fact
    // (task-8-report.md): the statistic really does come back +Infinity, not
    // NaN, so there is nothing here for a guard to catch.
    [Fact]
    public void Anova_of_two_zero_variance_groups_is_certain()
    {
        TestResult result = OneWayAnova.Test([5.0, 5.0], [7.0, 7.0]);

        Assert.True(double.IsPositiveInfinity(result.Statistic));
        Assert.Equal(0.0, result.PValue);
    }

    // Fix-round-1, finding 6: the fully degenerate quadrant of the same
    // division -- every group internally constant AND every group the same
    // constant, so both between and within are exactly 0.0. 0.0 / 0.0 is
    // NaN, not +Infinity, and there is no guard against it: OneWayAnova.Test's
    // own remarks say why (this matches scipy.stats.f_oneway on the same
    // input -- verified directly, task-8-report.md). Kruskal-Wallis on the
    // identical group shape instead throws (the fact above): the two are
    // deliberately different, not an inconsistency to fix.
    [Fact]
    public void Anova_of_two_identical_zero_variance_groups_is_nan()
    {
        TestResult result = OneWayAnova.Test([5.0, 5.0], [5.0, 5.0]);

        Assert.True(double.IsNaN(result.Statistic));
        Assert.True(double.IsNaN(result.PValue));
    }

    [Fact]
    public void Kruskal_refuses_fewer_than_two_groups_and_an_empty_group()
    {
        Assert.Throws<ArgumentException>(() => KruskalWallis.Test([1.0, 2.0, 3.0]));
        Assert.Throws<ArgumentException>(
            () => KruskalWallis.Test([1.0, 2.0], [], [3.0, 4.0]));
    }

    // Not in the brief's own listing of this file, but the guard it pins is
    // required reading (task-8-brief.md's "prove the guard is reachable"):
    // deleting the tieCorrection <= 0.0 check in KruskalWallis.Test and
    // rerunning this fact still throws, but the wrong thing --
    // ArgumentOutOfRangeException from Gamma.Validate ("the argument must not
    // be negative", actual value NaN), because h ends up 0.0 / 0.0 once the
    // correction that was supposed to guard it is gone (task-8-report.md has
    // the transcript).
    [Fact]
    public void Kruskal_refuses_a_pooled_sample_where_every_value_is_tied()
    {
        Assert.Throws<ArgumentException>(
            () => KruskalWallis.Test([5.0, 5.0], [5.0, 5.0]));
    }

    [Fact]
    public void Ks_refuses_an_empty_sample()
    {
        Assert.Throws<ArgumentException>(() => KolmogorovSmirnov.TwoSample([], [1.0, 2.0]));
    }

    [Fact]
    public void Ks_of_a_sample_against_itself_is_a_distance_of_zero()
    {
        double[] sample = [1.0, 2.0, 3.0, 4.0];

        KsResult result = KolmogorovSmirnov.TwoSample(sample, sample);

        Assert.Equal(0.0, result.Statistic);
        Assert.Equal(1.0, result.PValue);
    }

    // Fix-round-2, finding 2, exactly the input the review named: n1 = 100,
    // n2 = 150 built so the statistic is exactly 7/30 (StatisticLocation 30,
    // sign +1, verified against scipy's own ks_2samp construction). At the
    // effective sample size this produces (60), fix-round-2's own finding 1
    // fix routes this case through DurbinCdf rather than SmirnovSf, so it no
    // longer exercises the clamp directly -- see the fact below for one that
    // still does. Kept because the review asked for it by name and it is
    // still the right regression to hold: a finite p-value matching scipy,
    // not the NaN this shape once produced.
    [Fact]
    public void Ks_asymptotic_p_value_is_finite_at_seven_thirtieths()
    {
        double[] a = [.. Enumerable.Range(1, 30).Select(i => (double)i), .. Enumerable.Repeat(1000.0, 70)];
        double[] b = [.. Enumerable.Repeat(0.0, 10), .. Enumerable.Repeat(1000.0, 140)];

        KsResult result = KolmogorovSmirnov.TwoSample(a, b, Alternative.TwoSided, ExactMethod.Asymptotic);

        Assert.False(double.IsNaN(result.PValue), "PValue is NaN.");
        Assert.Equal(7.0 / 30.0, result.Statistic, 1e-12);
        Assert.Equal(0.002347089884095932, result.PValue, 1e-9);
    }

    // Fix-round-2, finding 2: the SmirnovSf reachability this package's
    // contract actually needs pinned, at an input still routed through
    // SmirnovSf after finding 1's dispatch fix -- n1 = n2 = 400 (effective
    // size 200, past LargeSampleBranch) built so the statistic is exactly
    // 0.32 (StatisticLocation 128, sign +1, verified against scipy). At the
    // final term of the survival-formula sum, the quantity that should be
    // exactly zero there lands a few ULPs negative; before the clamp this
    // fact pins, a logarithm of that produced NaN, and every clamp after it
    // in the call chain propagated that NaN rather than catching it
    // (delete-and-confirm below).
    [Fact]
    public void Ks_asymptotic_p_value_is_finite_at_the_smirnov_boundary()
    {
        double[] a = [.. Enumerable.Range(1, 128).Select(i => (double)i), .. Enumerable.Repeat(1000.0, 272)];
        double[] b = [.. Enumerable.Repeat(1000.0, 400)];

        KsResult result = KolmogorovSmirnov.TwoSample(a, b, Alternative.TwoSided, ExactMethod.Asymptotic);

        Assert.False(double.IsNaN(result.PValue), "PValue is NaN.");
        Assert.Equal(0.32, result.Statistic, 1e-12);
        double relative = Math.Abs(result.PValue - 1.0212075681507937e-18) / 1.0212075681507937e-18;
        Assert.True(relative <= 1e-6, $"PValue = {result.PValue}.");
    }
}
