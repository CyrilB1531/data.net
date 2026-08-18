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
        Assert.Equal(c.GetProperty("fowlkes_mallows").GetDouble(),
                     FowlkesMallows.Score(labelsTrue, labelsPred), MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("adjusted_mutual_information").GetDouble(),
                     AdjustedMutualInformation.Score(labelsTrue, labelsPred), MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("rand").GetDouble(),
                     RandIndex.Score(labelsTrue, labelsPred), MetricsCorpus.Tolerance);

        // scikit-learn 1.9.0 raises on this one case -- log(0) inside mutual_info_score,
        // where an empty input returns everywhere else in this family. The corpus records
        // that as null rather than a number, and there is nothing to compare it against.
        JsonElement mi = c.GetProperty("mutual_information");
        if (mi.ValueKind != JsonValueKind.Null)
        {
            Assert.Equal(mi.GetDouble(), MutualInformation.Score(labelsTrue, labelsPred), MetricsCorpus.Tolerance);
        }

        long[] pair = [.. c.GetProperty("pair_confusion").EnumerateArray().Select(x => x.GetInt64())];
        PairConfusionMatrix confusion = PairConfusionMatrix.Compute(labelsTrue, labelsPred);
        Assert.Equal(pair, new[]
        {
            confusion.DifferentInBoth, confusion.SameInPredictedOnly,
            confusion.SameInTrueOnly, confusion.SameInBoth,
        });
    }

    [Fact]
    public void Rand_and_AdjustedRand_agree_where_the_correction_is_zero()
    {
        // Two labellings that agree on every pair need no correction for chance:
        // there is nothing between "what was observed" and "what the correction
        // would have removed" to disagree about, uncorrected or not.
        int[] labelsTrue = [0, 0, 1, 1, 2, 2];
        int[] labelsPred = [2, 2, 0, 0, 1, 1];

        double rand = RandIndex.Score(labelsTrue, labelsPred);
        double adjusted = AdjustedRand.Score(labelsTrue, labelsPred);

        Assert.Equal(1.0, rand, MetricsCorpus.Tolerance);
        Assert.Equal(rand, adjusted, MetricsCorpus.Tolerance);
    }

    [Fact]
    public void MutualInformation_raises_on_empty_where_this_package_does_not()
    {
        // Measured against scikit-learn 1.9.0: mutual_info_score([], []) raises
        // ValueError (log(0) inside it), which is not a documented refusal --
        // decision 0039 records why this package returns 0.0 there instead.
        double empty = MutualInformation.Score([], []);
        Assert.Equal(0.0, empty, MetricsCorpus.Tolerance);

        // A single sample carries no information to share, and unlike the six
        // agreement metrics this family started with, that is 0.0 here, not 1.0.
        double single = MutualInformation.Score([0], [0]);
        Assert.Equal(0.0, single, MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Fowlkes_Mallows_scores_zero_where_the_other_five_score_one()
    {
        // A fact per metric, not a rule: no agreeing pair exists to count here,
        // where the other five read an absence of disagreement as agreement.
        Assert.Equal(0.0, FowlkesMallows.Score([], []), MetricsCorpus.Tolerance);
        Assert.Equal(0.0, FowlkesMallows.Score([0], [0]), MetricsCorpus.Tolerance);
        Assert.Equal(1.0, AdjustedMutualInformation.Score([], []), MetricsCorpus.Tolerance);
        Assert.Equal(1.0, AdjustedMutualInformation.Score([0], [0]), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void The_chance_correction_is_what_separates_the_two_new_metrics()
    {
        // Two independent partitions of four samples. Only the chance-corrected pair
        // scores below zero, which is the whole reason to reach for either of them.
        int[] labelsTrue = [0, 0, 1, 1];
        int[] labelsPred = [0, 1, 0, 1];

        Assert.True(AdjustedMutualInformation.Score(labelsTrue, labelsPred) < 0.0);
        Assert.True(AdjustedRand.Score(labelsTrue, labelsPred) < 0.0);
        Assert.Equal(0.0, NormalizedMutualInformation.Score(labelsTrue, labelsPred), MetricsCorpus.Tolerance);
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
