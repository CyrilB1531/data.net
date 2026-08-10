namespace DataNet.Metrics.Internal;

/// <summary>
/// Row sums, column sums, trace and total over a matrix's <c>Size × Size</c>
/// view — the one pass balanced accuracy, Matthews correlation and Cohen's kappa
/// all begin with.
/// </summary>
/// <remarks>
/// The <c>Size × Size</c> view, not the extended <c>Stride × Stride</c> store.
/// <see cref="Prf"/> reads the extended one deliberately, to recover
/// scikit-learn's per-label predicted and true sums for a label subset; these
/// three metrics must not, because scikit-learn computes them from
/// <c>confusion_matrix(y_true, y_pred, labels=…)</c>, which is <c>k × k</c> and
/// drops the samples outside the requested labels.
/// </remarks>
internal static class MatrixSums
{
    /// <summary>Fills the sums for <paramref name="cm"/>.</summary>
    /// <param name="cm">The matrix to read.</param>
    /// <param name="rowSums">One entry per label, the weight whose true label is that one.</param>
    /// <param name="colSums">One entry per label, the weight predicted as that one.</param>
    /// <param name="trace">The weight on the diagonal.</param>
    /// <param name="total">The weight the <c>Size × Size</c> view holds.</param>
    public static void Compute(
        ConfusionMatrix cm, double[] rowSums, double[] colSums, out double trace, out double total)
    {
        int k = cm.Size;
        int stride = cm.Stride;
        ReadOnlySpan<double> cells = cm.Cells;
        trace = 0.0;
        total = 0.0;

        for (int row = 0; row < k; row++)
        {
            for (int col = 0; col < k; col++)
            {
                double cell = cells[(row * stride) + col];
                rowSums[row] += cell;
                colSums[col] += cell;
                total += cell;
                if (row == col)
                {
                    trace += cell;
                }
            }
        }
    }
}
