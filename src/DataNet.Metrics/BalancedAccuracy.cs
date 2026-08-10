using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// Recall averaged over the classes rather than over the samples — the
/// equivalent of <c>sklearn.metrics.balanced_accuracy_score</c>.
/// </summary>
public static class BalancedAccuracy
{
    /// <summary>Balanced accuracy read off an existing matrix (<c>balanced_accuracy_score</c>).</summary>
    /// <param name="cm">The matrix to read.</param>
    /// <param name="adjusted">When true, rescale so that chance scores 0 and a perfect score stays 1.</param>
    /// <remarks>
    /// <para>
    /// The average runs over the classes that have at least one true sample, not
    /// over every class — which is scikit-learn's rule and the whole of this
    /// metric's degenerate case. A class that is predicted but never true has an
    /// undefined recall and is dropped; reading that recall as 0 and averaging
    /// over every class gives a different, smaller number. <paramref name="adjusted"/>
    /// divides by the count of the classes that were <em>kept</em> for the same
    /// reason.
    /// </para>
    /// <para>
    /// This overload scores exactly the classes <paramref name="cm"/> holds:
    /// each recall is a diagonal cell over that class's own row sum inside the
    /// <see cref="ConfusionMatrix.Labels"/>-sized view, so a matrix built with an
    /// explicit label subset contributes none of the samples it dropped — not
    /// even to a denominator. That is deliberately unlike <see cref="Recall"/>,
    /// whose denominator is scikit-learn's <c>true_sum</c> over every observed
    /// label; on a matrix that dropped nothing the two agree exactly.
    /// </para>
    /// <para>
    /// With exactly one class kept the rescale divides by zero, because chance is
    /// 1. <paramref name="adjusted"/> then returns <see cref="double.NaN"/> when
    /// that class's recall is exactly 1 — <c>0.0 / 0.0</c> under IEEE 754 — and
    /// <see cref="double.NegativeInfinity"/> for any lower recall, a negative
    /// numerator over zero. scikit-learn returns those same two values for the
    /// same inputs: <c>balanced_accuracy_score([1,1], [1,1], adjusted=True)</c>
    /// is <c>nan</c> and <c>balanced_accuracy_score([0,0], [0,1], adjusted=True)</c>
    /// is <c>-inf</c>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="cm"/> is null.</exception>
    public static double Score(ConfusionMatrix cm, bool adjusted = false)
    {
        Guard.NotNull(cm);

        double[] perClass = PerClassRecall(cm);
        double sum = 0.0;
        int kept = 0;

        // SonarLint S3267 asks for the condition to move into a LINQ Where, and a
        // single "foreach (double recall in perClass.Where(r => !double.IsNaN(r)))"
        // does silence the rule in one pass — measured, not assumed. It is
        // declined for what it costs, not because it cannot be written: an
        // iterator allocation plus one delegate invocation per class, on the
        // path whose merge gate is beating scikit-learn's processor time. A plain
        // loop pays neither and reads the same.
#pragma warning disable S3267
        foreach (double recall in perClass)
        {
            if (!double.IsNaN(recall))
            {
                sum += recall;
                kept++;
            }
        }
#pragma warning restore S3267

        double score = sum / kept;
        if (!adjusted)
        {
            return score;
        }

        double chance = 1.0 / kept;
        return (score - chance) / (1.0 - chance);
    }

    /// <summary>Balanced accuracy straight from the labels, counting the matrix on the way.</summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="adjusted">When true, rescale so that chance scores 0.</param>
    /// <param name="labels">The label set and its order. Omit for the sorted union of both inputs.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <exception cref="ArgumentException">The inputs disagree in length or are empty.</exception>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        bool adjusted = false,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default) =>
        Score(ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight), adjusted);

    // Recall per class read off the Size × Size view: the diagonal cell over
    // MatrixSums' row sum for that class, NaN where the row carries no weight so
    // the average above drops the class exactly as scikit-learn does.
    //
    // Deliberately not Recall.PerClass. That denominator is Prf.Support, which is
    // ConfusionMatrix.TrueSum — accumulated over every sample whose true label
    // was requested, including samples whose *predicted* label fell outside the
    // label set. Dividing this k × k diagonal by that extended row sum would
    // score a label-subset matrix partly on samples the matrix does not contain,
    // which is the one thing Score(ConfusionMatrix) must not do. With no explicit
    // labels Stride == Size, nothing is dropped, and the two denominators are the
    // same total.
    private static double[] PerClassRecall(ConfusionMatrix cm)
    {
        int k = cm.Size;
        int stride = cm.Stride;
        ReadOnlySpan<double> cells = cm.Cells;

        // colSums is filled and unread: MatrixSums.Compute is the one pass all
        // three of the matrix-restricted metrics share, and a k-element array is
        // not worth a second entry point to avoid.
        double[] rowSums = new double[k];
        double[] colSums = new double[k];
        MatrixSums.Compute(cm, rowSums, colSums, out _, out _);

        double[] perClass = new double[k];
        for (int i = 0; i < k; i++)
        {
            // Prf.Divide rather than a bare division: the zero-denominator test
            // is scikit-learn's own _prf_divide, and routing through it keeps the
            // exact-zero comparison and its reasoning in one place.
            perClass[i] = Prf.Divide(cells[(i * stride) + i], rowSums[i], ZeroDivision.NaN, "Recall");
        }

        return perClass;
    }
}
