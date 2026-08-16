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

    [Fact]
    public void A_zero_sum_weight_vector_is_refused_wherever_the_metric_divides()
    {
        double[] yTrue = [3.0, 2.0, 1.0, 0.0, 3.0, 2.0, 1.0, 0.0];
        double[] yScore = [0.9, 0.5, 0.4, 0.1, 0.1, 0.4, 0.5, 0.9];
        double[] zeroSum = [1.0, -1.0];

        ArgumentException dcg = Assert.Throws<ArgumentException>(
            () => Dcg.Score(yTrue, yScore, 4, sampleWeight: zeroSum));
        Assert.Contains("Weights sum to zero", dcg.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => Ndcg.Score(yTrue, yScore, 4, sampleWeight: zeroSum));
    }

    [Fact]
    public void A_weight_vector_of_the_wrong_length_is_refused()
    {
        double[] yTrue = [3.0, 2.0, 1.0, 0.0, 3.0, 2.0, 1.0, 0.0];
        double[] yScore = [0.9, 0.5, 0.4, 0.1, 0.1, 0.4, 0.5, 0.9];

        // Two queries, three weights: the shape scikit-learn refuses as
        // "inconsistent numbers of samples".
        Assert.Throws<ArgumentException>(
            () => Dcg.Score(yTrue, yScore, 4, sampleWeight: [1.0, 1.0, 1.0]));
        Assert.Throws<ArgumentException>(
            () => Ndcg.Score(yTrue, yScore, 4, sampleWeight: [1.0]));
        Assert.Throws<ArgumentException>(
            () => TopKAccuracy.Score([0, 1], [0.9, 0.1, 0.1, 0.9], 2, sampleWeight: [1.0]));
    }

    [Fact]
    public void An_empty_weight_vector_is_the_unweighted_mean()
    {
        // The default, asserted as an equality rather than as a frozen number: a
        // regression that ignored an empty span would still pass a value check.
        double[] yTrue = [3.0, 2.0, 1.0, 0.0, 3.0, 2.0, 1.0, 0.0];
        double[] yScore = [0.9, 0.5, 0.4, 0.1, 0.1, 0.4, 0.5, 0.9];

        Assert.Equal(Dcg.Score(yTrue, yScore, 4),
                     Dcg.Score(yTrue, yScore, 4, sampleWeight: []), MetricsCorpus.Tolerance);
        Assert.Equal(Ndcg.Score(yTrue, yScore, 4),
                     Ndcg.Score(yTrue, yScore, 4, sampleWeight: []), MetricsCorpus.Tolerance);
    }
}
