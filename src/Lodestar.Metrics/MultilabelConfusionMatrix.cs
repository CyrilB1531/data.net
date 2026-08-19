using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// One 2×2 confusion matrix per label, or per sample — the equivalent of
/// <c>sklearn.metrics.multilabel_confusion_matrix</c>.
/// </summary>
/// <remarks>
/// A stack of <see cref="ConfusionMatrix"/> rather than a type of its own: an entry is
/// one class counted against everything else, which a two-label matrix already is. Its
/// labels are <c>0</c> and <c>1</c> in that order, so the cells land where the
/// reference puts them — true negative, false positive, false negative, true positive.
/// </remarks>
public static class MultilabelConfusionMatrix
{
    /// <summary>One matrix per class, each the class against everything else — <c>multilabel_confusion_matrix(y_true, y_pred, labels=…, sample_weight=…)</c>.</summary>
    /// <param name="yTrue">The true labels, one per sample.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="labels">The classes to report and their order. Omit for the sorted union of both inputs.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <returns>One matrix per class, in label order.</returns>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, or the weights do not match.</exception>
    public static ConfusionMatrix[] Compute(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default)
    {
        Inputs.Validate(yTrue, yPred, sampleWeight);

        int[] classes = labels.IsEmpty ? Union(yTrue, yPred) : labels.ToArray();
        var stack = new ConfusionMatrix[classes.Length];
        int samples = yTrue.Length;
        int[] binaryTrue = new int[samples];
        int[] binaryPred = new int[samples];

        for (int i = 0; i < classes.Length; i++)
        {
            for (int sample = 0; sample < samples; sample++)
            {
                binaryTrue[sample] = yTrue[sample] == classes[i] ? 1 : 0;
                binaryPred[sample] = yPred[sample] == classes[i] ? 1 : 0;
            }

            stack[i] = OneVersusRest(binaryTrue, binaryPred, sampleWeight);
        }

        return stack;
    }

    /// <summary>One matrix per label of a matrix, or per sample when <paramref name="samplewise"/> holds.</summary>
    /// <param name="yTrue">Whether each label holds, row-major: one row per sample, <paramref name="labelCount"/> values each.</param>
    /// <param name="yPred">The predicted labels, same shape as <paramref name="yTrue"/>.</param>
    /// <param name="labelCount">How many labels each row holds.</param>
    /// <param name="samplewise">Count one matrix per <em>sample</em> instead of per label. The reference offers this on a matrix only, and refuses it on single-label input.</param>
    /// <param name="sampleWeight">A weight per <em>sample</em> — per row, not per label.</param>
    /// <returns>One matrix per label in column order, or per sample in row order.</returns>
    /// <exception cref="ArgumentException">The shapes disagree, or the weights do not match the row count.</exception>
    public static ConfusionMatrix[] Compute(
        ReadOnlySpan<bool> yTrue,
        ReadOnlySpan<bool> yPred,
        int labelCount,
        bool samplewise = false,
        ReadOnlySpan<double> sampleWeight = default)
    {
        int rows = Multilabel.Validate(yTrue, yPred, labelCount, sampleWeight);
        return samplewise
            ? PerSample(yTrue, yPred, labelCount, rows, sampleWeight)
            : PerLabel(yTrue, yPred, labelCount, rows, sampleWeight);
    }

    /// <summary>One matrix per label column, counting samples.</summary>
    private static ConfusionMatrix[] PerLabel(
        ReadOnlySpan<bool> yTrue, ReadOnlySpan<bool> yPred, int labelCount, int rows,
        ReadOnlySpan<double> sampleWeight)
    {
        var stack = new ConfusionMatrix[labelCount];
        int[] binaryTrue = new int[rows];
        int[] binaryPred = new int[rows];

        for (int label = 0; label < labelCount; label++)
        {
            for (int row = 0; row < rows; row++)
            {
                int at = (row * labelCount) + label;
                binaryTrue[row] = yTrue[at] ? 1 : 0;
                binaryPred[row] = yPred[at] ? 1 : 0;
            }

            stack[label] = OneVersusRest(binaryTrue, binaryPred, sampleWeight);
        }

        return stack;
    }

    /// <summary>One matrix per row, counting that row's labels under its own weight.</summary>
    private static ConfusionMatrix[] PerSample(
        ReadOnlySpan<bool> yTrue, ReadOnlySpan<bool> yPred, int labelCount, int rows,
        ReadOnlySpan<double> sampleWeight)
    {
        var stack = new ConfusionMatrix[rows];
        int[] binaryTrue = new int[labelCount];
        int[] binaryPred = new int[labelCount];

        for (int row = 0; row < rows; row++)
        {
            for (int label = 0; label < labelCount; label++)
            {
                int at = (row * labelCount) + label;
                binaryTrue[label] = yTrue[at] ? 1 : 0;
                binaryPred[label] = yPred[at] ? 1 : 0;
            }

            stack[row] = OneVersusRest(binaryTrue, binaryPred, RepeatedWeight(sampleWeight, row, labelCount));
        }

        return stack;
    }

    /// <summary>A two-label matrix over <c>[0, 1]</c>, which is the layout the reference uses.</summary>
    private static ConfusionMatrix OneVersusRest(
        int[] binaryTrue, int[] binaryPred, ReadOnlySpan<double> sampleWeight) =>
        ConfusionMatrix.Compute(binaryTrue, binaryPred, [0, 1], sampleWeight);

    /// <summary>One row's weight, repeated across its labels.</summary>
    private static double[] RepeatedWeight(ReadOnlySpan<double> sampleWeight, int row, int labelCount)
    {
        if (sampleWeight.IsEmpty)
        {
            return [];
        }

        var repeated = new double[labelCount];
        for (int i = 0; i < labelCount; i++)
        {
            repeated[i] = sampleWeight[row];
        }

        return repeated;
    }

    /// <summary>The sorted set of labels either input carries.</summary>
    private static int[] Union(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred)
    {
        var seen = new SortedSet<int>();
        for (int i = 0; i < yTrue.Length; i++)
        {
            seen.Add(yTrue[i]);
            seen.Add(yPred[i]);
        }

        return [.. seen];
    }
}
