using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// How well each sample sits in its own cluster rather than the nearest other one —
/// the equivalent of <c>sklearn.metrics.silhouette_score</c>.
/// </summary>
public static class Silhouette
{
    /// <summary>Scores a clustering from the samples themselves, with the euclidean distance — <c>sklearn.metrics.silhouette_score(X, labels)</c>.</summary>
    /// <param name="labels">One cluster label per sample.</param>
    /// <param name="features">The samples, row-major: sample <c>i</c> occupies <c>featureCount</c> values from <c>i * featureCount</c>.</param>
    /// <param name="featureCount">How many values each sample holds.</param>
    /// <returns>The mean of <see cref="PerSample"/>, in <c>[-1, 1]</c>.</returns>
    /// <remarks>
    /// Euclidean only. scikit-learn accepts some twenty <c>metric=</c> names, and each one admitted
    /// here would be a parity claim to prove and keep; a caller who wants another computes the
    /// matrix and passes it to the method that takes one. Both paths run the same arithmetic on the
    /// same distances, and the corpus checks they agree at <c>1e-9</c> on every case.
    /// </remarks>
    /// <exception cref="ArgumentException">The inputs disagree in length, or the number of distinct labels is outside <c>[2, n - 1]</c>.</exception>
    public static double Score(ReadOnlySpan<int> labels, ReadOnlySpan<double> features, int featureCount) =>
        Mean(PerSample(labels, features, featureCount));

    /// <summary>Scores a clustering from a distance matrix you already have — <c>sklearn.metrics.silhouette_score(D, labels, metric='precomputed')</c>.</summary>
    /// <param name="labels">One cluster label per sample.</param>
    /// <param name="distances">The matrix row-major: <c>distances[(i * n) + j]</c> is the distance from sample <c>i</c> to sample <c>j</c>.</param>
    /// <returns>The mean of <see cref="PerSampleFromDistances"/>, in <c>[-1, 1]</c>.</returns>
    /// <remarks>
    /// A name of its own rather than an overload: the two entry points would otherwise have the
    /// same signature, since a matrix and a feature block are both a span of <c>double</c> with a
    /// count. That is the ruling of decision 0021, applied to an input rather than to a return.
    /// </remarks>
    /// <exception cref="ArgumentException">The matrix is not <c>n × n</c> for the labels given, or the number of distinct labels is outside <c>[2, n - 1]</c>.</exception>
    public static double ScoreFromDistances(ReadOnlySpan<int> labels, ReadOnlySpan<double> distances) =>
        Mean(PerSampleFromDistances(labels, distances));

    /// <summary>The score of each sample rather than their mean — <c>sklearn.metrics.silhouette_samples(X, labels)</c>.</summary>
    /// <param name="labels">One cluster label per sample.</param>
    /// <param name="features">The samples, row-major.</param>
    /// <param name="featureCount">How many values each sample holds.</param>
    /// <returns>One value per sample, in the order the samples were given.</returns>
    /// <remarks>
    /// This is what makes silhouette a diagnostic rather than a number: a mean near zero says the
    /// clustering is mediocre, and this says which samples are on the wrong side of a boundary.
    /// </remarks>
    /// <exception cref="ArgumentException">The inputs disagree in length, or the number of distinct labels is outside <c>[2, n - 1]</c>.</exception>
    public static double[] PerSample(ReadOnlySpan<int> labels, ReadOnlySpan<double> features, int featureCount)
    {
        int samples = Partition.Samples(labels, features, featureCount);
        if ((long)samples * samples > int.MaxValue)
        {
            throw new ArgumentException(
                $"{samples} samples need a {(long)samples * samples}-element distance matrix, " +
                "which does not fit an array. Cluster a sample of them, or pass distances you hold.",
                nameof(labels));
        }

        double[] distances = new double[samples * samples];
        for (int i = 0; i < samples; i++)
        {
            for (int j = i + 1; j < samples; j++)
            {
                double distance = Euclidean(features, featureCount, i, j);
                distances[(i * samples) + j] = distance;
                distances[(j * samples) + i] = distance;
            }
        }

        return PerSampleFromDistances(labels, distances);
    }

    /// <summary>The score of each sample, from a distance matrix — <c>sklearn.metrics.silhouette_samples(D, labels, metric='precomputed')</c>.</summary>
    /// <param name="labels">One cluster label per sample.</param>
    /// <param name="distances">The <c>n × n</c> matrix of pairwise distances, row-major.</param>
    /// <returns>One value per sample, in the order the samples were given.</returns>
    /// <remarks>
    /// A cluster holding a single sample scores that sample <c>0.0</c>, which is scikit-learn's
    /// answer rather than a division by zero: there is no other member to be close to, and the
    /// sample is neither well nor badly placed.
    /// </remarks>
    /// <exception cref="ArgumentException">The matrix is not <c>n × n</c> for the labels given, or the number of distinct labels is outside <c>[2, n - 1]</c>.</exception>
    public static double[] PerSampleFromDistances(ReadOnlySpan<int> labels, ReadOnlySpan<double> distances)
    {
        int samples = Square(labels, distances);
        int[] sizes = Partition.Sizes(labels, out int[] ordinals, out int clusters);
        Partition.RequireScorableCount(clusters, samples, nameof(labels));

        double[] scores = new double[samples];
        double[] sums = new double[clusters];
        for (int i = 0; i < samples; i++)
        {
            Array.Clear(sums, 0, clusters);
            for (int j = 0; j < samples; j++)
            {
                sums[ordinals[j]] += distances[(i * samples) + j];
            }

            scores[i] = Score(sums, sizes, ordinals[i]);
        }

        return scores;
    }

    /// <summary>One sample's score: how far it sits from its own cluster against the nearest other.</summary>
    private static double Score(double[] sums, int[] sizes, int own)
    {
        if (sizes[own] == 1)
        {
            return 0.0;
        }

        double inside = sums[own] / (sizes[own] - 1);
        double nearest = double.PositiveInfinity;
        for (int cluster = 0; cluster < sums.Length; cluster++)
        {
            if (cluster != own)
            {
                nearest = Math.Min(nearest, sums[cluster] / sizes[cluster]);
            }
        }

        double scale = Math.Max(inside, nearest);

        // Coincident samples put 0 over 0. scikit-learn runs the same expression through
        // nan_to_num, so duplicate rows score 0 rather than NaN -- measured on 1.9.0.
        return scale <= 0.0 ? 0.0 : (nearest - inside) / scale;
    }

    private static double Mean(double[] scores)
    {
        double total = 0.0;
        foreach (double score in scores)
        {
            total += score;
        }

        return total / scores.Length;
    }

    private static double Euclidean(ReadOnlySpan<double> features, int featureCount, int left, int right)
    {
        double total = 0.0;
        for (int f = 0; f < featureCount; f++)
        {
            double difference = features[(left * featureCount) + f] - features[(right * featureCount) + f];
            total += difference * difference;
        }

        return Math.Sqrt(total);
    }

    private static int Square(ReadOnlySpan<int> labels, ReadOnlySpan<double> distances)
    {
        if (distances.Length != labels.Length * labels.Length)
        {
            throw new ArgumentException(
                $"distances holds {distances.Length} values, which is not {labels.Length} squared.",
                nameof(distances));
        }

        return labels.Length;
    }
}
