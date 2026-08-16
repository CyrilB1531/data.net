using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// Whether every sample of a class landed in the same cluster — the completeness of
/// <c>sklearn.metrics.homogeneity_completeness_v_measure</c>.
/// </summary>
public static class Completeness
{
    /// <summary>
    /// Scores how far each true class is kept together by the clustering —
    /// <c>sklearn.metrics.completeness_score(labels_true, labels_pred)</c>.
    /// </summary>
    /// <param name="labelsTrue">The reference labelling.</param>
    /// <param name="labelsPred">The labelling to score, same length as <paramref name="labelsTrue"/>.</param>
    /// <returns><c>1</c> when no class is split across clusters, <c>0</c> when the clustering says nothing about the classes.</returns>
    /// <remarks>
    /// The mirror of <see cref="Homogeneity"/>: this is that score with the two
    /// labellings exchanged, so putting every sample in one cluster scores <c>1</c>
    /// here and <c>0</c> there.
    /// </remarks>
    /// <exception cref="ArgumentException">The two labellings disagree in length.</exception>
    public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
    {
        // Validated here rather than inside, because the reversal below would otherwise
        // report the two lengths under each other's names.
        Contingency.Validate(labelsTrue, labelsPred);
        return Cluster.Homogeneity(labelsPred, labelsTrue);
    }
}
