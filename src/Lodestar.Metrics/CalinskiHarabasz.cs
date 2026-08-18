using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The variance ratio criterion — the equivalent of
/// <c>sklearn.metrics.calinski_harabasz_score</c>.
/// </summary>
/// <remarks>
/// How far the clusters sit from each other against how spread they are inside,
/// corrected for the degrees of freedom each uses. Higher is better and there is no
/// upper bound, so it compares clusterings of one dataset rather than reading as a
/// quality on its own.
/// </remarks>
public static class CalinskiHarabasz
{
    /// <summary>Scores a clustering from the samples themselves — <c>calinski_harabasz_score(X, labels)</c>.</summary>
    /// <param name="labels">One cluster label per sample.</param>
    /// <param name="features">The samples, row-major: sample <c>i</c> occupies <c>featureCount</c> values from <c>i * featureCount</c>.</param>
    /// <param name="featureCount">How many values each sample holds.</param>
    /// <returns>The between-cluster dispersion over the within-cluster dispersion, each divided by its degrees of freedom. <c>1</c> when the clusters have no spread at all.</returns>
    /// <remarks>
    /// No precomputed-distance form, unlike <see cref="Silhouette"/>: this reads
    /// cluster centroids, and a distance matrix does not carry them. Euclidean only,
    /// because the reference offers nothing else here either.
    /// </remarks>
    /// <exception cref="ArgumentException">The inputs disagree in length, or the number of distinct labels is outside <c>[2, n - 1]</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="featureCount"/> is not positive.</exception>
    public static double Score(ReadOnlySpan<int> labels, ReadOnlySpan<double> features, int featureCount)
    {
        int samples = Partition.Samples(labels, features, featureCount);
        int[] sizes = Partition.Sizes(labels, out int[] ordinals, out int clusters);
        Partition.RequireScorableCount(clusters, samples, nameof(labels));

        double[] centroids = Partition.Centroids(features, featureCount, ordinals, sizes, out double[] overall);

        double between = 0.0;
        for (int cluster = 0; cluster < clusters; cluster++)
        {
            double distance = Partition.Euclidean(centroids, cluster, overall, 0, featureCount);
            between += sizes[cluster] * distance * distance;
        }

        double within = 0.0;
        for (int sample = 0; sample < samples; sample++)
        {
            double distance = Partition.Euclidean(features, sample, centroids, ordinals[sample], featureCount);
            within += distance * distance;
        }

        // S1244: whether the clusters have any spread at all, not whether two computed
        // quantities are close. The reference tests the same quantity against exact zero
        // and answers 1 rather than dividing -- measured, four identical points in two
        // clusters score 1.0.
#pragma warning disable S1244
        if (within == 0.0)
#pragma warning restore S1244
        {
            return 1.0;
        }

        return between * (samples - clusters) / (within * (clusters - 1));
    }
}
