using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>What the two rank tests refuse, and where Auto changes branch.</summary>
public sealed class RankTestEdgeTests
{
    [Fact]
    public void MannWhitney_refuses_an_empty_sample()
    {
        Assert.Throws<ArgumentException>(() => MannWhitney.Test([], [1.0, 2.0]));
        Assert.Throws<ArgumentException>(() => MannWhitney.Test([1.0, 2.0], []));
    }

    [Fact]
    public void MannWhitney_auto_takes_the_exact_branch_only_when_small_and_untied()
    {
        double[] small = [1.0, 4.0, 7.0];
        double[] other = [2.0, 3.0, 8.0];

        Assert.Equal(
            MannWhitney.Test(small, other, method: ExactMethod.Exact).PValue,
            MannWhitney.Test(small, other, method: ExactMethod.Auto).PValue,
            1e-15);

        double[] tied = [1.0, 2.0, 2.0];
        double[] alsoTied = [2.0, 3.0, 3.0];

        Assert.Equal(
            MannWhitney.Test(tied, alsoTied, method: ExactMethod.Asymptotic).PValue,
            MannWhitney.Test(tied, alsoTied, method: ExactMethod.Auto).PValue,
            1e-15);
    }

    [Fact]
    public void MannWhitney_exact_still_answers_on_tied_data()
    {
        // Measured against scipy 1.18.0: mannwhitneyu(..., method="exact") on
        // tied samples returns a number rather than raising, so this does too.
        TestResult result = MannWhitney.Test(
            [1.0, 2.0, 2.0], [2.0, 3.0, 3.0], method: ExactMethod.Exact);

        Assert.True(result.PValue is > 0.0 and <= 1.0);
    }

    [Fact]
    public void MannWhitney_refuses_an_exact_request_whose_table_is_too_large()
    {
        // n * m = 90,000, well past the 20,000 bound: MannWhitneyCounts would
        // allocate an (m+1) x (n*m+1) table on each of n outer iterations.
        double[] big = new double[300];
        double[] alsoBig = new double[300];
        for (int i = 0; i < 300; i++)
        {
            big[i] = i;
            alsoBig[i] = i + 0.5;
        }

        Assert.Throws<ArgumentOutOfRangeException>(
            () => MannWhitney.Test(big, alsoBig, method: ExactMethod.Exact));
    }

    [Fact]
    public void MannWhitney_does_not_refuse_an_exact_request_below_the_bound()
    {
        // n * m = 2,500, comfortably under the 20,000 bound.
        double[] a = new double[50];
        double[] b = new double[50];
        for (int i = 0; i < 50; i++)
        {
            a[i] = i;
            b[i] = i + 0.5;
        }

        TestResult result = MannWhitney.Test(a, b, method: ExactMethod.Exact);
        Assert.True(result.PValue is > 0.0 and <= 1.0);
    }

    [Fact]
    public void MannWhitney_auto_falls_back_to_asymptotic_past_the_bound_on_a_skewed_shape()
    {
        // long-comment: an earlier version of this test used i*0.1 with no
        // offset and passed for the wrong reason -- the fix is the +0.05
        // below, and why it matters needs to stay visible next to it.
        // n=8 alone satisfies Auto's exact-branch size rule (the smaller
        // sample only), but n*m = 24,000 exceeds MaxExactProduct: Auto must
        // answer from the asymptotic branch rather than building a table
        // sized (m+1) x (n*m+1), or throwing the way an explicit Exact would.
        // The +0.05 offset is load-bearing: without it every large[i] at
        // i=10,40,70,... lands exactly on a small[] value (10*0.1 == 1.0),
        // Ranks.HasTies(pooled) is true, and wantsExact is already false
        // through the ties clause -- the tableTooLarge guard is never
        // consulted and this test cannot fail regardless of its state
        // (verified by deletion -- see task-6-report.md, fix round 2).
        double[] small = [1.0, 4.0, 7.0, 9.0, 11.0, 13.0, 15.0, 17.0];
        double[] large = new double[3000];
        for (int i = 0; i < large.Length; i++)
        {
            large[i] = (i * 0.1) + 0.05;
        }

        TestResult auto = MannWhitney.Test(small, large, method: ExactMethod.Auto);
        TestResult asymptotic = MannWhitney.Test(small, large, method: ExactMethod.Asymptotic);

        Assert.Equal(asymptotic.PValue, auto.PValue, 1e-15);
    }

