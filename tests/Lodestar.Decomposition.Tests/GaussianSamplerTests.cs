using Lodestar.Decomposition.Internal;
using Xunit;

namespace Lodestar.Decomposition.Tests;

/// <summary>
/// The package's own generator. It is deliberately not numpy's: a seed reproduces a run of
/// Lodestar, never scikit-learn's matrix, which is why the corpus freezes Ω as an input.
/// </summary>
public sealed class GaussianSamplerTests
{
    [Fact]
    public void The_same_seed_draws_the_same_block()
    {
        double[] first = new GaussianSampler(20260901).Normal(6, 4);
        double[] second = new GaussianSampler(20260901).Normal(6, 4);

        Assert.Equal(first, second);
    }

    [Fact]
    public void A_different_seed_draws_a_different_block()
    {
        double[] first = new GaussianSampler(1).Normal(6, 4);
        double[] second = new GaussianSampler(2).Normal(6, 4);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void The_block_has_the_shape_it_was_asked_for()
    {
        Assert.Equal(24, new GaussianSampler(7).Normal(6, 4).Length);
    }

    [Fact]
    public void The_draws_are_standard_normal_to_two_decimals()
    {
        double[] draws = new GaussianSampler(20260901).Normal(20_000, 5);

        double mean = 0;
        foreach (double draw in draws)
        {
            mean += draw;
        }
        mean /= draws.Length;

        double variance = 0;
        foreach (double draw in draws)
        {
            variance += (draw - mean) * (draw - mean);
        }
        variance /= draws.Length;

        Assert.Equal(0.0, mean, 2);
        Assert.Equal(1.0, variance, 2);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(4, 0)]
    [InlineData(-1, 4)]
    public void A_block_with_no_elements_is_refused(int rows, int columns)
    {
        GaussianSampler sampler = new(7);

        Assert.Throws<ArgumentOutOfRangeException>(() => sampler.Normal(rows, columns));
    }
}
