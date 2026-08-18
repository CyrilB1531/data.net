using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The mean squared error of a probabilistic prediction — the equivalent of
/// <c>sklearn.metrics.brier_score_loss</c>.
/// </summary>
/// <remarks>
/// The other half of the calibration question <see cref="LogLoss"/> asks, and the
/// gentler half: a confident mistake costs at most 1 here, where the logarithm makes
/// it unbounded. Both are proper scoring rules, so both are minimised by telling the
/// truth; they disagree about how much a single overconfident sample should matter.
/// </remarks>
public static class BrierScore
{
    /// <summary>The binary case — <c>brier_score_loss(y_true, y_proba, pos_label=…, sample_weight=…)</c>.</summary>
    /// <param name="yTrue">The true labels, one per sample.</param>
    /// <param name="yProba">The probability of <paramref name="posLabel"/> for each sample, in <c>[0, 1]</c>.</param>
    /// <param name="posLabel">The label the probability is about. scikit-learn infers the greater of the two labels present, and refuses to guess for non-numeric labels; this asks.</param>
    /// <param name="scaleByHalf">Halve the two-class sum, which is what <c>scale_by_half='auto'</c> resolves to for a one-dimensional probability. <see langword="false"/> doubles the number.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <returns><c>0</c> for a perfect, perfectly confident prediction; at most <c>1</c> when <paramref name="scaleByHalf"/> holds.</returns>
    /// <exception cref="ArgumentException">The lengths disagree, the input is empty, or a probability falls outside <c>[0, 1]</c>.</exception>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yProba,
        int posLabel = 1,
        bool scaleByHalf = true,
        ReadOnlySpan<double> sampleWeight = default)
    {
        Probabilities.ValidateBinary(yTrue, yProba, sampleWeight);
        Probabilities.RequireProbabilities(yProba, "less than");

        CompensatedSum total = default;
        double weights = 0.0;
        for (int i = 0; i < yTrue.Length; i++)
        {
            double weight = sampleWeight.IsEmpty ? 1.0 : sampleWeight[i];
            double residual = (yTrue[i] == posLabel ? 1.0 : 0.0) - yProba[i];
            total.Add(weight * residual * residual);
            weights += weight;
        }

        double mean = total.Value / weights;
        return scaleByHalf ? mean : 2.0 * mean;
    }

    /// <summary>The multiclass case — <c>brier_score_loss(y_true, y_proba)</c> over a probability matrix.</summary>
    /// <param name="yTrue">The true class index of each sample, in <c>[0, classCount)</c>.</param>
    /// <param name="yProba">Class probabilities, row-major: sample 0's classes, then sample 1's.</param>
    /// <param name="classCount">How many classes each row scores.</param>
    /// <param name="scaleByHalf">Halve the sum over classes. <see langword="false"/>, the default, is what <c>scale_by_half='auto'</c> resolves to for a probability matrix.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <remarks>
    /// The default differs from <see cref="Score"/>'s deliberately, because
    /// <c>scale_by_half='auto'</c> reads the *shape*: it halves a one-dimensional
    /// binary probability and does not halve a matrix. Measured, one four-sample
    /// matrix scores 0.245 unhalved and 0.1225 halved.
    /// </remarks>
    /// <exception cref="ArgumentException">The shapes disagree, a label is not a class index, or a probability falls outside <c>[0, 1]</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="classCount"/> is below two.</exception>
    public static double MultiClass(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yProba,
        int classCount,
        bool scaleByHalf = false,
        ReadOnlySpan<double> sampleWeight = default)
    {
        int samples = Probabilities.Samples(yTrue, yProba, classCount, sampleWeight);
        Probabilities.RequireProbabilities(yProba, "less than");

        CompensatedSum total = default;
        double weights = 0.0;
        for (int i = 0; i < samples; i++)
        {
            double weight = sampleWeight.IsEmpty ? 1.0 : sampleWeight[i];
            double row = 0.0;
            for (int k = 0; k < classCount; k++)
            {
                double residual = (yTrue[i] == k ? 1.0 : 0.0) - yProba[(i * classCount) + k];
                row += residual * residual;
            }

            total.Add(weight * row);
            weights += weight;
        }

        double mean = total.Value / weights;
        return scaleByHalf ? mean / 2.0 : mean;
    }
}
