using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class NormalizationTests
{
    [Fact]
    public void The_loader_decodes_a_non_finite_oracle_value()
    {
        // The corpus is strict JSON, so NaN travels as a string. Nothing else in
        // this repository's oracles has ever needed that, which is why the
        // decoding lives in one place instead of at each call site.
        using JsonDocument doc = JsonDocument.Parse("""{"a": "NaN", "b": 0.5, "c": "-Infinity"}""");
        JsonElement root = doc.RootElement;

        Assert.True(double.IsNaN(OracleLoader.Number(root.GetProperty("a"))));
        Assert.Equal(0.5, OracleLoader.Number(root.GetProperty("b")));
        Assert.True(double.IsNegativeInfinity(OracleLoader.Number(root.GetProperty("c"))));
    }

    [Theory]
    [InlineData("true", Normalization.True)]
    [InlineData("pred", Normalization.Pred)]
    [InlineData("all", Normalization.All)]
    public void Matches_sklearn_confusion_matrix_normalize(string key, Normalization normalization)
    {
        foreach (JsonElement c in MetricsCorpus.Cases)
        {
            if (!c.TryGetProperty("normalized", out JsonElement expectedAll))
            {
                continue;
            }

            ConfusionMatrix cm = MetricsCorpus.Matrix(c);
            double[,] actual = cm.ToArray(normalization);
            JsonElement expected = expectedAll.GetProperty(key);

            int k = cm.Labels.Count;
            for (int row = 0; row < k; row++)
            {
                for (int col = 0; col < k; col++)
                {
                    double want = OracleLoader.Number(expected[row][col]);
                    Assert.True(Math.Abs(want - actual[row, col]) < MetricsCorpus.Tolerance,
                        $"{MetricsCorpus.Describe(c)} {key}[{row},{col}]: expected {want}, got {actual[row, col]}");
                }
            }
        }
    }

    [Fact]
    public void A_row_that_counted_nothing_normalises_to_zero_not_NaN()
    {
        // scikit-learn runs nan_to_num over the result, so an absent class gives a
        // row of zeros. Divide and forget that, and every invariant below breaks
        // the moment a caller passes an explicit label that does not occur.
        int[] yTrue = [0, 0, 1, 1];
        int[] yPred = [0, 0, 1, 1];
        ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred, labels: [0, 1, 2]);

        double[,] normalized = cm.ToArray(Normalization.True);

        Assert.Equal(0.0, normalized[2, 0]);
        Assert.Equal(0.0, normalized[2, 1]);
        Assert.Equal(0.0, normalized[2, 2]);
    }

    [Fact]
    public void No_argument_ToArray_is_the_unnormalised_one()
    {
        int[] yTrue = [0, 0, 1, 2];
        int[] yPred = [0, 1, 1, 2];
        ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred);

        double[,] bare = cm.ToArray();
        double[,] none = cm.ToArray(Normalization.None);

        Assert.Equal(bare, none);
    }

    [Fact]
    public void An_out_of_range_normalization_is_refused()
    {
        int[] yTrue = [0, 1];
        int[] yPred = [0, 1];
        ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred);

        Assert.Throws<ArgumentOutOfRangeException>(() => cm.ToArray((Normalization)99));
    }
}
