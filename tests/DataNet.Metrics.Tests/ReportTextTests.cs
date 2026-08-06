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

        // ToText() == ToString() alone would pass even if the "digits = 2" default
        // silently drifted, since ToString forwards to ToText's default either way:
        // pin the default explicitly against digits: 2 so a changed default fails.
        Assert.Equal(report.ToText(2), report.ToText());
        Assert.Equal(report.ToText(), report.ToString());
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
