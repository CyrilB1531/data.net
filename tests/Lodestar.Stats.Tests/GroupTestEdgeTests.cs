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
}
