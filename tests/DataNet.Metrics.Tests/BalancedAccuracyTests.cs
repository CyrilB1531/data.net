using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class BalancedAccuracyTests
{
    [Theory]
    [MemberData(nameof(MetricsCorpus.Indices), MemberType = typeof(MetricsCorpus))]
    public void Matches_sklearn_balanced_accuracy_score(int index)
    {
        JsonElement c = MetricsCorpus.Cases[index];

        double actual = BalancedAccuracy.Score(
            MetricsCorpus.Ints(c, "y_true"),
            MetricsCorpus.Ints(c, "y_pred"),
            sampleWeight: MetricsCorpus.OptionalDoubles(c, "sample_weight"));
        double want = OracleLoader.Number(c.GetProperty("balanced_accuracy"));

        Assert.True(Math.Abs(want - actual) < MetricsCorpus.Tolerance,
            $"{MetricsCorpus.Describe(c)}: expected {want}, got {actual}");
    }

    [Theory]
    [MemberData(nameof(MetricsCorpus.Indices), MemberType = typeof(MetricsCorpus))]
    public void Matches_sklearn_with_adjusted(int index)
    {
        JsonElement c = MetricsCorpus.Cases[index];

        double actual = BalancedAccuracy.Score(
            MetricsCorpus.Ints(c, "y_true"),
            MetricsCorpus.Ints(c, "y_pred"),
            adjusted: true,
            sampleWeight: MetricsCorpus.OptionalDoubles(c, "sample_weight"));
        double want = OracleLoader.Number(c.GetProperty("balanced_accuracy_adjusted"));

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
    public void Adjusted_divides_by_zero_when_a_single_class_is_kept()
    {
        // Chance is 1 with one class kept, so the rescale's denominator is 0 and
        // the result is non-finite either way — which value depends on the kept
        // class's recall, and the documented remark used to name only the first:
        //
        //   recall 1.0 -> (1.0 - 1.0) / 0.0 = 0.0 / 0.0  = NaN
        //   recall 0.5 -> (0.5 - 1.0) / 0.0 = -0.5 / 0.0 = -Infinity
        //
        // Both measured against the oracle venv, not guessed: scikit-learn's
        // balanced_accuracy_score returns nan for the first pair and -inf for the
        // second. A two-sample, single-class target is an ordinary accident for a
        // caller to pass, not a contrived edge case.
        Assert.True(double.IsNaN(BalancedAccuracy.Score([1, 1], [1, 1], adjusted: true)));
        Assert.True(double.IsNegativeInfinity(BalancedAccuracy.Score([0, 0], [0, 1], adjusted: true)));
    }

    [Fact]
    public void Reads_a_label_subset_matrix_over_just_the_kept_classes()
    {
        // scikit-learn's balanced_accuracy_score takes no labels= argument, so it
        // has no counterpart for a matrix restricted to a label subset — this is
        // ground the reference cannot reach, not a divergence from it. Score(cm)
        // does the only thing it can: recall over exactly the classes the matrix
        // holds, each one a diagonal cell over its own row sum in the 3x3 view.
        //
        // Restricting to labels [0, 2, 3] gives, in that order:
        //
        //   true 0: [1, 0, 0] -> 1/1 = 1.0     (its other sample was predicted 1)
        //   true 2: [1, 2, 0] -> 2/3
        //   true 3: [0, 0, 0] -> no weight at all, dropped
        //
        //   mean(1.0, 2/3) = 5/6 = 0.833333333333   <- what this pins
        //
        // Three readings of the same matrix, all different, so the number below
        // proves the rule rather than coinciding with it:
        //
        //   0.833333333333  diagonal / row sum of the 3x3 view, kept classes only
        //   0.388888888889  diagonal / ConfusionMatrix.TrueSum (the extended row
        //                   sum Recall.PerClass reads), every class kept: label 0
        //                   becomes 1/2 because the sample predicted as 1 still
        //                   counts in its denominator, and label 3 becomes 0/1
        //                   instead of being dropped -> mean(0.5, 2/3, 0.0)
        //   0.555555555556  a naive average over all Size classes with the
        //                   dropped recall read as 0 -> mean(1.0, 2/3, 0.0)
        int[] yTrue = [0, 0, 2, 2, 2, 3, 1];
        int[] yPred = [0, 1, 2, 2, 0, 1, 1];
        int[] labels = [0, 2, 3];

        ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred, labels);

        Assert.Equal(5.0 / 6.0, BalancedAccuracy.Score(cm), 12);

        // And the kept count the rescale divides by is 2, not 3: chance is 1/2,
        // so adjusted is (5/6 - 1/2) / (1 - 1/2) = 2/3. Reading the extended row
        // sums would keep all three and put chance at 1/3.
        Assert.Equal(2.0 / 3.0, BalancedAccuracy.Score(cm, adjusted: true), 12);
    }
}
