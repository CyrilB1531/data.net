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
    public void A_NaN_in_a_group_propagates_rather_than_being_dropped()
    {
        // The spec's ruling: nan_policy is not a parameter here. Measured against
        // scipy 1.18.0: kruskal([1, 2, nan], [3, 4, 5]) returns (nan, nan).
        TestResult result = KruskalWallis.Test([1.0, 2.0, double.NaN], [3.0, 4.0, 5.0]);

        Assert.True(double.IsNaN(result.Statistic));
        Assert.True(double.IsNaN(result.PValue));
    }

    [Fact]
    public void Ks_refuses_an_empty_sample()
    {
        Assert.Throws<ArgumentException>(() => KolmogorovSmirnov.TwoSample([], [1.0, 2.0]));
    }

    [Fact]
    public void A_NaN_in_either_sample_propagates_rather_than_hanging()
    {
        // Walk spun forever here before this guard existed (sorted[index] == NaN is
        // always false). Measured against scipy 1.18.0: (nan, nan), location nan too.
        KsResult result = KolmogorovSmirnov.TwoSample([1.0, 2.0, double.NaN], [3.0, 4.0, 5.0]);

        Assert.True(double.IsNaN(result.Statistic));
        Assert.True(double.IsNaN(result.PValue));
        Assert.True(double.IsNaN(result.StatisticLocation));
    }

    [Fact]
    public void Ks_refuses_an_exact_request_whose_table_is_too_large()
    {
        // n * m = 1,440,000, past the 1,000,000 bound: ExactPValue would allocate a
        // fresh double[m+1] row on each of n+1 iterations.
        double[] big = new double[1200];
        double[] alsoBig = new double[1200];
        for (int i = 0; i < 1200; i++)
        {
            big[i] = i;
            alsoBig[i] = i + 0.5;
        }

        Assert.Throws<ArgumentOutOfRangeException>(
            () => KolmogorovSmirnov.TwoSample(big, alsoBig, method: ExactMethod.Exact));
    }

    [Fact]
    public void Ks_does_not_refuse_an_exact_request_at_the_bound()
    {
        // Exactly 1,000,000 (n = m = 1,000): the bound is inclusive, so this must
        // still answer rather than refuse.
        double[] a = new double[1000];
        double[] b = new double[1000];
        for (int i = 0; i < 1000; i++)
        {
            a[i] = i;
            b[i] = i + 0.5;
        }

        KsResult result = KolmogorovSmirnov.TwoSample(a, b, method: ExactMethod.Exact);
        Assert.True(result.PValue is >= 0.0 and <= 1.0);
    }

    [Fact]
    public void Ks_auto_returns_without_throwing_past_the_exact_bound()
    {
        // long-comment: this test cannot fail the way the one above it can, and
        // that needs to be on the record rather than discovered later.
        // n * m = 1,440,000, past the 1,000,000 exact-table bound but also past
        // Auto's own 10,000 threshold: Auto's own rule already routes this to
        // asymptotic before the size guard is ever consulted, so removing the
        // guard entirely does not make this fail (verified). It only proves Auto
        // keeps answering rather than throwing once a second reason to fall back
        // is layered on top of the first.
        double[] big = new double[1200];
        double[] alsoBig = new double[1200];
        for (int i = 0; i < 1200; i++)
        {
            big[i] = i;
            alsoBig[i] = i + 0.5;
        }

        KsResult auto = KolmogorovSmirnov.TwoSample(big, alsoBig, method: ExactMethod.Auto);
        KsResult asymptotic = KolmogorovSmirnov.TwoSample(big, alsoBig, method: ExactMethod.Asymptotic);

        Assert.Equal(asymptotic.PValue, auto.PValue, 1e-15);
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