    [Fact]
    public void MannWhitney_exact_matches_scipy_on_tied_data()
    {
        // Measured with scipy 1.18.0:
        // mannwhitneyu([1,2,2,3], [2,3,4,4], method="exact", alternative=...).
        double[] a = [1.0, 2.0, 2.0, 3.0];
        double[] b = [2.0, 3.0, 4.0, 4.0];

        TestResult twoSided = MannWhitney.Test(a, b, Alternative.TwoSided, method: ExactMethod.Exact);
        TestResult less = MannWhitney.Test(a, b, Alternative.Less, method: ExactMethod.Exact);
        TestResult greater = MannWhitney.Test(a, b, Alternative.Greater, method: ExactMethod.Exact);

        StatsOracleAsserts.Statistic(2.5, twoSided.Statistic, "two-sided");
        StatsOracleAsserts.PValue(0.2, twoSided.PValue, "two-sided");
        StatsOracleAsserts.Statistic(2.5, less.Statistic, "less");
        StatsOracleAsserts.PValue(0.1, less.PValue, "less");
        StatsOracleAsserts.Statistic(2.5, greater.Statistic, "greater");
        StatsOracleAsserts.PValue(0.9714285714285714, greater.PValue, "greater");
    }

    [Fact]
    public void Wilcoxon_refuses_samples_of_different_length()
    {
        Assert.Throws<ArgumentException>(() => Wilcoxon.Paired([1.0, 2.0, 3.0], [1.0, 2.0]));
    }

    [Fact]
    public void Wilcoxon_refuses_an_empty_sample()
    {
        Assert.Throws<ArgumentException>(() => Wilcoxon.OneSample([]));
    }

    [Fact]
    public void Wilcox_drops_the_zero_pairs_and_pratt_keeps_them()
    {
        double[] x = [1.0, 3.0, 5.0, 7.0, 9.0, 11.0];
        double[] y = [1.0, 3.5, 5.0, 8.0, 8.5, 13.0];

        double wilcox = Wilcoxon.Paired(x, y, ZeroMethod.Wilcox).Statistic;
        double pratt = Wilcoxon.Paired(x, y, ZeroMethod.Pratt).Statistic;

        // Two of the six differences are zero, so the two rules rank different
        // numbers of values and cannot agree.
        Assert.NotEqual(wilcox, pratt);
    }

    [Fact]
    public void Wilcoxon_of_all_zero_differences_is_a_statistic_of_zero_and_a_p_value_of_one()
    {
        TestResult result = Wilcoxon.OneSample([0.0, 0.0, 0.0]);

        Assert.Equal(0.0, result.Statistic);
        Assert.Equal(1.0, result.PValue);
    }

    // Measured with scipy 1.18.0: wilcoxon([1,1,-1,2,-2,0], zero_method=...,
    // correction=False, alternative=..., method="exact"). Ties (two 1's,
    // two 2's) and a zero, so all three ZeroMethod rules disagree.
    private static readonly double[] TiedWithZero = [1.0, 1.0, -1.0, 2.0, -2.0, 0.0];

    [Fact]
    public void Wilcoxon_exact_matches_scipy_on_tied_data_wilcox()
    {
        AssertExactMatchesScipy(ZeroMethod.Wilcox, Alternative.TwoSided, 6.5, 1.0);
        AssertExactMatchesScipy(ZeroMethod.Wilcox, Alternative.Less, 8.5, 0.6875);
        AssertExactMatchesScipy(ZeroMethod.Wilcox, Alternative.Greater, 8.5, 0.5);
    }

