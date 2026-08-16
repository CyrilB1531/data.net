using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

public sealed class ClassificationReportTests
{
    [Theory]
    [MemberData(nameof(MetricsCorpus.Indices), MemberType = typeof(MetricsCorpus))]
    public void Rows_carry_the_same_numbers_as_precision_recall_fscore_support(int index)
    {
        JsonElement c = MetricsCorpus.Cases[index];
        string what = MetricsCorpus.Describe(c);
        ConfusionMatrix cm = ConfusionMatrix.Compute(
            MetricsCorpus.Ints(c, "y_true"),
            MetricsCorpus.Ints(c, "y_pred"),
            MetricsCorpus.OptionalInts(c, "labels"),
            MetricsCorpus.OptionalDoubles(c, "sample_weight"));

        ClassificationReport report = ClassificationReport.Compute(cm, TargetNames(c));

        JsonElement perClass = c.GetProperty("per_class").GetProperty("0");
        double[] precision = MetricsCorpus.Doubles(perClass, "precision");
        double[] recall = MetricsCorpus.Doubles(perClass, "recall");
        double[] f1 = MetricsCorpus.Doubles(perClass, "f1");
        double[] support = MetricsCorpus.Doubles(perClass, "support");

        Assert.Equal(precision.Length, report.Classes.Count);
        for (int i = 0; i < precision.Length; i++)
        {
            ClassRow row = report.Classes[i];
            Assert.Equal(cm.Labels[i], row.Label);
            Assert.Equal(precision[i], row.Precision, MetricsCorpus.Tolerance);
            Assert.Equal(recall[i], row.Recall, MetricsCorpus.Tolerance);
            Assert.Equal(f1[i], row.F1, MetricsCorpus.Tolerance);
            Assert.Equal(support[i], row.Support, MetricsCorpus.Tolerance);
        }

        double totalSupport = support.Sum();

        // The full row, not just precision/recall: a MacroAverage wired from the
        // weighted aggregate, or a wrong Support total, has to break this.
        JsonElement macro = c.GetProperty("averaged").GetProperty("macro|0");
        Assert.Equal("macro avg", report.MacroAverage.Name);
        Assert.Equal(macro.GetProperty("precision").GetDouble(), report.MacroAverage.Precision, MetricsCorpus.Tolerance);
        Assert.Equal(macro.GetProperty("recall").GetDouble(), report.MacroAverage.Recall, MetricsCorpus.Tolerance);
        Assert.Equal(macro.GetProperty("f1").GetDouble(), report.MacroAverage.F1, MetricsCorpus.Tolerance);
        Assert.Equal(totalSupport, report.MacroAverage.Support, MetricsCorpus.Tolerance);

        JsonElement weighted = c.GetProperty("averaged").GetProperty("weighted|0");
        Assert.Equal("weighted avg", report.WeightedAverage.Name);
        Assert.Equal(weighted.GetProperty("precision").GetDouble(), report.WeightedAverage.Precision, MetricsCorpus.Tolerance);
        Assert.Equal(weighted.GetProperty("recall").GetDouble(), report.WeightedAverage.Recall, MetricsCorpus.Tolerance);
        Assert.Equal(weighted.GetProperty("f1").GetDouble(), report.WeightedAverage.F1, MetricsCorpus.Tolerance);
        Assert.Equal(totalSupport, report.WeightedAverage.Support, MetricsCorpus.Tolerance);

        Assert.Equal(totalSupport, report.TotalSupport, MetricsCorpus.Tolerance);

        if (c.GetProperty("labels").ValueKind == JsonValueKind.Null)
        {
            // No explicit label set means nothing was dropped: no micro row, and
            // Accuracy equals accuracy_score over the whole input.
            Assert.True(report.MicroAverage is null,
                $"{what}: a micro row appeared without an explicit label set");
            Assert.Equal(c.GetProperty("accuracy").GetDouble(), report.Accuracy, MetricsCorpus.Tolerance);
        }
        else if (report.MicroAverage is not null)
        {
            // An explicit label set that dropped something: the micro row must
            // carry the real micro-averaged numbers, not just be present.
            JsonElement micro = c.GetProperty("averaged").GetProperty("micro|0");
            Assert.Equal("micro avg", report.MicroAverage.Name);
            Assert.Equal(micro.GetProperty("precision").GetDouble(), report.MicroAverage.Precision, MetricsCorpus.Tolerance);
            Assert.Equal(micro.GetProperty("recall").GetDouble(), report.MicroAverage.Recall, MetricsCorpus.Tolerance);
            Assert.Equal(micro.GetProperty("f1").GetDouble(), report.MicroAverage.F1, MetricsCorpus.Tolerance);
            Assert.Equal(totalSupport, report.MicroAverage.Support, MetricsCorpus.Tolerance);
        }
    }

    [Fact]
    public void Names_the_classes_when_target_names_are_supplied()
    {
        int[] yTrue = [0, 1, 1, 0];
        int[] yPred = [0, 1, 0, 0];

        ClassificationReport report = ClassificationReport.Compute(
            yTrue, yPred, targetNames: ["negative", "positive"]);

        Assert.Equal("negative", report.Classes[0].Name);
        Assert.Equal("positive", report.Classes[1].Name);
    }

    [Fact]
    public void Rejects_target_names_of_the_wrong_length()
    {
        int[] yTrue = [0, 1, 2];
        int[] yPred = [0, 1, 2];

        Assert.Throws<ArgumentException>(
            () => ClassificationReport.Compute(yTrue, yPred, targetNames: ["a", "b"]));
    }

    [Fact]
    public void Reports_a_micro_row_instead_of_accuracy_when_labels_exclude_something()
    {
        int[] yTrue = [0, 1, 2, 2, 0];
        int[] yPred = [0, 1, 1, 2, 2];

        ClassificationReport covered = ClassificationReport.Compute(yTrue, yPred);
        ClassificationReport partial = ClassificationReport.Compute(yTrue, yPred, labels: [0, 1]);

        Assert.Null(covered.MicroAverage);
        Assert.NotNull(partial.MicroAverage);
    }

    private static string[]? TargetNames(JsonElement c) =>
        c.GetProperty("target_names").ValueKind == JsonValueKind.Null
            ? null
            : [.. c.GetProperty("target_names").EnumerateArray().Select(x => x.GetString()!)];
}
