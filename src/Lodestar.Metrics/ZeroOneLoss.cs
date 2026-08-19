using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The fraction of samples predicted wrongly — the equivalent of
/// <c>sklearn.metrics.zero_one_loss</c>.
/// </summary>
/// <remarks>
/// On single-label input this is <c>1 - </c><see cref="Accuracy"/> and agrees with
/// <see cref="HammingLoss"/>. On a label matrix the two part company: a row counts as
/// wrong here if **any** of its labels differs, so one wrong label of three costs a
/// whole sample rather than a third of one.
/// </remarks>
public static class ZeroOneLoss
{
    /// <summary>The fraction of samples wrong — <c>zero_one_loss(y_true, y_pred, normalize=…, sample_weight=…)</c>.</summary>
    /// <param name="yTrue">The true labels, one per sample.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="normalize">Divide by the total weight. <see langword="false"/> returns the weight of the wrong samples instead, as <c>normalize=False</c> does.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, or the weights do not match.</exception>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        bool normalize = true,
        ReadOnlySpan<double> sampleWeight = default)
    {
        Inputs.Validate(yTrue, yPred, sampleWeight);

        double wrong = 0.0;
        double total = 0.0;
        for (int i = 0; i < yTrue.Length; i++)
        {
            double weight = sampleWeight.IsEmpty ? 1.0 : sampleWeight[i];
            if (yTrue[i] != yPred[i])
            {
                wrong += weight;
            }

            total += weight;
        }

        return normalize ? wrong / total : wrong;
    }

    /// <summary>The same over a label matrix, where a row is wrong if any of its labels is.</summary>
    /// <param name="yTrue">Whether each label holds, row-major: one row per sample, <paramref name="labelCount"/> values each.</param>
    /// <param name="yPred">The predicted labels, same shape as <paramref name="yTrue"/>.</param>
    /// <param name="labelCount">How many labels each row holds.</param>
    /// <param name="normalize">Divide by the total weight. <see langword="false"/> returns the weight of the wrong rows.</param>
    /// <param name="sampleWeight">A weight per <em>sample</em> — per row, not per label.</param>
    /// <exception cref="ArgumentException">The shapes disagree, or the weights do not match the row count.</exception>
    public static double Score(
        ReadOnlySpan<bool> yTrue,
        ReadOnlySpan<bool> yPred,
        int labelCount,
        bool normalize = true,
        ReadOnlySpan<double> sampleWeight = default)
    {
        int rows = Multilabel.Validate(yTrue, yPred, labelCount, sampleWeight);

        double wrong = 0.0;
        double total = 0.0;
        for (int row = 0; row < rows; row++)
        {
            double weight = sampleWeight.IsEmpty ? 1.0 : sampleWeight[row];
            total += weight;

            for (int label = 0; label < labelCount; label++)
            {
                int at = (row * labelCount) + label;
                if (yTrue[at] != yPred[at])
                {
                    wrong += weight;
                    break;
                }
            }
        }

        return normalize ? wrong / total : wrong;
    }
}
