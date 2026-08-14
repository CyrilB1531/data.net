using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class ReportTextTests
{
    [Theory]
    [MemberData(nameof(MetricsCorpus.Indices), MemberType = typeof(MetricsCorpus))]
    public void Renders_the_sklearn_table_character_for_character(int index)
    {
        JsonElement c = MetricsCorpus.Cases[index];
        string what = MetricsCorpus.Describe(c);

        ClassificationReport report = ClassificationReport.Compute(
            MetricsCorpus.Ints(c, "y_true"),
            MetricsCorpus.Ints(c, "y_pred"),
            TargetNames(c),
            ZeroDivision.Zero,
            MetricsCorpus.OptionalInts(c, "labels"),
            MetricsCorpus.OptionalDoubles(c, "sample_weight"));

        foreach (JsonProperty entry in c.GetProperty("reports").EnumerateObject())
        {
            int digits = int.Parse(entry.Name, System.Globalization.CultureInfo.InvariantCulture);
            string expected = entry.Value.GetString()!;
            string actual = report.ToText(digits);

            Assert.True(expected == actual,
                $"{what} at {digits} digits:\n--- expected ---\n{expected}\n--- actual ---\n{actual}");
        }
    }

    [Fact]
    public void ToString_is_the_two_digit_table()
    {
        int[] yTrue = [0, 1, 1, 0];
        int[] yPred = [0, 1, 0, 0];
        ClassificationReport report = ClassificationReport.Compute(yTrue, yPred);

        // ToText() == ToString() alone would still pass if the "digits = 2" default
        // silently drifted, since both forward to it; pin digits: 2 explicitly too.
        Assert.Equal(report.ToText(2), report.ToText());
        Assert.Equal(report.ToText(), report.ToString());
    }

    /// <summary>
    /// Every requested class (0, 1) is wrong; class 2 — not requested — is right,
    /// so scikit-learn's own "was anything predicted correctly at all" check
    /// still finds tp_bins non-empty and prints integers, not floats, although
    /// <see cref="ClassificationReport.Accuracy"/> over the requested labels is
    /// 0.0. Confirmed against scikit-learn 1.9.0 (transcript in task-2-report.md).
    /// </summary>
    [Fact]
    public void Support_stays_integral_when_a_correct_prediction_falls_outside_the_requested_labels()
    {
        int[] yTrue = [0, 1, 2, 2];
        int[] yPred = [1, 0, 2, 2];
        ClassificationReport report = ClassificationReport.Compute(
            yTrue, yPred, null, ZeroDivision.Zero, labels: [0, 1]);

        Assert.Equal(0.0, report.Accuracy);
        const string Expected =
            "              precision    recall  f1-score   support\n"
            + "\n"
            + "           0       0.00      0.00      0.00         1\n"
            + "           1       0.00      0.00      0.00         1\n"
            + "\n"
            + "   micro avg       0.00      0.00      0.00         2\n"
            + "   macro avg       0.00      0.00      0.00         2\n"
            + "weighted avg       0.00      0.00      0.00         2\n";

        Assert.Equal(Expected, report.ToText(2));
    }

    [Fact]
    public void Rejects_a_digit_count_below_zero()
    {
        int[] yTrue = [0, 1];
        int[] yPred = [0, 1];
        ClassificationReport report = ClassificationReport.Compute(yTrue, yPred);

        Assert.Throws<ArgumentOutOfRangeException>(() => report.ToText(-1));
    }

    private static string[]? TargetNames(JsonElement c) =>
        c.GetProperty("target_names").ValueKind == JsonValueKind.Null
            ? null
            : [.. c.GetProperty("target_names").EnumerateArray().Select(x => x.GetString()!)];
}
