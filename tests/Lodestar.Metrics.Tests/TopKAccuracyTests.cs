using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>Top-k accuracy against the frozen corpus, ties in a score row included.</summary>
public sealed class TopKAccuracyTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("top_k_accuracy.json");

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
    public void Matches_sklearn_at_every_k(int index)
    {
        JsonElement c = Cases[index];
        int[] yTrue = MetricsCorpus.Ints(c, "y_true");
        double[] yScore = MetricsCorpus.Doubles(c, "y_score");
        int classes = c.GetProperty("class_count").GetInt32();

        foreach (int k in new[] { 1, 2, classes })
        {
            Assert.Equal(c.GetProperty($"top_{k}").GetDouble(),
                         TopKAccuracy.Score(yTrue, yScore, classes, k), MetricsCorpus.Tolerance);
            Assert.Equal(c.GetProperty($"top_{k}_count").GetDouble(),
                         TopKAccuracy.Score(yTrue, yScore, classes, k, normalize: false),
                         MetricsCorpus.Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void Matches_sklearn_at_every_k_with_a_sample_weight(int index)
    {
        JsonElement c = Cases[index];
        int[] yTrue = MetricsCorpus.Ints(c, "y_true");
        double[] yScore = MetricsCorpus.Doubles(c, "y_score");
        double[] weight = MetricsCorpus.Doubles(c, "sample_weight");
        int classes = c.GetProperty("class_count").GetInt32();

        foreach (int k in new[] { 1, 2, classes })
        {
            Assert.Equal(c.GetProperty($"top_{k}_weighted").GetDouble(),
                         TopKAccuracy.Score(yTrue, yScore, classes, k, sampleWeight: weight),
                         MetricsCorpus.Tolerance);

            // normalize: false sums the weights of the hits rather than counting them.
            // Measured, 7.0 against the unweighted 3.0 on the corpus' first fixture.
            Assert.Equal(c.GetProperty($"top_{k}_weighted_count").GetDouble(),
                         TopKAccuracy.Score(yTrue, yScore, classes, k, normalize: false,
                                            sampleWeight: weight),
                         MetricsCorpus.Tolerance);
        }
    }

    [Fact]
    public void Every_class_in_the_top_k_is_a_hit_when_k_reaches_the_class_count()
    {
        int[] yTrue = [0, 1, 2];
        double[] yScore = [0.5, 0.3, 0.2, 0.1, 0.8, 0.1, 0.2, 0.2, 0.6];

        Assert.Equal(1.0, TopKAccuracy.Score(yTrue, yScore, 3, k: 3), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void A_k_past_the_class_count_is_not_an_error()
    {
        // The loop is bounded by the class count as well as by k, so a k nobody can
        // exceed scores every sample rather than reading past the row.
        int[] yTrue = [0, 1, 2];
        double[] yScore = [0.5, 0.3, 0.2, 0.1, 0.8, 0.1, 0.2, 0.2, 0.6];

        Assert.Equal(1.0, TopKAccuracy.Score(yTrue, yScore, 3, k: 99), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void A_k_below_one_is_refused()
    {
        int[] yTrue = [0, 1];
        double[] yScore = [0.5, 0.5, 0.5, 0.5];

        Assert.Throws<ArgumentOutOfRangeException>(
            () => TopKAccuracy.Score(yTrue, yScore, 2, k: 0));
    }

    [Fact]
    public void Fewer_than_two_classes_is_refused()
    {
        Assert.Throws<ArgumentException>(() => TopKAccuracy.Score([0, 0], [0.5, 0.5], 1));
    }

    [Fact]
    public void A_score_block_that_is_not_one_row_per_sample_is_refused()
    {
        Assert.Throws<ArgumentException>(
            () => TopKAccuracy.Score([0, 1], [0.5, 0.3, 0.2, 0.1, 0.8], 3));
    }

    [Fact]
    public void No_samples_at_all_is_refused_rather_than_returning_NaN()
    {
        Assert.Throws<ArgumentException>(
            () => TopKAccuracy.Score([], [], 3));
    }

    [Fact]
    public void A_true_class_outside_the_row_is_refused_rather_than_counted_as_a_miss()
    {
        Assert.Throws<ArgumentException>(
            () => TopKAccuracy.Score([0, 5], [0.7, 0.2, 0.1, 0.3, 0.5, 0.2], 3));
    }

    [Fact]
    public void Ties_rank_by_descending_index_past_the_sort_that_stops_being_stable()
    {
        // scikit-learn takes the last k of a mergesort, which puts the highest index of a
        // tied group first; Array.Sort is only stable below 16, so 30 classes see the gap.
        const int n = 30;
        double[] tied = new double[n];
        Array.Fill(tied, 0.5);

        Assert.Equal(1.0, TopKAccuracy.Score([n - 3], tied, n, k: 3), MetricsCorpus.Tolerance);
        Assert.Equal(0.0, TopKAccuracy.Score([n - 4], tied, n, k: 3), MetricsCorpus.Tolerance);
    }
}
