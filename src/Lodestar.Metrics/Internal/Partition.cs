namespace Lodestar.Metrics.Internal;

/// <summary>
/// What the three internal-validity metrics share: reading a label vector as a
/// partition, and agreeing that a feature block really is <c>n × featureCount</c>.
/// </summary>
/// <remarks>
/// Extracted rather than copied a third time when Calinski-Harabasz and
/// Davies-Bouldin arrived (#192): all three refuse the same label counts, with the
/// same sentence, and a second copy of that bound is a second place for it to drift.
/// </remarks>
internal static class Partition
{
    /// <summary>Cluster sizes, plus a dense ordinal per sample in first-seen order.</summary>
    /// <param name="labels">One cluster label per sample; any integers, not necessarily contiguous.</param>
    /// <param name="ordinals">Filled with each sample's index into the returned sizes.</param>
    /// <param name="clusters">How many distinct labels occur.</param>
    public static int[] Sizes(ReadOnlySpan<int> labels, out int[] ordinals, out int clusters)
    {
        Dictionary<int, int> known = [];
        List<int> sizes = [];
        ordinals = new int[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            if (!known.TryGetValue(labels[i], out int ordinal))
            {
                ordinal = sizes.Count;
                known[labels[i]] = ordinal;
                sizes.Add(0);
            }

            ordinals[i] = ordinal;
            sizes[ordinal]++;
        }

        clusters = sizes.Count;
        return [.. sizes];
    }

    /// <summary>scikit-learn's own bound on how many clusters a validity score can read.</summary>
    /// <remarks>
    /// One cluster leaves nothing to compare against, and one cluster per sample
    /// leaves nothing inside one. Measured on 1.9.0: silhouette, Calinski-Harabasz
    /// and Davies-Bouldin refuse both, with the sentence reproduced here.
    /// </remarks>
    /// <exception cref="ArgumentException">The count is outside <c>[2, samples - 1]</c>.</exception>
    public static void RequireScorableCount(int clusters, int samples, string parameterName)
    {
        if (clusters < 2 || clusters > samples - 1)
        {
            throw new ArgumentException(
                $"Number of labels is {clusters}. Valid values are 2 to n_samples - 1 (inclusive)",
                parameterName);
        }
    }

    /// <summary>Checks a feature block against its labels and returns the sample count.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="featureCount"/> is not positive.</exception>
    /// <exception cref="ArgumentException">The block is not <c>labels.Length × featureCount</c>.</exception>
    public static int Samples(ReadOnlySpan<int> labels, ReadOnlySpan<double> features, int featureCount)
    {
        if (featureCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(featureCount), featureCount, "featureCount must be positive.");
        }

        if (features.Length != labels.Length * featureCount)
        {
            throw new ArgumentException(
                $"features holds {features.Length} values, which is not {labels.Length} samples " +
                $"of {featureCount}.",
                nameof(features));
        }

        return labels.Length;
    }

    /// <summary>Each cluster's centroid, and the centroid of everything, row-major.</summary>
    /// <param name="features">The samples, row-major.</param>
    /// <param name="featureCount">How many values each sample holds.</param>
    /// <param name="ordinals">Each sample's cluster ordinal.</param>
    /// <param name="sizes">How many samples each cluster holds.</param>
    /// <param name="overall">Filled with the mean of every sample.</param>
    public static double[] Centroids(
        ReadOnlySpan<double> features, int featureCount, int[] ordinals, int[] sizes, out double[] overall)
    {
        int clusters = sizes.Length;
        var centroids = new double[clusters * featureCount];
        overall = new double[featureCount];

        for (int sample = 0; sample < ordinals.Length; sample++)
        {
            int at = ordinals[sample] * featureCount;
            int from = sample * featureCount;
            for (int f = 0; f < featureCount; f++)
            {
                centroids[at + f] += features[from + f];
                overall[f] += features[from + f];
            }
        }

        for (int cluster = 0; cluster < clusters; cluster++)
        {
            int at = cluster * featureCount;
            for (int f = 0; f < featureCount; f++)
            {
                centroids[at + f] /= sizes[cluster];
            }
        }

        for (int f = 0; f < featureCount; f++)
        {
            overall[f] /= ordinals.Length;
        }

        return centroids;
    }

    /// <summary>The euclidean distance between two rows of two row-major blocks.</summary>
    public static double Euclidean(
        ReadOnlySpan<double> left, int leftRow, ReadOnlySpan<double> right, int rightRow, int featureCount)
    {
        double total = 0.0;
        int a = leftRow * featureCount;
        int b = rightRow * featureCount;
        for (int f = 0; f < featureCount; f++)
        {
            double delta = left[a + f] - right[b + f];
            total += delta * delta;
        }

        return Math.Sqrt(total);
    }
}
