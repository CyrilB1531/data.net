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

    // SonarLint S107 warns above 7 parameters; this constructor just names the
    // nine pieces of state the matrix is immutably built from, once, from
    // Compute — a parameter object here would only add a type with no other
    // reason to exist.
#pragma warning disable S107
    private ConfusionMatrix(
        double[] cells, int[] labels, int stride, double[] trueSum, bool noSampleCorrect,
        double totalWeight, bool weighted, bool dropped, bool explicitLabels)
#pragma warning restore S107
    {
        _cells = cells;
        _labels = labels;
        _labelView = Array.AsReadOnly(labels);
        _stride = stride;
        _trueSum = trueSum;
        NoSampleCorrect = noSampleCorrect;
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

    /// <summary>
    /// True when not one sample in the whole dataset was predicted correctly —
    /// <c>y_true[i] == y_pred[i]</c> for no <c>i</c> at all, checked over every
    /// observed label, not only the requested ones. This is the exact condition
    /// under which scikit-learn's <c>multilabel_confusion_matrix</c> takes its
    /// "pathological case" branch and seeds <c>tp_sum</c>/<c>pred_sum</c>/
    /// <c>true_sum</c> with <c>np.zeros(...)</c> — a NumPy float64 array — in
    /// place of the int64 array <c>np.bincount</c> would otherwise have produced;
    /// NumPy then upcasts the whole assembled matrix to float64, so every support
    /// value downstream prints with a decimal point even when nothing was
    /// weighted. It is not the same thing as accuracy over the requested labels
    /// being zero: a sample outside the requested label set can still be the one
    /// correct prediction that keeps this false while <see cref="Prf"/>'s
    /// accuracy over just the requested labels is nonetheless zero.
    /// </summary>
    internal bool NoSampleCorrect { get; }

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
    /// The cells as a rectangular array, scaled — <c>confusion_matrix(…, normalize=…)</c>.
    /// </summary>
    /// <param name="normalization">Which sum each cell is divided by.</param>
    /// <returns>A fresh <c>k × k</c> array; the matrix itself is unchanged.</returns>
    /// <remarks>
    /// A row, column or total that counted nothing yields zeros rather than
    /// <see cref="double.NaN"/>, which is what scikit-learn's <c>nan_to_num</c>
    /// does to the same division.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="normalization"/> is not one of the four modes.</exception>
    public double[,] ToArray(Normalization normalization)
    {
        int k = _labels.Length;
        double[,] result = new double[k, k];
        double total = 0.0;
        double[] rowSums = new double[k];
        double[] colSums = new double[k];

        for (int row = 0; row < k; row++)
        {
            for (int col = 0; col < k; col++)
            {
                double cell = _cells[(row * _stride) + col];
                rowSums[row] += cell;
                colSums[col] += cell;
                total += cell;
            }
        }

        for (int row = 0; row < k; row++)
        {
            for (int col = 0; col < k; col++)
            {
                double cell = _cells[(row * _stride) + col];
                double denominator = normalization switch
                {
                    Normalization.None => 1.0,
                    Normalization.True => rowSums[row],
                    Normalization.Pred => colSums[col],
                    Normalization.All => total,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(normalization), normalization, "Not one of the four normalize= modes."),
                };

                // SonarLint S1244 warns against comparing floating point for exact
                // equality, which is right for arithmetic and wrong here: this asks
                // whether anything accumulated at all, not whether two computed
                // quantities are close. scikit-learn passes the divided matrix
                // through nan_to_num, so a row, column or total that counted
                // nothing gives zeros rather than NaN. A tolerance would treat a
                // denominator that is merely small as if it were absent, which
                // changes the answer for a legitimately tiny weight.
#pragma warning disable S1244
                result[row, col] = denominator == 0.0 ? 0.0 : cell / denominator;
#pragma warning restore S1244
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
    /// <param name="labels">The label set and its order. Omit for the sorted union of both inputs. In this matrix's public view — <see cref="Labels"/>, the indexer, <see cref="ToArray()"/>, <see cref="TotalWeight"/> — a sample whose true or predicted label falls outside this set is not counted, exactly as in scikit-learn's own <c>confusion_matrix(labels=…)</c>.</param>
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
        bool anySampleCorrect = false;

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

            // Same label maps to the same ordinal, so row == col here is exactly
            // yTrue[i] == yPred[i] — checked over every observed label, matching
            // scikit-learn's tp_bins before it is ever narrowed to the request.
            if (row == col)
            {
                anySampleCorrect = true;
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
            cells, reportedLabels, m, trueSum, !anySampleCorrect, total, weighted, dropped, index.Explicit);
    }
}
