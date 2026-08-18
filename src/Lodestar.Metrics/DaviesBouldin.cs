using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The Davies-Bouldin index — the equivalent of
/// <c>sklearn.metrics.davies_bouldin_score</c>.
/// </summary>
/// <remarks>
/// For each cluster, the worst ratio of "how spread these two are" to "how far apart
/// they sit", averaged over the clusters. **Lower is better**, unlike
/// <see cref="CalinskiHarabasz"/> and <see cref="Silhouette"/>, and <c>0</c> is the
/// floor.
/// </remarks>
public static class DaviesBouldin
{
    /// <summary>Scores a clustering from the samples themselves — <c>davies_bouldin_score(X, labels)</c>.</summary>
    /// <param name="labels">One cluster label per sample.</param>
    /// <param name="features">The samples, row-major: sample <c>i</c> occupies <c>featureCount</c> values from <c>i * featureCount</c>.</param>
    /// <param name="featureCount">How many values each sample holds.</param>
    /// <returns>The mean worst-case similarity between a cluster and any other, <c>0</c> or above. Lower is better.</returns>
    /// <remarks>
    /// No precomputed-distance form, for the reason
    /// <see cref="CalinskiHarabasz.Score"/> gives. Two clusters sharing a centroid
    /// contribute nothing rather than an infinity: the reference replaces a zero
    /// centroid distance with infinity before dividing, so the pair scores zero.
    /// </remarks>
    /// <exception cref="ArgumentException">The inputs disagree in length, or the number of distinct labels is outside <c>[2, n - 1]</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="featureCount"/> is not positive.</exception>
    public static double Score(ReadOnlySpan<int> labels, ReadOnlySpan<double> features, int featureCount)
    {
        int samples = Partition.Samples(labels, features, featureCount);
        int[] sizes = Partition.Sizes(labels, out int[] ordinals, out int clusters);
        Partition.RequireScorableCount(clusters, samples, nameof(labels));

        double[] centroids = Partition.Centroids(features, featureCount, ordinals, sizes, out _);

        // Each cluster's mean distance from its own centroid: the "spread" half of
        // every ratio below.
        var spread = new double[clusters];
        for (int sample = 0; sample < samples; sample++)
        {
            spread[ordinals[sample]] +=
                Partition.Euclidean(features, sample, centroids, ordinals[sample], featureCount);
        }

        for (int cluster = 0; cluster < clusters; cluster++)
        {
            spread[cluster] /= sizes[cluster];
        }

        double total = 0.0;
        for (int i = 0; i < clusters; i++)
        {
            double worst = 0.0;
            for (int j = 0; j < clusters; j++)
            {
                if (i == j)
                {
                    continue;
                }

                double apart = Partition.Euclidean(centroids, i, centroids, j, featureCount);

                // S1244: whether the two centroids coincide, which is what the reference
                // replaces with infinity before dividing -- the pair then contributes 0.
#pragma warning disable S1244
                double ratio = apart == 0.0 ? 0.0 : (spread[i] + spread[j]) / apart;
#pragma warning restore S1244
                if (ratio > worst)
                {
                    worst = ratio;
                }
            }

            total += worst;
        }

        return total / clusters;
    }
}
