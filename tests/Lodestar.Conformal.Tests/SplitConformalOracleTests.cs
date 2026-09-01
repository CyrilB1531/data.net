using System.Text.Json;
using Xunit;

namespace Lodestar.Conformal.Tests;

/// <summary>Every member replayed against the frozen MAPIE 1.5.0 corpus.</summary>
public sealed class SplitConformalOracleTests
{
    public static TheoryData<int> QuantileCases() => ConformalCorpus.Indices("quantile");

    public static TheoryData<int> RegressionCases() => ConformalCorpus.Indices("regression");

    public static TheoryData<int> ClassificationCases() => ConformalCorpus.Indices("classification");

    [Theory]
    [MemberData(nameof(QuantileCases))]
    public void Quantile_matches_the_k_th_smallest_score(int index)
    {
        JsonElement c = ConformalCorpus.Section("quantile")[index];

        Assert.Equal(ConformalCorpus.Frozen(c, "quantile"),
                     SplitConformal.Quantile(ConformalCorpus.Doubles(c, "scores"),
                                             ConformalCorpus.Alpha(c)),
                     ConformalCorpus.Tolerance);
    }

    [Theory]
    [MemberData(nameof(QuantileCases))]
    public void Quantile_leaves_the_caller_s_scores_in_order(int index)
    {
        JsonElement c = ConformalCorpus.Section("quantile")[index];
        double[] scores = ConformalCorpus.Doubles(c, "scores");
        double[] untouched = [.. scores];

        _ = SplitConformal.Quantile(scores, ConformalCorpus.Alpha(c));

        Assert.Equal(untouched, scores);
    }

    [Theory]
    [MemberData(nameof(RegressionCases))]
    public void Regression_intervals_match_MAPIE(int index)
    {
        JsonElement c = ConformalCorpus.Section("regression")[index];
        double[] residuals = SplitConformal.AbsoluteResiduals(
            ConformalCorpus.Doubles(c, "y_calib"), ConformalCorpus.Doubles(c, "y_calib_pred"));
        double quantile = SplitConformal.Quantile(residuals, ConformalCorpus.Alpha(c));

        Assert.Equal(ConformalCorpus.Frozen(c, "quantile"), quantile, ConformalCorpus.Tolerance);

        double[] predictions = ConformalCorpus.Doubles(c, "y_test_pred");
        double[] lower = ConformalCorpus.Doubles(c, "lower");
        double[] upper = ConformalCorpus.Doubles(c, "upper");
        for (int i = 0; i < predictions.Length; i++)
        {
            (double Lower, double Upper) interval = SplitConformal.Interval(predictions[i], quantile);
            Assert.Equal(lower[i], interval.Lower, ConformalCorpus.Tolerance);
            Assert.Equal(upper[i], interval.Upper, ConformalCorpus.Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(ClassificationCases))]
    public void Prediction_sets_match_MAPIE(int index)
    {
        JsonElement c = ConformalCorpus.Section("classification")[index];
        int classes = c.GetProperty("class_count").GetInt32();

        double[] scores = SplitConformal.LeastAmbiguousScores(
            ConformalCorpus.Doubles(c, "calib_proba"),
            ConformalCorpus.Ints(c, "calib_labels"),
            classes);
        Assert.Equal(ConformalCorpus.Doubles(c, "scores"), scores);

        double quantile = SplitConformal.Quantile(scores, ConformalCorpus.Alpha(c));
        Assert.Equal(ConformalCorpus.Frozen(c, "quantile"), quantile, ConformalCorpus.Tolerance);

        double[] test = ConformalCorpus.Doubles(c, "test_proba");
        int[] expected = ConformalCorpus.Ints(c, "sets");
        for (int row = 0; row < c.GetProperty("test_count").GetInt32(); row++)
        {
            bool[] set = SplitConformal.PredictionSet(
                ConformalCorpus.Row(test, row, classes), quantile);
            for (int j = 0; j < classes; j++)
            {
                Assert.Equal(expected[(row * classes) + j] != 0, set[j]);
            }
        }
    }

    /// <summary>
    /// The corpus exists partly to prove this case is real: LAC returns nothing at all when
    /// no class clears the threshold, and substituting the arg-max there would return a set
    /// with no coverage guarantee under a name that promises one.
    /// </summary>
    [Fact]
    public void An_empty_prediction_set_is_reproduced_rather_than_repaired()
    {
        int empty = 0;
        foreach (JsonElement c in ConformalCorpus.Section("classification"))
        {
            int classes = c.GetProperty("class_count").GetInt32();
            double quantile = ConformalCorpus.Frozen(c, "quantile");
            double[] test = ConformalCorpus.Doubles(c, "test_proba");
            for (int row = 0; row < c.GetProperty("test_count").GetInt32(); row++)
            {
                bool[] set = SplitConformal.PredictionSet(
                    ConformalCorpus.Row(test, row, classes), quantile);
                if (Array.TrueForAll(set, included => !included))
                {
                    empty++;
                }
            }
        }

        Assert.True(empty > 0, "the corpus no longer carries an empty prediction set");
    }

    /// <summary>
    /// The corpus freezes <c>k</c> beside the quantile, so a case that stopped exercising the
    /// ceiling would be visible rather than silently passing on a neighbouring index.
    /// </summary>
    [Theory]
    [MemberData(nameof(QuantileCases))]
    public void The_frozen_k_is_the_index_the_quantile_came_from(int index)
    {
        JsonElement c = ConformalCorpus.Section("quantile")[index];
        double[] sorted = ConformalCorpus.Doubles(c, "scores");
        Array.Sort(sorted);

        Assert.Equal(sorted[c.GetProperty("k").GetInt32() - 1],
                     ConformalCorpus.Frozen(c, "quantile"),
                     ConformalCorpus.Tolerance);
    }
}
