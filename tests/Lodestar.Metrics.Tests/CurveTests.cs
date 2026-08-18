using System.Globalization;
using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>The three curves and the trapezoid over them, against the frozen corpus.</summary>
public sealed class CurveTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("curves.json");

    private static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("cases").EnumerateArray()];

    public static TheoryData<int> Indices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            data.Add(i);
        }

        return data;
    }

    /// <summary>The corpus writes the threshold at +inf as a string, which JSON cannot hold.</summary>
    private static double[] Thresholds(JsonElement curve) =>
        [.. curve.GetProperty("thresholds").EnumerateArray().Select(v =>
            v.ValueKind == JsonValueKind.String
                ? double.PositiveInfinity
                : v.GetDouble())];

    private static void Same(string what, IReadOnlyList<double> actual, double[] expected)
    {
        Assert.Equal(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            if (double.IsInfinity(expected[i]))
            {
                Assert.Equal(expected[i], actual[i]);
                continue;
            }

            Assert.True(
                Math.Abs(expected[i] - actual[i]) <= MetricsCorpus.Tolerance,
                $"{what}[{i}]: expected {expected[i].ToString("R", CultureInfo.InvariantCulture)}, " +
                $"got {actual[i].ToString("R", CultureInfo.InvariantCulture)}");
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void Matches_sklearn_on_every_curve(int index)
    {
        JsonElement c = Cases[index];
        int[] yTrue = MetricsCorpus.Ints(c, "y_true");
        double[] yScore = MetricsCorpus.Doubles(c, "y_score");
        double[] weight = MetricsCorpus.OptionalDoubles(c, "sample_weight");

        foreach (bool drop in new[] { true, false })
        {
            JsonElement roc = c.GetProperty($"roc_{drop}");
            RocCurve curve = RocCurve.Compute(yTrue, yScore, 1, weight, drop);
            Same("roc fpr", curve.FalsePositiveRate, MetricsCorpus.Doubles(roc, "first"));
            Same("roc tpr", curve.TruePositiveRate, MetricsCorpus.Doubles(roc, "second"));
            Same("roc thresholds", curve.Thresholds, Thresholds(roc));

            JsonElement pr = c.GetProperty($"pr_{drop}");
            PrecisionRecallCurve prc = PrecisionRecallCurve.Compute(yTrue, yScore, 1, weight, drop);
            Same("precision", prc.Precision, MetricsCorpus.Doubles(pr, "first"));
            Same("recall", prc.Recall, MetricsCorpus.Doubles(pr, "second"));
            Same("pr thresholds", prc.Thresholds, Thresholds(pr));

            JsonElement det = c.GetProperty($"det_{drop}");
            DetCurve dc = DetCurve.Compute(yTrue, yScore, 1, weight, drop);
            Same("det fpr", dc.FalsePositiveRate, MetricsCorpus.Doubles(det, "first"));
            Same("det fnr", dc.FalseNegativeRate, MetricsCorpus.Doubles(det, "second"));
            Same("det thresholds", dc.Thresholds, Thresholds(det));
        }
    }

    // The invariant #212 asks for, which no oracle states: integrating the curve this
    // package draws gives the area this package already computed another way.
    [Theory]
    [MemberData(nameof(Indices))]
    public void The_trapezoid_over_the_roc_curve_is_the_roc_auc(int index)
    {
        JsonElement c = Cases[index];
        int[] yTrue = MetricsCorpus.Ints(c, "y_true");
        double[] yScore = MetricsCorpus.Doubles(c, "y_score");
        double[] weight = MetricsCorpus.OptionalDoubles(c, "sample_weight");

        RocCurve curve = RocCurve.Compute(yTrue, yScore, 1, weight);
        double area = Auc.Trapezoid([.. curve.FalsePositiveRate], [.. curve.TruePositiveRate]);

        Assert.Equal(c.GetProperty("roc_auc").GetDouble(), area, MetricsCorpus.Tolerance);
        Assert.Equal(RocAuc.Score(yTrue, yScore, 1, weight), area, MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("roc_trapezoid").GetDouble(), area, MetricsCorpus.Tolerance);
    }

    // The precision-recall curve is where the trapezoid is the wrong reading, which is
    // why AveragePrecision sums instead. Both are kept, and they must not agree.
    [Fact]
    public void The_trapezoid_over_the_precision_recall_curve_is_not_average_precision()
    {
        int[] yTrue = [0, 0, 1, 1];
        double[] yScore = [0.1, 0.4, 0.35, 0.8];

        PrecisionRecallCurve curve = PrecisionRecallCurve.Compute(yTrue, yScore);
        double trapezoid = Auc.Trapezoid([.. curve.Recall], [.. curve.Precision]);

        Assert.Equal(0.7916666666666666, trapezoid, MetricsCorpus.Tolerance);
        Assert.Equal(0.8333333333333333, AveragePrecision.Score(yTrue, yScore), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void The_thresholds_array_is_one_shorter_only_on_the_precision_recall_curve()
    {
        int[] yTrue = [0, 0, 1, 1];
        double[] yScore = [0.1, 0.4, 0.35, 0.8];

        RocCurve roc = RocCurve.Compute(yTrue, yScore);
        Assert.Equal(roc.FalsePositiveRate.Count, roc.Thresholds.Count);

        DetCurve det = DetCurve.Compute(yTrue, yScore);
        Assert.Equal(det.FalsePositiveRate.Count, det.Thresholds.Count);

        PrecisionRecallCurve pr = PrecisionRecallCurve.Compute(yTrue, yScore);
        Assert.Equal(pr.Precision.Count - 1, pr.Thresholds.Count);
    }

    [Fact]
    public void Auc_refuses_what_it_cannot_integrate()
    {
        Assert.Throws<ArgumentException>(() => Auc.Trapezoid([0.0], [1.0]));
        Assert.Throws<ArgumentException>(() => Auc.Trapezoid([0.0, 1.0], [1.0]));
        Assert.Throws<ArgumentException>(() => Auc.Trapezoid([0.0, 1.0, 0.5], [1.0, 2.0, 3.0]));
    }

    [Fact]
    public void Auc_reads_a_curve_the_same_in_either_direction()
    {
        double[] x = [0.0, 0.5, 1.0];
        double[] y = [0.0, 0.8, 1.0];
        double[] backwardsX = [1.0, 0.5, 0.0];
        double[] backwardsY = [1.0, 0.8, 0.0];

        Assert.Equal(Auc.Trapezoid(x, y), Auc.Trapezoid(backwardsX, backwardsY), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Refuses_an_empty_input()
    {
        Assert.Throws<ArgumentException>(() => RocCurve.Compute([], []));
        Assert.Throws<ArgumentException>(() => PrecisionRecallCurve.Compute([0], [double.NaN]));
        Assert.Throws<ArgumentException>(() => DetCurve.Compute([0, 1], [0.5]));
    }
}
