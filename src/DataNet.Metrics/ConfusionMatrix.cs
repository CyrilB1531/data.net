using System.Collections.ObjectModel;
using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// A confusion matrix over predicted labels — the equivalent of
/// <c>sklearn.metrics.confusion_matrix</c>, and the shared engine every other
/// metric in this package derives from.
/// </summary>
/// <remarks>
/// <para>
/// Rows are true labels and columns are predicted ones, which is scikit-learn's
/// orientation. Computing the matrix once and asking it for several metrics
/// costs one pass; calling the scalar helpers separately counts once each.
/// </para>
/// <para>
/// Counts are <see cref="double"/> rather than <see cref="int"/> because
/// <c>sampleWeight</c> is supported throughout. Unweighted counts are exact:
/// a <see cref="double"/> represents every integer up to 2^53.
/// </para>
/// </remarks>
public sealed class ConfusionMatrix
{
    private readonly double[] _cells;
    private readonly int[] _labels;
    private readonly ReadOnlyCollection<int> _labelView;

    private ConfusionMatrix(
        double[] cells, int[] labels, double totalWeight, bool weighted, bool dropped, bool explicitLabels)
    {
        _cells = cells;
        _labels = labels;
        _labelView = Array.AsReadOnly(labels);
        TotalWeight = totalWeight;
        IsWeighted = weighted;
        DroppedSamples = dropped;
        ExplicitLabels = explicitLabels;
    }

    /// <summary>The labels, in the order rows and columns use.</summary>
    /// <remarks>
    /// The ascending sorted union of both inputs when <c>labels</c> was omitted;
    /// otherwise the caller's order, left unsorted — scikit-learn's rule.
    /// </remarks>
    public IReadOnlyList<int> Labels => _labelView;

    /// <summary>The total weight the matrix counted (the sample count when unweighted).</summary>
    public double TotalWeight { get; }

    /// <summary>The weight of samples whose true label is at <paramref name="trueIndex"/> and predicted label at <paramref name="predictedIndex"/>.</summary>
    /// <param name="trueIndex">Row: the index into <see cref="Labels"/> of the true label.</param>
    /// <param name="predictedIndex">Column: the index into <see cref="Labels"/> of the predicted label.</param>
    public double this[int trueIndex, int predictedIndex] => _cells[(trueIndex * _labels.Length) + predictedIndex];

    internal int Size => _labels.Length;

    internal ReadOnlySpan<double> Cells => _cells;

    internal bool IsWeighted { get; }

    /// <summary>True when at least one sample fell outside the label set and was not counted.</summary>
    internal bool DroppedSamples { get; }

    internal bool ExplicitLabels { get; }

    /// <summary>Copies the matrix into a two-dimensional array.</summary>
    /// <returns>A fresh <c>[rows, columns]</c> array; the matrix keeps its own storage.</returns>
    public double[,] ToArray()
    {
        int k = _labels.Length;
        double[,] result = new double[k, k];
        for (int row = 0; row < k; row++)
        {
            for (int col = 0; col < k; col++)
            {
                result[row, col] = _cells[(row * k) + col];
            }
        }
        return result;
    }

    /// <summary>
    /// Counts predictions against truth — the equivalent of
    /// <c>sklearn.metrics.confusion_matrix(y_true, y_pred, labels=…, sample_weight=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="labels">The label set and its order. Omit for the sorted union of both inputs. Samples whose true or predicted label falls outside this set are not counted, as in scikit-learn.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, contain duplicate labels, or no supplied label occurs in <paramref name="yTrue"/>.</exception>
    public static ConfusionMatrix Compute(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default)
    {
        Inputs.Validate(yTrue, yPred, sampleWeight);

        LabelIndex index = LabelIndex.Create(yTrue, yPred, labels);
        int k = index.Count;
        double[] cells = new double[k * k];
        bool weighted = !sampleWeight.IsEmpty;
        double total = 0.0;
        bool dropped = false;
        bool anyTrueLabelKnown = false;

        for (int i = 0; i < yTrue.Length; i++)
        {
            int row = index.IndexOf(yTrue[i]);
            if (row >= 0)
            {
                anyTrueLabelKnown = true;
            }

            int col = index.IndexOf(yPred[i]);
            if (row < 0 || col < 0)
            {
                dropped = true;
                continue;
            }

            double weight = weighted ? sampleWeight[i] : 1.0;
            cells[(row * k) + col] += weight;
            total += weight;
        }

        if (index.Explicit && !anyTrueLabelKnown)
        {
            throw new ArgumentException(
                "At least one supplied label must occur in yTrue.", nameof(labels));
        }

        return new ConfusionMatrix(cells, index.Labels, total, weighted, dropped, index.Explicit);
    }
}
