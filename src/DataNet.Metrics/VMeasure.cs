using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// Homogeneity and completeness as one number, their harmonic mean — the V-measure of
/// <c>sklearn.metrics.homogeneity_completeness_v_measure</c>.
/// </summary>
public static class VMeasure
{
    /// <summary>
    /// Scores a clustering on both counts at once —
    /// <c>sklearn.metrics.v_measure_score(labels_true, labels_pred)</c>.
    /// </summary>
    /// <param name="labelsTrue">The reference labelling.</param>
    /// <param name="labelsPred">The labelling to score, same length as <paramref name="labelsTrue"/>.</param>
    /// <returns>The harmonic mean of <see cref="Homogeneity"/> and <see cref="Completeness"/>, or <c>0</c> when both are.</returns>
    /// <remarks>
    /// scikit-learn's <c>beta</c> is not reproduced: its default of <c>1</c> is the harmonic mean,
    /// and no oracle row exists for another value. Equal to
    /// <see cref="NormalizedMutualInformation"/> by construction, which the corpus shows per case.
    /// </remarks>
    /// <exception cref="ArgumentException">The two labellings disagree in length.</exception>
    public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
    {
        double homogeneity = Cluster.Homogeneity(labelsTrue, labelsPred);
        double completeness = Cluster.Homogeneity(labelsPred, labelsTrue);
        return homogeneity + completeness <= 0.0
            ? 0.0
            : 2.0 * homogeneity * completeness / (homogeneity + completeness);
    }
}