    [Fact]
    public void Wilcoxon_exact_matches_scipy_on_tied_data_pratt()
    {
        AssertExactMatchesScipy(ZeroMethod.Pratt, Alternative.TwoSided, 8.5, 1.0);
        AssertExactMatchesScipy(ZeroMethod.Pratt, Alternative.Less, 11.5, 0.65625);
        AssertExactMatchesScipy(ZeroMethod.Pratt, Alternative.Greater, 11.5, 0.5);
    }

    [Fact]
    public void Wilcoxon_exact_matches_scipy_on_tied_data_zsplit()
    {
        AssertExactMatchesScipy(ZeroMethod.ZSplit, Alternative.TwoSided, 9.0, 0.84375);
        AssertExactMatchesScipy(ZeroMethod.ZSplit, Alternative.Less, 12.0, 0.65625);
        AssertExactMatchesScipy(ZeroMethod.ZSplit, Alternative.Greater, 12.0, 0.421875);
    }

    private static void AssertExactMatchesScipy(
        ZeroMethod zeroMethod, Alternative alternative, double statistic, double pValue)
    {
        TestResult result = Wilcoxon.OneSample(
            TiedWithZero, zeroMethod, alternative, Continuity.None, ExactMethod.Exact);

        string name = $"{zeroMethod}/{alternative}";
        StatsOracleAsserts.Statistic(statistic, result.Statistic, name);
        StatsOracleAsserts.PValue(pValue, result.PValue, name);
    }

    [Fact]
    public void Wilcoxon_refuses_an_exact_request_whose_table_is_too_large()
    {
        // 600 ranked values, past the 500 bound the refusal exists to keep
        // Exact well clear of 2^n's overflow to Infinity at n = 1024.
        double[] big = UntiedDifferences(600);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Wilcoxon.OneSample(big, method: ExactMethod.Exact));
    }

    [Fact]
    public void Wilcoxon_does_not_refuse_an_exact_request_at_the_bound()
    {
        // Exactly 500 ranked values: the bound is inclusive, so this must
        // still answer rather than refuse.
        double[] atBound = UntiedDifferences(500);

        TestResult result = Wilcoxon.OneSample(atBound, method: ExactMethod.Exact);
        Assert.True(result.PValue is > 0.0 and <= 1.0);
    }

    [Fact]
    public void Wilcoxon_auto_returns_below_the_bound()
    {
        double[] small = UntiedDifferences(10);

        TestResult auto = Wilcoxon.OneSample(small, method: ExactMethod.Auto);
        TestResult exact = Wilcoxon.OneSample(small, method: ExactMethod.Exact);

        Assert.Equal(exact.PValue, auto.PValue, 1e-15);
    }

    [Fact]
    public void Wilcoxon_auto_returns_without_throwing_above_the_bound()
    {
        // long-comment: this test cannot fail the way the two above it can,
        // and that needs to be on the record rather than discovered later.
        // 600 ranked values: past both AutoAsymptoticThreshold (50) and the
        // 500 exact-table bound. Regression coverage, not a bound-guard
        // test -- Auto's own fifty-value rule already routes this to
        // Asymptotic before the size guard is ever consulted, so removing
        // the guard entirely does not make this fail (verified). It only
        // proves Auto keeps answering rather than throwing once a second
        // reason to fall back is layered on top of the first.
        double[] big = UntiedDifferences(600);

        TestResult auto = Wilcoxon.OneSample(big, method: ExactMethod.Auto);
        TestResult asymptotic = Wilcoxon.OneSample(big, method: ExactMethod.Asymptotic);

        Assert.Equal(asymptotic.PValue, auto.PValue, 1e-15);
    }

    // Distinct, nonzero, unit-spaced differences: no ties, no zeros, so every
    // ZeroMethod and every branch treats them identically.
    private static double[] UntiedDifferences(int count)
    {
        double[] differences = new double[count];
        for (int i = 0; i < count; i++)
        {
            differences[i] = i + 1.0;
        }

        return differences;
    }
}
