using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The fraction of labels predicted wrongly — the equivalent of
/// <c>sklearn.metrics.hamming_loss</c>.
/// </summary>
/// <remarks>
/// On single-label input this is <c>1 - </c><see cref="Accuracy"/>. On a label matrix
/// it is **not**: it counts wrong *labels* where
/// <see cref="ZeroOneLoss"/> counts wrong *rows*, so a row with one label wrong out of
/// three costs a third here and a whole sample there.
/// </remarks>
public static class HammingLoss
{
    /// <summary>The fraction of labels wrong — <c>hamming_loss(y_true, y_pred, sample_weight=…)</c>.</summary>
    /// <param name="yTrue">The true labels, one per sample.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <returns><c>0</c> when every sample is right, <c>1</c> when none is.</returns>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, or the weights do not match.</exception>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
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

        return wrong / total;
    }

    /// <summary>The fraction of labels wrong over a label matrix — <c>hamming_loss</c> on 2-D input.</summary>
    /// <param name="yTrue">Whether each label holds, row-major: one row per sample, <paramref name="labelCount"/> values each.</param>
    /// <param name="yPred">The predicted labels, same shape as <paramref name="yTrue"/>.</param>
    /// <param name="labelCount">How many labels each row holds.</param>
    /// <param name="sampleWeight">A weight per <em>sample</em> — per row, not per label. Omit to weight every sample by 1.</param>
    /// <returns>The share of the <c>rows × labelCount</c> entries that disagree.</returns>
    /// <exception cref="ArgumentException">The shapes disagree, or the weights do not match the row count.</exception>
    public static double Score(
        ReadOnlySpan<bool> yTrue,
        ReadOnlySpan<bool> yPred,
        int labelCount,
        ReadOnlySpan<double> sampleWeight = default)
    {
        int rows = Multilabel.Validate(yTrue, yPred, labelCount, sampleWeight);

        double wrong = 0.0;
        double total = 0.0;
        for (int row = 0; row < rows; row++)
        {
            double weight = sampleWeight.IsEmpty ? 1.0 : sampleWeight[row];
            for (int label = 0; label < labelCount; label++)
            {
                int at = (row * labelCount) + label;
                if (yTrue[at] != yPred[at])
                {
                    wrong += weight;
                }

                total += weight;
            }
        }

        return wrong / total;
    }
}
