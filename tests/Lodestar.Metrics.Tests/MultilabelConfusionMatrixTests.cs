using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>The per-label and per-sample stacks against the frozen corpus.</summary>
public sealed class MultilabelConfusionMatrixTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("multilabel_confusion.json");

    private static JsonElement Multilabel => Document.RootElement.GetProperty("multilabel");

    private static JsonElement Multiclass => Document.RootElement.GetProperty("multiclass");

    /// <summary>Each expected 2×2 is flattened as <c>[tn, fp, fn, tp]</c>, its reading order.</summary>
    private static void Same(string what, ConfusionMatrix[] actual, JsonElement expected)
    {
        JsonElement[] rows = [.. expected.EnumerateArray()];
        Assert.Equal(rows.Length, actual.Length);

        for (int i = 0; i < rows.Length; i++)
        {
            double[] want = [.. rows[i].EnumerateArray().Select(v => v.GetDouble())];
            double[,] cells = actual[i].ToArray();
            double[] got = [cells[0, 0], cells[0, 1], cells[1, 0], cells[1, 1]];

            for (int j = 0; j < want.Length; j++)
            {
                Assert.True(
                    Math.Abs(want[j] - got[j]) <= MetricsCorpus.Tolerance,
                    $"{what}[{i}][{j}]: expected {want[j]}, got {got[j]}");
            }
        }
    }

    [Fact]
    public void Matches_sklearn_on_a_label_matrix()
    {
        bool[] yTrue = MetricsCorpus.Bools(Multilabel, "y_true");
        bool[] yPred = MetricsCorpus.Bools(Multilabel, "y_pred");
        double[] weight = MetricsCorpus.Doubles(Multilabel, "sample_weight");
        int labels = Multilabel.GetProperty("label_count").GetInt32();

        Same("per label", MultilabelConfusionMatrix.Compute(yTrue, yPred, labels),
             Multilabel.GetProperty("per_label"));
        Same("samplewise", MultilabelConfusionMatrix.Compute(yTrue, yPred, labels, true),
             Multilabel.GetProperty("samplewise"));
        Same("per label, weighted",
             MultilabelConfusionMatrix.Compute(yTrue, yPred, labels, false, weight),
             Multilabel.GetProperty("per_label_weighted"));
        Same("samplewise, weighted",
             MultilabelConfusionMatrix.Compute(yTrue, yPred, labels, true, weight),
             Multilabel.GetProperty("samplewise_weighted"));
    }

    [Fact]
    public void Matches_sklearn_on_single_label_input()
    {
        int[] yTrue = MetricsCorpus.Ints(Multiclass, "y_true");
        int[] yPred = MetricsCorpus.Ints(Multiclass, "y_pred");
        int[] labels = MetricsCorpus.Ints(Multiclass, "labels");
        double[] weight = MetricsCorpus.Doubles(Multiclass, "sample_weight");

        Same("per class", MultilabelConfusionMatrix.Compute(yTrue, yPred),
             Multiclass.GetProperty("per_class"));
        Same("selected labels", MultilabelConfusionMatrix.Compute(yTrue, yPred, labels),
             Multiclass.GetProperty("selected_labels"));
        Same("per class, weighted",
             MultilabelConfusionMatrix.Compute(yTrue, yPred, default, weight),
             Multiclass.GetProperty("per_class_weighted"));
    }

    // Each entry is a real ConfusionMatrix, so everything that reads one reads these —
    // which is the whole argument for a stack of them over a type of its own.
    [Fact]
    public void Each_entry_is_a_matrix_the_other_metrics_can_read()
    {
        int[] yTrue = [0, 1, 2, 1];
        int[] yPred = [0, 2, 2, 1];

        ConfusionMatrix[] stack = MultilabelConfusionMatrix.Compute(yTrue, yPred);

        // Class 1's own matrix, read as a binary problem, gives class 1's recall.
        double recallOfOne = Recall.Score(stack[1], Averaging.Binary);
        Assert.Equal(Recall.PerClass(yTrue, yPred)[1], recallOfOne, MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Counts_labels_one_way_and_samples_the_other()
    {
        bool[] yTrue = MetricsCorpus.Bools(Multilabel, "y_true");
        bool[] yPred = MetricsCorpus.Bools(Multilabel, "y_pred");
        int labels = Multilabel.GetProperty("label_count").GetInt32();

        Assert.Equal(labels, MultilabelConfusionMatrix.Compute(yTrue, yPred, labels).Length);
        Assert.Equal(yTrue.Length / labels,
                     MultilabelConfusionMatrix.Compute(yTrue, yPred, labels, true).Length);
    }

    [Fact]
    public void Refuses_a_matrix_that_is_not_whole_rows()
    {
        bool[] yTrue = [true, false, true];
        bool[] yPred = [true, false, true];

        Assert.Throws<ArgumentException>(() => MultilabelConfusionMatrix.Compute(yTrue, yPred, 2));
        Assert.Throws<ArgumentException>(() => MultilabelConfusionMatrix.Compute([0, 1], [0]));
    }
}
