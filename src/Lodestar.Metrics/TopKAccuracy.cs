using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// How often the true class is among the highest-scoring few — the equivalent of
/// <c>sklearn.metrics.top_k_accuracy_score</c>.
/// </summary>
public static class TopKAccuracy
{
    /// <summary>Scores a multiclass prediction by whether the truth is in the top <c>k</c> — <c>sklearn.metrics.top_k_accuracy_score(y_true, y_score, k=…, normalize=…)</c>.</summary>
    /// <param name="yTrue">The true class of each sample, as an index into the score row.</param>
    /// <param name="yScore">The scores, row-major: one row per sample, <paramref name="classCount"/> values each.</param>
    /// <param name="classCount">How many classes each row scores.</param>
    /// <param name="k">How many of the highest-scoring classes count as a hit. <c>2</c> is scikit-learn's default.</param>
    /// <param name="normalize">When true (the default) return the fraction; when false, the number of hits.</param>
    /// <param name="sampleWeight">One weight per sample, or empty to weight each equally.</param>
    /// <returns>A fraction in <c>[0, 1]</c>, or a count when <paramref name="normalize"/> is false — measured, <c>3.0</c> on the corpus' first fixture at <c>k = 2</c>.</returns>
    /// <remarks>
    /// scikit-learn infers the class set from <c>y_true</c> and refuses a score row wider than
    /// what it found, unless given <c>labels</c>. Here the count is a parameter, so a class no
    /// sample happens to carry raises nothing — there is no inference to be wrong about.
    /// </remarks>
    /// <exception cref="ArgumentException">The inputs disagree in shape, <paramref name="classCount"/> is below 2, <paramref name="yTrue"/> names a class outside <c>[0, classCount)</c>, or <paramref name="sampleWeight"/> has the wrong length — or sums to zero while <paramref name="normalize"/> is true.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="k"/> is below 1.</exception>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yScore,
        int classCount,
        int k = 2,
        bool normalize = true,
        ReadOnlySpan<double> sampleWeight = default)
    {
        Validate(yTrue, yScore, classCount, k, sampleWeight);
        double hits = WeightedHits(yTrue, yScore, classCount, k, sampleWeight);

        // normalize: false never divides, so a zero-sum weight vector gives 0 here
        // where the fraction refuses it. The reference draws the same line.
        if (!normalize)
        {
            return hits;
        }

        return sampleWeight.Length == 0
            ? hits / yTrue.Length
            : hits / Weights.Sum(sampleWeight, throwOnZero: true);
    }

    /// <summary>Refuses the shapes scikit-learn refuses, before any row is read.</summary>
    private static void Validate(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yScore,
        int classCount,
        int k,
        ReadOnlySpan<double> sampleWeight)
    {
        if (k < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(k), k, "k must be at least 1.");
        }

        if (classCount < 2)
        {
            throw new ArgumentException(
                $"yScore holds {classCount} classes; scoring a top-k needs at least 2.",
                nameof(classCount));
        }

        if (yTrue.Length == 0)
        {
            throw new ArgumentException(
                "Found array with 0 sample(s) while a minimum of 1 is required.",
                nameof(yTrue));
        }

        if (yScore.Length != yTrue.Length * classCount)
        {
            throw new ArgumentException(
                $"yScore holds {yScore.Length} values, which is not {yTrue.Length} samples of " +
                $"{classCount}.",
                nameof(yScore));
        }

        Weights.Validate(sampleWeight, yTrue.Length, nameof(sampleWeight));
    }

    /// <summary>The weight of every sample whose true class is among the highest k scored.</summary>
    /// <remarks>
    /// A weight rather than a count: <c>top_k_accuracy_score(normalize=False)</c> adds the
    /// weights of the hits, which is <c>7.0</c> where the unweighted count is <c>3.0</c>.
    /// </remarks>
    private static double WeightedHits(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yScore,
        int classCount,
        int k,
        ReadOnlySpan<double> sampleWeight)
    {
        double hits = 0.0;
        for (int sample = 0; sample < yTrue.Length; sample++)
        {
            int trueClass = yTrue[sample];

            // Silently a miss otherwise, which reads as a bad model rather than as the
            // caller error it is. scikit-learn refuses the same input, by inference.
            if (trueClass < 0 || trueClass >= classCount)
            {
                throw new ArgumentException(
                    $"yTrue holds the class {trueClass}, which is not an index into a row of " +
                    $"{classCount}.",
                    nameof(yTrue));
            }

            int[] order = Ranking.Descending(yScore.Slice(sample * classCount, classCount));
            for (int rank = 0; rank < k && rank < classCount; rank++)
            {
                if (order[rank] == trueClass)
                {
                    hits += sampleWeight.Length == 0 ? 1.0 : sampleWeight[sample];
                    break;
                }
            }
        }

        return hits;
    }
}
