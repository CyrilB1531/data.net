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
    /// <param name="adjusted">When true, rescale so that chance scores 0 and a perfect score stays 1; see docs/decisions/0029 for what a single kept class returns instead.</param>
    /// <remarks>
    /// The average runs over the classes <paramref name="cm"/> kept — those with
    /// at least one true sample — not every class it might have been asked for;
    /// see docs/decisions/0029. Each recall divides by its own row sum in the
    /// <see cref="ConfusionMatrix.Labels"/>-sized view, unlike <see cref="Recall"/>,
    /// whose denominator is scikit-learn's <c>true_sum</c> over every observed
    /// label (see <see cref="ConfusionMatrix.Stride"/>); the two agree when
    /// nothing was dropped.
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

    // NaN drops an unweighted row, as scikit-learn does. Deliberately not
    // Recall.PerClass: its Stride-sized denominator would score samples this label-subset matrix does not contain.
    private static double[] PerClassRecall(ConfusionMatrix cm)
    {
        int k = cm.Size;
        int stride = cm.Stride;
        ReadOnlySpan<double> cells = cm.Cells;

        // colSums is filled and unread: MatrixSums.Compute is the one pass the
        // three matrix-restricted metrics share; not worth a second entry point.
        double[] rowSums = new double[k];
        double[] colSums = new double[k];
        MatrixSums.Compute(cm, rowSums, colSums, out _, out _);

        double[] perClass = new double[k];
        for (int i = 0; i < k; i++)
        {
            // Prf.Divide rather than a bare division, so the zero-denominator
            // test stays scikit-learn's own _prf_divide, defined once for all three.
            perClass[i] = Prf.Divide(cells[(i * stride) + i], rowSums[i], ZeroDivision.NaN, "Recall");
        }

        return perClass;
    }
}
