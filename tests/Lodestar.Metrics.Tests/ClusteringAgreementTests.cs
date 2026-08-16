using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>
/// The five agreement metrics against the frozen corpus, and the four degenerate
/// cases whose answers a reader would otherwise take for bugs.
/// </summary>
public sealed class ClusteringAgreementTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("clustering_agreement.json");

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
        int[] labelsTrue = MetricsCorpus.Ints(c, "labels_true");
        int[] labelsPred = MetricsCorpus.Ints(c, "labels_pred");

        Assert.Equal(c.GetProperty("adjusted_rand").GetDouble(),
                     AdjustedRand.Score(labelsTrue, labelsPred), MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("normalized_mutual_information").GetDouble(),
                     NormalizedMutualInformation.Score(labelsTrue, labelsPred), MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("homogeneity").GetDouble(),
                     Homogeneity.Score(labelsTrue, labelsPred), MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("completeness").GetDouble(),
                     Completeness.Score(labelsTrue, labelsPred), MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("v_measure").GetDouble(),
                     VMeasure.Score(labelsTrue, labelsPred), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void An_empty_labelling_is_perfect_agreement_rather_than_an_error()
    {
        // scikit-learn's answer, and the one thing here that no other metric in this
        // package does: Accuracy.Score refuses an empty input outright.
        Assert.Equal(1.0, AdjustedRand.Score([], []), MetricsCorpus.Tolerance);
        Assert.Equal(1.0, NormalizedMutualInformation.Score([], []), MetricsCorpus.Tolerance);
        Assert.Equal(1.0, VMeasure.Score([], []), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Renaming_the_clusters_changes_nothing()
    {
        int[] labels = [0, 0, 1, 1, 2, 2];
        int[] renamed = [7, 7, -3, -3, 5, 5];

        Assert.Equal(1.0, AdjustedRand.Score(labels, renamed), MetricsCorpus.Tolerance);
        Assert.Equal(1.0, VMeasure.Score(labels, renamed), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Homogeneity_and_completeness_are_each_other_reversed()
    {
        int[] one = [0, 0, 1, 1, 2, 2];
        int[] other = [0, 0, 0, 1, 1, 2];

        Assert.Equal(Homogeneity.Score(one, other),
                     Completeness.Score(other, one), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Splitting_every_sample_scores_one_for_homogeneity_and_zero_for_chance()
    {
        // The pair that shows why both numbers are reported: a clustering that says
        // nothing is perfectly homogeneous, and adjusted Rand is what sees through it.
        int[] labelsTrue = [0, 0, 1, 1];
        int[] alone = [0, 1, 2, 3];

        Assert.Equal(1.0, Homogeneity.Score(labelsTrue, alone), MetricsCorpus.Tolerance);
        Assert.Equal(0.0, AdjustedRand.Score(labelsTrue, alone), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Labellings_of_different_lengths_are_refused()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => AdjustedRand.Score([0, 1, 2], [0, 1]));

        Assert.Contains("they must agree", error.Message, StringComparison.Ordinal);
    }
}
