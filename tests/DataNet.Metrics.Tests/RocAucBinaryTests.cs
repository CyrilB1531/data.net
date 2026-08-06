using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class RocAucBinaryTests
{
    [Theory]
    [MemberData(nameof(RocCorpus.BinaryIndices), MemberType = typeof(RocCorpus))]
    public void Matches_sklearn_roc_auc_score(int index)
    {
        JsonElement c = RocCorpus.Cases[index];
        double expected = c.GetProperty("values").GetProperty("binary").GetDouble();

        double actual = RocAuc.Score(
            RocCorpus.YTrue(c), RocCorpus.FlatScores(c), sampleWeight: RocCorpus.SampleWeight(c));

        Assert.True(Math.Abs(expected - actual) < MetricsCorpus.Tolerance,
            $"{RocCorpus.Describe(c)}: expected {expected}, got {actual}");
    }

    [Fact]
    public void Rejects_a_single_class()
    {
        int[] yTrue = [1, 1, 1];
        double[] scores = [0.1, 0.4, 0.9];

        Assert.Throws<ArgumentException>(() => RocAuc.Score(yTrue, scores));
    }

    [Fact]
    public void Rejects_mismatched_lengths()
    {
        int[] yTrue = [0, 1, 1];
        double[] scores = [0.1, 0.4];

        Assert.Throws<ArgumentException>(() => RocAuc.Score(yTrue, scores));
    }

    [Fact]
    public void Rejects_a_nan_score()
    {
        int[] yTrue = [0, 1];
        double[] scores = [0.5, double.NaN];

        Assert.Throws<ArgumentException>(() => RocAuc.Score(yTrue, scores));
    }

    [Fact]
    public void A_perfect_ranking_scores_one_and_its_reverse_scores_zero()
    {
        int[] yTrue = [0, 0, 1, 1];
        double[] good = [0.1, 0.2, 0.8, 0.9];
        double[] bad = [0.9, 0.8, 0.2, 0.1];

        Assert.Equal(1.0, RocAuc.Score(yTrue, good), MetricsCorpus.Tolerance);
        Assert.Equal(0.0, RocAuc.Score(yTrue, bad), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void All_scores_tied_gives_one_half()
    {
        int[] yTrue = [0, 1, 0, 1];
        double[] scores = [0.5, 0.5, 0.5, 0.5];

        Assert.Equal(0.5, RocAuc.Score(yTrue, scores), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void PosLabel_selects_which_class_counts_as_positive()
    {
        int[] yTrue = [0, 0, 1, 1];
        double[] scores = [0.1, 0.2, 0.8, 0.9];

        // Label 1 ranks highest: treating 1 as positive gives a perfect score,
        // but treating 0 as positive reverses which end of the ranking counts,
        // so it must come out as the worst possible score, not the same value.
        Assert.Equal(1.0, RocAuc.Score(yTrue, scores, posLabel: 1), MetricsCorpus.Tolerance);
        Assert.Equal(0.0, RocAuc.Score(yTrue, scores, posLabel: 0), MetricsCorpus.Tolerance);
    }
}
