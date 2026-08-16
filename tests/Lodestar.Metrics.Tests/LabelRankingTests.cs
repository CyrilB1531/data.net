using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>The three label-matrix metrics against the frozen corpus.</summary>
public sealed class LabelRankingTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("label_ranking.json");

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

    [Theory]
    [MemberData(nameof(Indices))]
    public void Matches_sklearn_on_every_case(int index)
    {
        JsonElement c = Cases[index];
        bool[] yTrue = MetricsCorpus.Bools(c, "y_true");
        double[] yScore = MetricsCorpus.Doubles(c, "y_score");
        double[] weight = MetricsCorpus.OptionalDoubles(c, "sample_weight");
        int labels = c.GetProperty("label_count").GetInt32();

        Assert.Equal(c.GetProperty("lrap").GetDouble(),
                     LabelRankingAveragePrecision.Score(yTrue, yScore, labels, weight),
                     MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("coverage").GetDouble(),
                     CoverageError.Score(yTrue, yScore, labels, weight),
                     MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("ranking_loss").GetDouble(),
                     LabelRankingLoss.Score(yTrue, yScore, labels, weight),
                     MetricsCorpus.Tolerance);
    }
}
