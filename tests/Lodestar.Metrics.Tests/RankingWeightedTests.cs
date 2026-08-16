using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>
/// DCG and NDCG with a sample weight, over the multi-row queries that alone can show
/// one: a weight over a single row multiplies numerator and denominator alike.
/// </summary>
public sealed class RankingWeightedTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("ranking_weighted.json");

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
        double[] yTrue = MetricsCorpus.Doubles(c, "y_true");
        double[] yScore = MetricsCorpus.Doubles(c, "y_score");
        double[] weight = MetricsCorpus.Doubles(c, "sample_weight");
        int labels = c.GetProperty("label_count").GetInt32();

        Assert.Equal(c.GetProperty("dcg_weighted").GetDouble(),
                     Dcg.Score(yTrue, yScore, labels, sampleWeight: weight),
                     MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("dcg_weighted_log_e").GetDouble(),
                     Dcg.Score(yTrue, yScore, labels, logBase: Math.E, sampleWeight: weight),
                     MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("dcg_weighted_at_2").GetDouble(),
                     Dcg.Score(yTrue, yScore, labels, k: 2, sampleWeight: weight),
                     MetricsCorpus.Tolerance);

        Assert.Equal(c.GetProperty("ndcg_weighted").GetDouble(),
                     Ndcg.Score(yTrue, yScore, labels, sampleWeight: weight),
                     MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("ndcg_weighted_at_2").GetDouble(),
                     Ndcg.Score(yTrue, yScore, labels, k: 2, sampleWeight: weight),
                     MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("ndcg_weighted_ignore_ties").GetDouble(),
                     Ndcg.Score(yTrue, yScore, labels, ignoreTies: true, sampleWeight: weight),
                     MetricsCorpus.Tolerance);
    }

    [Fact]
    public void The_corpus_separates_a_weighted_mean_from_an_unweighted_one()
    {
        // Without this the suite could pass with sampleWeight ignored altogether:
        // one fixture weights both rows equally, and its two columns agree by design.
        int separating = 0;
        foreach (JsonElement c in Cases)
        {
            if (Math.Abs(c.GetProperty("dcg").GetDouble() -
                         c.GetProperty("dcg_weighted").GetDouble()) > MetricsCorpus.Tolerance)
            {
                separating++;
            }
        }

        Assert.Equal(5, separating);
    }
}
