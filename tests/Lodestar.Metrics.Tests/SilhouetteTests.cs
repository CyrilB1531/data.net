using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>
/// Silhouette against the frozen corpus — the score, the per-sample values, and the
/// equality of the two input paths — plus the edges a corpus cannot hold.
/// </summary>
public sealed class SilhouetteTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("silhouette.json");

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
    public void Matches_sklearn_from_features(int index)
    {
        JsonElement c = Cases[index];
        int[] labels = MetricsCorpus.Ints(c, "labels");
        double[] features = MetricsCorpus.Doubles(c, "features");
        int featureCount = c.GetProperty("feature_count").GetInt32();

        Assert.Equal(c.GetProperty("score").GetDouble(),
                     Silhouette.Score(labels, features, featureCount), MetricsCorpus.Tolerance);

        double[] expected = MetricsCorpus.Doubles(c, "per_sample");
        double[] actual = Silhouette.PerSample(labels, features, featureCount);
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], MetricsCorpus.Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void Matches_sklearn_from_a_precomputed_matrix(int index)
    {
        JsonElement c = Cases[index];
        int[] labels = MetricsCorpus.Ints(c, "labels");
        double[] distances = MetricsCorpus.Doubles(c, "distances");

        Assert.Equal(c.GetProperty("score_precomputed").GetDouble(),
                     Silhouette.ScoreFromDistances(labels, distances), MetricsCorpus.Tolerance);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_two_input_paths_are_one_computation(int index)
    {
        // sklearn gives the same double either way, and so must this pair -- which is what
        // lets a reader pick whichever input they already hold.
        JsonElement c = Cases[index];
        int[] labels = MetricsCorpus.Ints(c, "labels");

        Assert.Equal(
            Silhouette.Score(labels, MetricsCorpus.Doubles(c, "features"),
                             c.GetProperty("feature_count").GetInt32()),
            Silhouette.ScoreFromDistances(labels, MetricsCorpus.Doubles(c, "distances")),
            MetricsCorpus.Tolerance);
    }

    [Fact]
    public void A_cluster_of_one_sample_scores_that_sample_zero()
    {
        double[] features = [0.0, 0.0, 0.1, 0.1, 5.0, 5.0, 5.1, 5.2, 9.0, 9.0];
        int[] labels = [0, 0, 1, 1, 2];

        double[] scores = Silhouette.PerSample(labels, features, 2);

        Assert.Equal(0.0, scores[4], MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Fewer_than_two_clusters_is_refused_in_sklearns_words()
    {
        double[] features = [0.0, 0.0, 1.0, 1.0, 2.0, 2.0];
        int[] labels = [0, 0, 0];

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => Silhouette.Score(labels, features, 2));

        Assert.Contains("Number of labels is 1. Valid values are 2 to n_samples - 1 (inclusive)",
                        error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void One_cluster_per_sample_is_refused_too()
    {
        double[] features = [0.0, 0.0, 1.0, 1.0, 2.0, 2.0];
        int[] labels = [0, 1, 2];

        Assert.Throws<ArgumentException>(() => Silhouette.Score(labels, features, 2));
    }

    [Fact]
    public void A_matrix_that_is_not_n_by_n_is_refused()
    {
        double[] distances = [0.0, 1.0, 1.0, 0.0, 2.0];
        int[] labels = [0, 1];

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => Silhouette.ScoreFromDistances(labels, distances));

        Assert.Contains("which is not 2 squared", error.Message, StringComparison.Ordinal);
    }
}
