using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// Whether each cluster holds samples of a single class — the homogeneity of
/// <c>sklearn.metrics.homogeneity_completeness_v_measure</c>.
/// </summary>
public static class Homogeneity
{
    /// <summary>
    /// Scores how far each predicted cluster is made of one true class —
    /// <c>sklearn.metrics.homogeneity_score(labels_true, labels_pred)</c>.
    /// </summary>
    /// <param name="labelsTrue">The reference labelling.</param>
    /// <param name="labelsPred">The labelling to score, same length as <paramref name="labelsTrue"/>.</param>
    /// <returns><c>1</c> when every cluster is pure, <c>0</c> when the clustering says nothing about the classes.</returns>
    /// <remarks>
    /// Not symmetric, and that is the point: giving every sample its own cluster scores
    /// <c>1</c> here and <c>0.5</c> for <see cref="Completeness"/>, while merging
    /// everything into one cluster does the reverse. <see cref="VMeasure"/> is the pair
    /// read as one number.
    /// </remarks>
    /// <exception cref="ArgumentException">The two labellings disagree in length.</exception>
    public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred) =>
        Cluster.Homogeneity(labelsTrue, labelsPred);
}
