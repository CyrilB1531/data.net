using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class BalancedAccuracyTests
{
    [Fact]
    public void Matches_sklearn_balanced_accuracy_score()
    {
        foreach (JsonElement c in MetricsCorpus.Cases)
        {
            if (!c.TryGetProperty("balanced_accuracy", out JsonElement expected))
            {
                continue;
            }

            double actual = BalancedAccuracy.Score(
                MetricsCorpus.Ints(c, "y_true"),
                MetricsCorpus.Ints(c, "y_pred"),
                sampleWeight: MetricsCorpus.OptionalDoubles(c, "sample_weight"));
            double want = OracleLoader.Number(expected);

            Assert.True(Math.Abs(want - actual) < MetricsCorpus.Tolerance,
                $"{MetricsCorpus.Describe(c)}: expected {want}, got {actual}");
        }
    }

    [Fact]
    public void Matches_sklearn_with_adjusted()
    {
        foreach (JsonElement c in MetricsCorpus.Cases)
        {
            if (!c.TryGetProperty("balanced_accuracy_adjusted", out JsonElement expected))
            {
                continue;
            }

            double actual = BalancedAccuracy.Score(
                MetricsCorpus.Ints(c, "y_true"),
                MetricsCorpus.Ints(c, "y_pred"),
                adjusted: true,
                sampleWeight: MetricsCorpus.OptionalDoubles(c, "sample_weight"));
            double want = OracleLoader.Number(expected);

            // A handful of fixtures (single_sample, single_class) keep exactly one
            // class, where chance is 1 and the adjusted score is 0.0 / 0.0 — NaN
            // under IEEE 754, which is also what scikit-learn's own
            // balanced_accuracy_score returns there. NaN is never < Tolerance of
            // itself, so those two need the IsNaN branch rather than the
            // subtraction every other fixture uses.
            if (double.IsNaN(want))
            {
                Assert.True(double.IsNaN(actual),
                    $"{MetricsCorpus.Describe(c)}: expected NaN, got {actual}");
            }
            else
            {
                Assert.True(Math.Abs(want - actual) < MetricsCorpus.Tolerance,
                    $"{MetricsCorpus.Describe(c)}: expected {want}, got {actual}");
            }
        }
    }

    [Fact]
    public void Averages_over_the_classes_it_kept_not_over_all_of_them()
    {
        // The whole of balanced accuracy's degenerate case, and the only fixture
        // that separates the right implementation from the plausible one. Class 2
        // is predicted but never true, so its recall is undefined and
        // scikit-learn drops it: mean(0.5, 1.0) = 0.75. Averaging over all three
        // with an undefined recall read as 0 would give 0.5.
        int[] yTrue = [0, 0, 1];
        int[] yPred = [0, 2, 1];

        Assert.Equal(0.75, BalancedAccuracy.Score(yTrue, yPred), 12);
    }

    [Fact]
    public void Adjusted_divides_by_the_classes_it_kept()
    {
        // Two classes kept, so chance is 1/2 and the adjusted score is
        // (0.75 - 0.5) / (1 - 0.5) = 0.5. Counting all three classes would put
        // chance at 1/3 and give 0.625.
        int[] yTrue = [0, 0, 1];
        int[] yPred = [0, 2, 1];

        Assert.Equal(0.5, BalancedAccuracy.Score(yTrue, yPred, adjusted: true), 12);
    }

    [Fact]
    public void Reads_the_same_number_from_the_labels_and_from_the_matrix()
    {
        int[] yTrue = [0, 0, 1, 1, 2, 2, 2];
        int[] yPred = [0, 1, 1, 1, 2, 0, 2];

        double fromLabels = BalancedAccuracy.Score(yTrue, yPred);
        double fromMatrix = BalancedAccuracy.Score(ConfusionMatrix.Compute(yTrue, yPred));

        Assert.Equal(fromMatrix, fromLabels);
    }

    [Fact]
    public void Adjusted_is_NaN_when_a_single_class_is_kept()
    {
        // scikit-learn: balanced_accuracy_score([1, 1], [1, 1], adjusted=True)
        // returns nan (measured against the oracle venv, not guessed) — chance
        // is 1 with one class kept, so the rescale is 0.0 / 0.0. This is a
        // two-sample, single-class target, which is an ordinary accident for a
        // caller to pass, not a contrived edge case.
        int[] yTrue = [1, 1];
        int[] yPred = [1, 1];

        Assert.True(double.IsNaN(BalancedAccuracy.Score(yTrue, yPred, adjusted: true)));
    }

    [Fact]
    public void Reads_a_label_subset_matrix_over_just_the_kept_classes()
    {
        // scikit-learn's balanced_accuracy_score takes no labels= argument, so it
        // has no counterpart for a matrix restricted to a label subset — this is
        // ground the reference cannot reach, not a divergence from it. Score(cm)
        // does the only thing it can: average recall over the classes the matrix
        // itself holds. Restricting to labels [0, 2] keeps two rows: true label 0
        // has 2 samples, 1 predicted correctly (recall 0.5); true label 2 has 3
        // samples, all predicted correctly (recall 1.0). mean(0.5, 1.0) = 0.75,
        // pinned here as our own implementation's behaviour.
        int[] yTrue = [0, 0, 1, 2, 2, 2];
        int[] yPred = [0, 1, 1, 2, 2, 2];
        int[] labels = [0, 2];

        ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred, labels);

        Assert.Equal(0.75, BalancedAccuracy.Score(cm), 12);
    }
}
