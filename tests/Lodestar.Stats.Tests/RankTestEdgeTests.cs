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
}
