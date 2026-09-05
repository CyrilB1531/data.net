using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>What Shapiro-Wilk refuses, and the range Royston's approximation covers.</summary>
public sealed class ShapiroWilkEdgeTests
{
    [Fact]
    public void Refuses_fewer_than_three_values()
    {
        Assert.Throws<ArgumentException>(() => ShapiroWilk.Test([1.0, 2.0]));
    }

    [Fact]
    public void Refuses_a_sample_with_no_spread()
    {
        // Every value identical: the statistic's denominator is zero, and there
        // is nothing to compare a normal shape against.
        Assert.Throws<ArgumentException>(() => ShapiroWilk.Test([2.0, 2.0, 2.0, 2.0]));
    }

    [Fact]
    public void Refuses_a_sample_above_five_thousand()
    {
        // Royston's normalising transform is fitted to n <= 5000; scipy warns and
        // answers anyway, which is a number nobody should read. Refusing says so.
        double[] tooMany = [.. Enumerable.Range(0, 5001).Select(i => (double)i)];

        Assert.Throws<ArgumentException>(() => ShapiroWilk.Test(tooMany));
    }

    [Fact]
    public void A_normal_looking_sample_is_not_rejected()
    {
        double[] sample =
        [
            -1.62, -1.10, -0.74, -0.47, -0.23, 0.0, 0.23, 0.47, 0.74, 1.10, 1.62,
        ];

        TestResult result = ShapiroWilk.Test(sample);

        Assert.True(result.Statistic is > 0.9 and <= 1.0);
        Assert.True(result.PValue > 0.05);
    }
}
