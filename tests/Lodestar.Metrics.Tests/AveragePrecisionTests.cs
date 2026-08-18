using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>Average precision against the frozen corpus, binary and multilabel.</summary>
public sealed class AveragePrecisionTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("average_precision.json");

    private static IReadOnlyList<JsonElement> BinaryCases { get; } =
        [.. Document.RootElement.GetProperty("binary_cases").EnumerateArray()];

    private static IReadOnlyList<JsonElement> MultilabelCases { get; } =
        [.. Document.RootElement.GetProperty("multilabel_cases").EnumerateArray()];

    public static TheoryData<int> BinaryIndices() => Rows(BinaryCases.Count);

    public static TheoryData<int> MultilabelIndices() => Rows(MultilabelCases.Count);

    private static TheoryData<int> Rows(int count)
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < count; i++)
        {
            data.Add(i);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(BinaryIndices))]
    public void Matches_sklearn_on_every_binary_case(int index)
    {
        JsonElement c = BinaryCases[index];
        int[] yTrue = MetricsCorpus.Ints(c, "y_true");
        double[] yScore = MetricsCorpus.Doubles(c, "y_score");
        double[] weight = MetricsCorpus.OptionalDoubles(c, "sample_weight");
        int posLabel = c.GetProperty("pos_label").GetInt32();

        Assert.Equal(
            c.GetProperty("average_precision").GetDouble(),
            AveragePrecision.Score(yTrue, yScore, posLabel, weight),
            MetricsCorpus.Tolerance);
    }

    [Theory]
    [MemberData(nameof(MultilabelIndices))]
    public void Matches_sklearn_on_every_multilabel_case(int index)
    {
        JsonElement c = MultilabelCases[index];
        bool[] yTrue = MetricsCorpus.Bools(c, "y_true");
        double[] yScore = MetricsCorpus.Doubles(c, "y_score");
        double[] weight = MetricsCorpus.OptionalDoubles(c, "sample_weight");
        int labels = c.GetProperty("label_count").GetInt32();

        Assert.Equal(c.GetProperty("macro").GetDouble(),
                     AveragePrecision.Score(yTrue, yScore, labels, Averaging.Macro, weight),
                     MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("micro").GetDouble(),
                     AveragePrecision.Score(yTrue, yScore, labels, Averaging.Micro, weight),
                     MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("weighted").GetDouble(),
                     AveragePrecision.Score(yTrue, yScore, labels, Averaging.Weighted, weight),
                     MetricsCorpus.Tolerance);

        double[] expected = MetricsCorpus.Doubles(c, "per_label");
        double[] actual = AveragePrecision.PerLabel(yTrue, yScore, labels, weight);
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], MetricsCorpus.Tolerance);
        }
    }

    // The corpus carries the trapezoid beside the sum precisely so this can be asserted:
    // reproducing auc(recall, precision) instead is the mistake #210 exists to avoid.
    [Fact]
    public void Is_a_sum_and_not_the_trapezoid()
    {
        int disagreements = 0;
        foreach (JsonElement c in BinaryCases)
        {
            double sum = c.GetProperty("average_precision").GetDouble();
            double trapezoid = c.GetProperty("trapezoid").GetDouble();
            if (Math.Abs(sum - trapezoid) <= MetricsCorpus.Tolerance)
            {
                continue;
            }

            disagreements++;
            int[] yTrue = MetricsCorpus.Ints(c, "y_true");
            double[] yScore = MetricsCorpus.Doubles(c, "y_score");
            double[] weight = MetricsCorpus.OptionalDoubles(c, "sample_weight");
            int posLabel = c.GetProperty("pos_label").GetInt32();

            double actual = AveragePrecision.Score(yTrue, yScore, posLabel, weight);
            Assert.Equal(sum, actual, MetricsCorpus.Tolerance);
            Assert.NotEqual(trapezoid, actual, MetricsCorpus.Tolerance);
        }

        Assert.True(disagreements >= 5, $"only {disagreements} fixtures separate the two");
    }

    [Fact]
    public void Scores_zero_when_no_sample_is_positive()
    {
        int[] yTrue = [0, 0, 0, 0];
        double[] yScore = [0.1, 0.4, 0.35, 0.8];

        Assert.Equal(0.0, AveragePrecision.Score(yTrue, yScore));
    }

    [Fact]
    public void Refuses_binary_averaging_over_a_matrix()
    {
        bool[] yTrue = [true, false, false, false, false, true];
        double[] yScore = [0.75, 0.5, 1.0, 1.0, 0.2, 0.1];

        var error = Assert.Throws<ArgumentOutOfRangeException>(
            () => AveragePrecision.Score(yTrue, yScore, 3, Averaging.Binary));
        Assert.Equal("averaging", error.ParamName);
    }

    [Fact]
    public void Refuses_shapes_that_disagree()
    {
        int[] yTrue = [0, 1, 1];
        double[] yScore = [0.1, 0.2];

        Assert.Throws<ArgumentException>(() => AveragePrecision.Score(yTrue, yScore));
    }
}
