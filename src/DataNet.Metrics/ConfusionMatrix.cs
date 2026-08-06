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
    // The flat, row-major store behind Cells is m*m — the *extended* label
    // count (see LabelIndex) — not k*k. A sample whose true or predicted label
    // was requested but whose other side was not still needs to land somewhere,
    // or Prf's per-label predicted/true sums would silently exclude it, which
    // is exactly the discrepancy scikit-learn's own precision_score avoids by
    // computing those sums before restricting to the requested labels. The
    // public surface below — Labels, the indexer, ToArray — only ever exposes
    // the first k*k of it, so nothing public changes shape.
    private readonly double[] _cells;
    private readonly int[] _labels;
    private readonly ReadOnlyCollection<int> _labelView;
    private readonly int _stride;
    private readonly double[] _trueSum;

    private ConfusionMatrix(
        double[] cells, int[] labels, int stride, double[] trueSum,
        double totalWeight, bool weighted, bool dropped, bool explicitLabels)
    {
        _cells = cells;
        _labels = labels;
        _labelView = Array.AsReadOnly(labels);
        _stride = stride;
        _trueSum = trueSum;
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
    public double this[int trueIndex, int predictedIndex] => _cells[(trueIndex * _stride) + predictedIndex];

    internal int Size => _labels.Length;

    /// <summary>
    /// The row/column count of the flat store <see cref="Cells"/> is laid out
    /// over — the extended label count, greater than <see cref="Size"/>
    /// exactly when <see cref="ExplicitLabels"/> left some observed label out
    /// of the request. <see cref="Prf"/> is the only reader that needs it: it
    /// sums a requested label's row or column across every one of these, not
    /// just the other requested labels, which is how it recovers scikit-learn's
    /// predicted/true counts for a label subset.
    /// </summary>
    internal int Stride => _stride;

    internal ReadOnlySpan<double> Cells => _cells;

    /// <summary>
    /// Weight per requested true label, accumulated sample by sample in the same
    /// single pass as <see cref="Cells"/> rather than recovered afterwards by
    /// summing a row of it. scikit-learn computes this same quantity — its
    /// <c>true_sum</c> — the same way, via <c>np.bincount</c> over the samples in
    /// their original order; summing the already-built matrix's cells instead
    /// groups the same additions differently and, being floating-point, can land
    /// on a different last bit. <see cref="Prf.Support"/> is the only reader.
    /// </summary>
    internal ReadOnlySpan<double> TrueSum => _trueSum;

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
                result[row, col] = _cells[(row * _stride) + col];
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
    /// <param name="labels">The label set and its order. Omit for the sorted union of both inputs. In this matrix's public view — <see cref="Labels"/>, the indexer, <see cref="ToArray"/>, <see cref="TotalWeight"/> — a sample whose true or predicted label falls outside this set is not counted, exactly as in scikit-learn's own <c>confusion_matrix(labels=…)</c>.</param>
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
        int k = index.RequestedCount;
        int m = index.Count;
        double[] cells = new double[m * m];
        double[] trueSum = new double[k];
        bool weighted = !sampleWeight.IsEmpty;
        double total = 0.0;
        bool anyTrueLabelRequested = false;

        // index.IndexOf never misses here: the extended set is the requested
        // labels plus every label actually observed in yTrue/yPred, so every
        // sample's row and column resolve to some ordinal in [0, m). What can
        // still fall outside the *requested* k is exactly what scikit-learn's
        // confusion_matrix(labels=…) — and this type's public view — drop.
        for (int i = 0; i < yTrue.Length; i++)
        {
            int row = index.IndexOf(yTrue[i]);
            int col = index.IndexOf(yPred[i]);
            double weight = weighted ? sampleWeight[i] : 1.0;

            if (row < k)
            {
                anyTrueLabelRequested = true;
                // Accumulated here, sample by sample, so it lands on the same
                // floating-point total as scikit-learn's bincount — see TrueSum.
                trueSum[row] += weight;
            }

            cells[(row * m) + col] += weight;
            if (row < k && col < k)
            {
                total += weight;
            }
        }

        if (index.Explicit && !anyTrueLabelRequested)
        {
            throw new ArgumentException(
                "At least one supplied label must occur in yTrue.", nameof(labels));
        }

        int[] reportedLabels = new int[k];
        Array.Copy(index.Labels, reportedLabels, k);
        bool dropped = m > k;

        return new ConfusionMatrix(
            cells, reportedLabels, m, trueSum, total, weighted, dropped, index.Explicit);
    }
}
