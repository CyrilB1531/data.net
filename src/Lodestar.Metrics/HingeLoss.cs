using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The hinge loss of a decision function — the equivalent of
/// <c>sklearn.metrics.hinge_loss</c>.
/// </summary>
/// <remarks>
/// The only metric here reading a <b>decision function</b> — the signed distance from
/// a boundary, not a label and not a probability. A sample costs nothing once it is
/// right by a margin of 1, and rises linearly below that: the loss an SVM minimises.
/// </remarks>
public static class HingeLoss
{
    /// <summary>The binary case — <c>hinge_loss(y_true, pred_decision, sample_weight=…)</c>.</summary>
    /// <param name="yTrue">The true labels, one per sample.</param>
    /// <param name="predDecision">The decision value per sample: positive on <paramref name="posLabel"/>'s side of the boundary, and further from zero the more confident.</param>
    /// <param name="posLabel">The label on the positive side. scikit-learn infers the two classes; this asks.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <returns><c>0</c> when every sample sits on the right side by a margin of at least 1, and unbounded above.</returns>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, or the weights do not match.</exception>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> predDecision,
        int posLabel = 1,
        ReadOnlySpan<double> sampleWeight = default)
    {
        Probabilities.ValidateBinary(yTrue, predDecision, sampleWeight);

        CompensatedSum total = default;
        double weights = 0.0;
        for (int i = 0; i < yTrue.Length; i++)
        {
            double weight = sampleWeight.IsEmpty ? 1.0 : sampleWeight[i];
            double sign = yTrue[i] == posLabel ? 1.0 : -1.0;
            total.Add(weight * Hinge(sign * predDecision[i]));
            weights += weight;
        }

        return total.Value / weights;
    }

    /// <summary>The multiclass case — <c>hinge_loss(y_true, pred_decision)</c> over one decision per class.</summary>
    /// <param name="yTrue">The true class index of each sample, in <c>[0, classCount)</c>.</param>
    /// <param name="predDecision">One decision per class, row-major: sample 0's classes, then sample 1's.</param>
    /// <param name="classCount">How many classes each row scores.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <remarks>
    /// The margin is the true class's decision less the best of the others, so a
    /// sample costs nothing once its own class wins by 1. Crammer and Singer's
    /// multiclass hinge, which is what the reference computes.
    /// </remarks>
    /// <exception cref="ArgumentException">The shapes disagree, or a label is not a class index.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="classCount"/> is below two.</exception>
    public static double MultiClass(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> predDecision,
        int classCount,
        ReadOnlySpan<double> sampleWeight = default)
    {
        int samples = Probabilities.Samples(yTrue, predDecision, classCount, sampleWeight);

        CompensatedSum total = default;
        double weights = 0.0;
        for (int i = 0; i < samples; i++)
        {
            double weight = sampleWeight.IsEmpty ? 1.0 : sampleWeight[i];
            int at = i * classCount;
            double own = predDecision[at + yTrue[i]];

            double best = double.NegativeInfinity;
            for (int k = 0; k < classCount; k++)
            {
                if (k != yTrue[i] && predDecision[at + k] > best)
                {
                    best = predDecision[at + k];
                }
            }

            total.Add(weight * Hinge(own - best));
            weights += weight;
        }

        return total.Value / weights;
    }

    /// <summary>What one margin costs: nothing at 1 or above, and rising linearly below it.</summary>
    private static double Hinge(double margin)
    {
        double cost = 1.0 - margin;
        return cost > 0.0 ? cost : 0.0;
    }
}
