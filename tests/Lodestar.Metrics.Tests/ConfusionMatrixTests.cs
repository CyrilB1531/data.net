using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

public sealed class ConfusionMatrixTests
{
    [Theory]
    [MemberData(nameof(MetricsCorpus.Indices), MemberType = typeof(MetricsCorpus))]
    public void Matches_sklearn_confusion_matrix(int index)
    {
        JsonElement c = MetricsCorpus.Cases[index];
        string what = MetricsCorpus.Describe(c);

        ConfusionMatrix cm = ConfusionMatrix.Compute(
            MetricsCorpus.Ints(c, "y_true"),
            MetricsCorpus.Ints(c, "y_pred"),
            MetricsCorpus.OptionalInts(c, "labels"),
            MetricsCorpus.OptionalDoubles(c, "sample_weight"));

        Assert.Equal(MetricsCorpus.Ints(c, "expected_labels"), cm.Labels);

        JsonElement expected = c.GetProperty("confusion_matrix");
        int k = cm.Labels.Count;
        Assert.Equal(k, expected.GetArrayLength());
        for (int row = 0; row < k; row++)
        {
            JsonElement expectedRow = expected[row];
            for (int col = 0; col < k; col++)
            {
                Assert.True(
                    Math.Abs(expectedRow[col].GetDouble() - cm[row, col]) < MetricsCorpus.Tolerance,
                    $"{what}: cell [{row},{col}] expected {expectedRow[col].GetDouble()}, got {cm[row, col]}");
            }
        }
    }

    [Fact]
    public void Rejects_mismatched_lengths()
    {
        int[] yTrue = [0, 1, 0];
        int[] yPred = [0, 1];
        Assert.Throws<ArgumentException>(() => ConfusionMatrix.Compute(yTrue, yPred));
    }

    [Fact]
    public void Rejects_empty_input()
    {
        Assert.Throws<ArgumentException>(() => ConfusionMatrix.Compute([], []));
    }

    [Fact]
    public void Rejects_mismatched_sample_weight_length()
    {
        int[] yTrue = [0, 1, 0];
        int[] yPred = [0, 1, 1];
        double[] weight = [1.0, 2.0];
        Assert.Throws<ArgumentException>(
            () => ConfusionMatrix.Compute(yTrue, yPred, default, weight));
    }

    [Fact]
    public void Rejects_duplicate_labels()
    {
        int[] yTrue = [0, 1, 0];
        int[] yPred = [0, 1, 1];
        int[] labels = [0, 1, 0];
        Assert.Throws<ArgumentException>(() => ConfusionMatrix.Compute(yTrue, yPred, labels));
    }

    [Fact]
    public void Rejects_labels_that_appear_in_no_true_value()
    {
        int[] yTrue = [0, 0, 1];
        int[] yPred = [0, 1, 1];
        int[] labels = [7, 8];
        Assert.Throws<ArgumentException>(() => ConfusionMatrix.Compute(yTrue, yPred, labels));
    }

    [Fact]
    public void Keeps_the_caller_s_label_order_unsorted()
    {
        int[] yTrue = [0, 1, 2, 2];
        int[] yPred = [0, 1, 2, 1];
        int[] labels = [2, 0, 1];

        ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred, labels);

        Assert.Equal(labels, cm.Labels);
        Assert.Equal(1.0, cm[0, 0]);   // true 2, predicted 2
        Assert.Equal(1.0, cm[0, 2]);   // true 2, predicted 1
    }

    [Fact]
    public void Handles_label_values_that_are_not_zero_based()
    {
        int[] yTrue = [-1, 42, 5, 42];
        int[] yPred = [-1, 5, 5, 42];

        ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred);

        Assert.Equal([-1, 5, 42], cm.Labels);
        Assert.Equal(4.0, cm.TotalWeight);
    }

    [Fact]
    public void Explicit_wide_range_labels_use_the_binary_search_fallback()
    {
        // 5,000,000 apart exceeds LabelIndex.MaxDirectTableSize (~4.19M), forcing
        // the binary-search path instead of the direct offset table.
        int[] yTrue = [0, 5_000_000, 0, 5_000_000];
        int[] yPred = [0, 5_000_000, 5_000_000, 0];
        int[] labels = [5_000_000, 0];

        ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred, labels);

        Assert.Equal(labels, cm.Labels);
        Assert.Equal(1.0, cm[0, 0]);   // true 5,000,000, predicted 5,000,000
        Assert.Equal(1.0, cm[1, 0]);   // true 0, predicted 5,000,000
    }

    [Fact]
    public void Implicit_union_over_wide_range_values_uses_the_full_sort_fallback()
    {
        // Same threshold, reached through LabelIndex.SortedUnion: a 7,000,001 range
        // forces sort-and-dedup instead of a dense presence array.
        int[] yTrue = [4_000_000, -3_000_000, 0];
        int[] yPred = [0, 4_000_000, -3_000_000];

        ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred);

        Assert.Equal([-3_000_000, 0, 4_000_000], cm.Labels);
    }

    [Fact]
    public void Rejects_duplicate_labels_on_the_binary_search_fallback_path()
    {
        // Wide range again: exercises the sorted adjacent-pair check in the
        // fallback branch, not the dense path's slot check covered above.
        int[] yTrue = [0, 5_000_000];
        int[] yPred = [0, 5_000_000];
        int[] labels = [0, 5_000_000, 0];

        Assert.Throws<ArgumentException>(() => ConfusionMatrix.Compute(yTrue, yPred, labels));
    }
}
