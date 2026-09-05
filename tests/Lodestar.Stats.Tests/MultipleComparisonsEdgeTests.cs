using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>The three corrections' shared contract.</summary>
public sealed class MultipleComparisonsEdgeTests
{
    [Fact]
    public void All_three_refuse_an_empty_family()
    {
        Assert.Throws<ArgumentException>(() => MultipleComparisons.Bonferroni([]));
        Assert.Throws<ArgumentException>(() => MultipleComparisons.BenjaminiHochberg([]));
        Assert.Throws<ArgumentException>(() => MultipleComparisons.BenjaminiYekutieli([]));
    }

    [Fact]
    public void All_three_refuse_a_p_value_outside_the_unit_interval()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MultipleComparisons.Bonferroni([0.5, 1.5]));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MultipleComparisons.BenjaminiHochberg([-0.1, 0.5]));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MultipleComparisons.BenjaminiYekutieli([double.NaN]));
    }

    [Fact]
    public void All_three_return_the_adjusted_values_in_the_input_order()
    {
        double[] adjusted = MultipleComparisons.BenjaminiHochberg([0.3, 0.001, 0.02]);

        // The smallest input is at index 1, so the smallest adjusted value is too.
        Assert.True(adjusted[1] < adjusted[2]);
        Assert.True(adjusted[2] < adjusted[0]);
    }

    [Fact]
    public void Bonferroni_multiplies_by_the_family_size_and_clamps_at_one()
    {
        // Per-element with a tolerance, not Assert.Equal on the arrays: 0.2 * 3
        // is 0.6000000000000001 in IEEE double, one ULP off the literal 0.6, so
        // an exact array comparison would fail on a correct implementation.
        double[] expected = [0.12, 0.6, 1.0];
        double[] actual = MultipleComparisons.Bonferroni([0.04, 0.2, 0.9]);

        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], 1e-12);
        }
    }

    [Fact]
    public void Yekutieli_is_never_smaller_than_hochberg()
    {
        double[] p = [0.001, 0.008, 0.039, 0.041, 0.042];
        double[] bh = MultipleComparisons.BenjaminiHochberg(p);
        double[] by = MultipleComparisons.BenjaminiYekutieli(p);

        for (int i = 0; i < p.Length; i++)
        {
            Assert.True(by[i] >= bh[i] - 1e-15, $"by[{i}] = {by[i]} fell below bh[{i}] = {bh[i]}.");
        }
    }
}
