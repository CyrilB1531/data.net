using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The cross-entropy of a probabilistic prediction — the equivalent of
/// <c>sklearn.metrics.log_loss</c>.
/// </summary>
/// <remarks>
/// Answers "was the confidence honest", where <see cref="Accuracy"/> answers "was the
/// prediction right" and <see cref="RocAuc"/> answers "was the ranking right". `0` is
/// a perfect, perfectly confident prediction and it is unbounded above — a single
/// confident mistake dominates the whole average, which is what the metric is for.
/// </remarks>
public static class LogLoss
{
    /// <summary>The binary case — <c>log_loss(y_true, y_proba, normalize=…, sample_weight=…)</c>.</summary>
    /// <param name="yTrue">The true labels, one per sample.</param>
    /// <param name="yProba">The probability of <paramref name="posLabel"/> for each sample, in <c>[0, 1]</c>.</param>
    /// <param name="posLabel">The label the probability is about. scikit-learn infers the greater of the two labels present; this asks.</param>
    /// <param name="normalize">Divide by the total weight. <see langword="false"/> returns the sum, as <c>normalize=False</c> does.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <returns><c>0</c> for a perfect prediction — to within one epsilon, since the clip below never lets a logarithm reach zero.</returns>
    /// <exception cref="ArgumentException">The lengths disagree, the input is empty, or a probability falls outside <c>[0, 1]</c>.</exception>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yProba,
        int posLabel = 1,
        bool normalize = true,
        ReadOnlySpan<double> sampleWeight = default)
    {
        Probabilities.ValidateBinary(yTrue, yProba, sampleWeight);
        Probabilities.RequireProbabilities(yProba, "lower than");

        CompensatedSum total = default;
        double weights = 0.0;
        for (int i = 0; i < yTrue.Length; i++)
        {
            double weight = sampleWeight.IsEmpty ? 1.0 : sampleWeight[i];
            double p = Probabilities.Clip(yTrue[i] == posLabel ? yProba[i] : 1.0 - yProba[i]);
            total.Add(weight * -Math.Log(p));
            weights += weight;
        }

        return normalize ? total.Value / weights : total.Value;
    }

    /// <summary>The multiclass case — <c>log_loss(y_true, y_proba)</c> over a probability matrix.</summary>
    /// <param name="yTrue">The true class index of each sample, in <c>[0, classCount)</c>.</param>
    /// <param name="yProba">Class probabilities, row-major: sample 0's classes, then sample 1's.</param>
    /// <param name="classCount">How many classes each row scores.</param>
    /// <param name="normalize">Divide by the total weight. <see langword="false"/> returns the sum.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <remarks>
    /// A row that does not sum to 1 is **not** refused and **not** renormalised: the
    /// reference warns and scores the values as given, and this reproduces the number
    /// in silence. Measured, halving every row of a four-sample matrix takes the loss
    /// from 0.5017337127232719 to 1.1948808932832173 rather than leaving it alone.
    /// </remarks>
    /// <exception cref="ArgumentException">The shapes disagree, a label is not a class index, or a probability falls outside <c>[0, 1]</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="classCount"/> is below two.</exception>
    public static double MultiClass(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yProba,
        int classCount,
        bool normalize = true,
        ReadOnlySpan<double> sampleWeight = default)
    {
        int samples = Probabilities.Samples(yTrue, yProba, classCount, sampleWeight);
        Probabilities.RequireProbabilities(yProba, "lower than");

        CompensatedSum total = default;
        double weights = 0.0;
        for (int i = 0; i < samples; i++)
        {
            double weight = sampleWeight.IsEmpty ? 1.0 : sampleWeight[i];
            double p = Probabilities.Clip(yProba[(i * classCount) + yTrue[i]]);
            total.Add(weight * -Math.Log(p));
            weights += weight;
        }

        return normalize ? total.Value / weights : total.Value;
    }
}
