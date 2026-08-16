using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

public sealed class RegressionCorpusTests
{
    [Theory]
    [MemberData(nameof(RegressionCorpus.Indices), MemberType = typeof(RegressionCorpus))]
    public void Every_case_is_rectangular_and_carries_the_metrics_every_task_replays(int index)
    {
        JsonElement c = RegressionCorpus.Cases[index];
        double[] yTrue = RegressionCorpus.Doubles(c, "y_true");
        double[] yPred = RegressionCorpus.Doubles(c, "y_pred");
        int k = RegressionCorpus.OutputCount(c);

        Assert.Equal(yTrue.Length, yPred.Length);
        Assert.Equal(0, yTrue.Length % k);
        foreach (string key in new[] { "mse|uniform", "mae|uniform", "r2|uniform|force_finite" })
        {
            Assert.True(RegressionCorpus.Has(c, key), $"{RegressionCorpus.Describe(c)} lacks {key}");
        }
    }

    [Fact]
    public void The_corpus_carries_a_non_finite_value_and_the_reader_decodes_it()
    {
        // r2 on one sample is nan under either force_finite; the corpus's only
        // undefined case, and the only fixture that exercises zeroDivision here.
        JsonElement single = RegressionCorpus.Cases
            .First(c => c.GetProperty("fixture").GetString() == "single_sample");

        Assert.True(double.IsNaN(RegressionCorpus.Value(single, "r2|uniform|force_finite")));
        Assert.True(double.IsNaN(RegressionCorpus.Value(single, "r2|uniform|raw_infinity")));
    }

    [Fact]
    public void The_comparison_rule_scales_with_the_value()
    {
        // 4.5e15 is mape's clamped answer for a zero target; an absolute 1e-9
        // there would refuse every implementation, including a correct one.
        RegressionCorpus.AssertClose(4503599627370496.0, 4503599627370496.5, "large");
        RegressionCorpus.AssertClose(0.0, 5e-10, "small");
        Assert.Throws<Xunit.Sdk.TrueException>(
            () => RegressionCorpus.AssertClose(0.0, 5e-8, "small but wrong"));
    }
}
