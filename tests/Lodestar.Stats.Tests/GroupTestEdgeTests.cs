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

    // Identical *within* every group (not the fact above's between=0): within/dfWithin = 0
    // gives ordinary IEEE +Infinity, and FisherSf(+Infinity, ...) is exact there, returning 0.0.
    [Fact]
    public void Anova_of_two_zero_variance_groups_is_certain()
    {
        TestResult result = OneWayAnova.Test([5.0, 5.0], [7.0, 7.0]);

        Assert.True(double.IsPositiveInfinity(result.Statistic));
        Assert.Equal(0.0, result.PValue);
    }

    // Fully degenerate: both between and within are exactly 0.0, so 0.0/0.0 is NaN, not
    // +Infinity -- matches scipy's own f_oneway; Kruskal-Wallis on the same shape throws instead.
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

    // Deleting KruskalWallis's tieCorrection<=0 guard still throws, but the wrong thing --
    // Gamma.Validate's ArgumentOutOfRangeException on NaN, since h becomes 0.0/0.0 (task-8-report.md).
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

    // Fix-round-2's own named input (n1=100, n2=150, statistic 7/30) -- now routed through
    // DurbinCdf rather than SmirnovSf, kept as the regression the review asked for by name.
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

    // The SmirnovSf reachability this package's contract needs pinned (n1=n2=400, statistic
    // 0.32, past LargeSampleBranch): before the clamp, a few-ULP-negative term's log gave NaN.
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
