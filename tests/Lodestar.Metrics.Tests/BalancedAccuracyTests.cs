using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

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

        // Two fixtures keep exactly one class, where chance is 1 and the score
        // is 0.0 / 0.0 = NaN, which is never < Tolerance of itself.
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

    /// <summary>
    /// The one fixture that separates the right implementation from the plausible
    /// one. Class 2 is predicted but never true, so its recall is undefined and
    /// scikit-learn drops it: mean(0.5, 1.0) = 0.75. Averaging over all three
    /// classes with the undefined recall read as 0 would give 0.5 instead.
    /// </summary>
    [Fact]
    public void Averages_over_the_classes_it_kept_not_over_all_of_them()
    {
        int[] yTrue = [0, 0, 1];
        int[] yPred = [0, 2, 1];

        Assert.Equal(0.75, BalancedAccuracy.Score(yTrue, yPred), 12);
    }

    [Fact]
    public void Adjusted_divides_by_the_classes_it_kept()
    {
        // Two classes are kept, so chance sits at one half and the adjusted score
        // is 0.5 -- counting all three would put chance at a third, giving 0.625.
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

    /// <summary>
    /// Chance is 1 with one class kept, so the rescale's denominator is 0 and the
    /// result is non-finite either way; which one depends on the kept class's
    /// recall: <c>(1.0 - 1.0) / 0.0 = 0.0 / 0.0 = NaN</c>, but
    /// <c>(0.5 - 1.0) / 0.0 = -0.5 / 0.0 = -Infinity</c>. Both measured against
    /// scikit-learn's <c>balanced_accuracy_score</c>, not guessed. A two-sample,
    /// single-class target is an ordinary accident for a caller to pass, not a
    /// contrived edge case.
    /// </summary>
    [Fact]
    public void Adjusted_divides_by_zero_when_a_single_class_is_kept()
    {
        Assert.True(double.IsNaN(BalancedAccuracy.Score([1, 1], [1, 1], adjusted: true)));
        Assert.True(double.IsNegativeInfinity(BalancedAccuracy.Score([0, 0], [0, 1], adjusted: true)));
    }

    /// <summary>
    /// scikit-learn's <c>balanced_accuracy_score</c> takes no <c>labels=</c>
    /// argument, so <c>Score(cm)</c> reaches ground it cannot: recall over exactly
    /// the classes the matrix holds, diagonal over its own row sum in the 3x3
    /// view. Labels [0, 2, 3] give true 0: 1/1, true 2: 2/3, true 3: dropped -&gt;
    /// mean(1.0, 2/3) = 5/6. Two other readings diverge: diagonal over
    /// <see cref="ConfusionMatrix.TrueSum"/>'s extended row sum (un-dropping
    /// label 3) gives 0.388888888889; a naive average reading the drop as 0
    /// gives 0.555555555556.
    /// </summary>
    [Fact]
    public void Reads_a_label_subset_matrix_over_just_the_kept_classes()
    {
        int[] yTrue = [0, 0, 2, 2, 2, 3, 1];
        int[] yPred = [0, 1, 2, 2, 0, 1, 1];
        int[] labels = [0, 2, 3];

        ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred, labels);

        Assert.Equal(5.0 / 6.0, BalancedAccuracy.Score(cm), 12);

        // Chance sits at one half with two classes kept, giving an adjusted score
        // of two thirds; extended row sums would keep all three, at a third.
        Assert.Equal(2.0 / 3.0, BalancedAccuracy.Score(cm, adjusted: true), 12);
    }
}
