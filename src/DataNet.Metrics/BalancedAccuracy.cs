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
    /// The average runs over the classes that have at least one true sample, not
    /// over every class — which is scikit-learn's rule and the whole of this
    /// metric's degenerate case. A class that is predicted but never true has an
    /// undefined recall and is dropped; reading that recall as 0 and averaging
    /// over every class gives a different, smaller number. <paramref name="adjusted"/>
    /// divides by the count of the classes that were <em>kept</em> for the same
    /// reason. With exactly one class kept, chance is 1 and the adjusted score is
    /// <c>0.0 / 0.0</c> — <see cref="double.NaN"/> under IEEE 754, which is what
    /// scikit-learn itself returns for <c>balanced_accuracy_score([1,1],[1,1], adjusted=True)</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="cm"/> is null.</exception>
    public static double Score(ConfusionMatrix cm, bool adjusted = false)
    {
        Guard.NotNull(cm);

        double[] perClass = Recall.PerClass(cm, ZeroDivision.NaN);
        double sum = 0.0;
        int kept = 0;

        // SonarLint S3267 wants this rewritten with Where/Sum() and a separate
        // Where/Count(), which this codebase avoids on paths like this one:
        // Score(cm) runs once per call, on the same benchmarked path a later
        // task gates on beating scikit-learn's processor time, and needs both
        // the sum and the count of the kept classes from the very same pass.
        // Two LINQ pipelines would walk perClass twice and allocate an
        // iterator and a delegate call per element; a plain loop does neither.
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
}
