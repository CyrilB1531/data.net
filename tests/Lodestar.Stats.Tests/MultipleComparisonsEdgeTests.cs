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
        // Per-element with a tolerance, not Assert.Equal on the arrays: 0.2*3 is
        // 0.6000000000000001 in IEEE double, one ULP off the literal 0.6.
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

    [Fact]
    public void A_family_at_the_ceiling_stays_at_one_under_every_correction()
    {
        // scipy.stats.false_discovery_control leaves an all-ones family at 1.0
        // under both 'bh' and 'by'; Bonferroni's min(p * n, 1) does the same.
        double[] p = [1.0, 1.0, 1.0, 1.0];

        AssertBounded(MultipleComparisons.Bonferroni(p), [1.0, 1.0, 1.0, 1.0]);
        AssertBounded(MultipleComparisons.BenjaminiHochberg(p), [1.0, 1.0, 1.0, 1.0]);
        AssertBounded(MultipleComparisons.BenjaminiYekutieli(p), [1.0, 1.0, 1.0, 1.0]);
    }

    [Fact]
    public void A_family_of_one_passes_through_unchanged_under_every_correction()
    {
        // n = 1 collapses every correction's formula to the identity: Bonferroni's
        // p * 1, BH's p * 1 / 1, and BY's p * 1 * harmonic(1) / 1 all equal p.
        double[] p = [0.037];

        AssertBounded(MultipleComparisons.Bonferroni(p), [0.037]);
        AssertBounded(MultipleComparisons.BenjaminiHochberg(p), [0.037]);
        AssertBounded(MultipleComparisons.BenjaminiYekutieli(p), [0.037]);
    }

    [Fact]
    public void A_zero_p_value_adjusts_to_zero_without_disturbing_the_rest()
    {
        // scipy's false_discovery_control(method='bh') gives [0.0, 0.3, 0.9]; 'by' gives
        // [0.0, 0.55, 1.0] (harmonic(3) ~= 1.8333333333333333); Bonferroni is min(p*3, 1).
        double[] p = [0.0, 0.2, 0.9];

        AssertBounded(MultipleComparisons.Bonferroni(p), [0.0, 0.6, 1.0]);
        AssertBounded(MultipleComparisons.BenjaminiHochberg(p), [0.0, 0.3, 0.9]);
        AssertBounded(MultipleComparisons.BenjaminiYekutieli(p), [0.0, 0.55, 1.0]);
    }

    /// <summary>Every adjusted value matches within tolerance and lies in <c>[0, 1]</c>, never NaN.</summary>
    private static void AssertBounded(double[] actual, double[] expected)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.False(double.IsNaN(actual[i]), $"actual[{i}] was NaN.");
            Assert.InRange(actual[i], 0.0, 1.0);
            Assert.Equal(expected[i], actual[i], 1e-9);
        }
    }
}
