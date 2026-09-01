using Lodestar.Abstractions;

namespace Lodestar.Decomposition.Internal;

/// <summary>Lee and Seung's multiplicative updates, in scikit-learn's <c>solver="mu"</c> form.</summary>
/// <remarks>
/// Each factor is scaled by a ratio rather than moved by a step, which keeps it non-negative
/// with no projection and no line search — and makes a zero permanent, so the initialisation
/// decides the sparsity of the answer. W is updated first and H second, against the
/// already-updated W: doing both against the old pair is a different algorithm.
/// </remarks>
internal static class MultiplicativeUpdates
{
    internal static void UpdateWeights(
        CsrMatrix matrix, double[] w, double[] h, int k, NmfBetaLoss loss)
    {
        int features = matrix.ColumnCount;
        double[] numerator;
        double[] denominator;

        if (loss == NmfBetaLoss.KullbackLeibler)
        {
            // WH is needed only where X is non-zero, and the ratio X/WH replaces it there.
            double[] ratio = SparseRatio(matrix, w, h, k);
            numerator = SparsePatternTimesTranspose(matrix, ratio, h, k);
            double[] rowSums = RowSums(h, k, features);
            denominator = new double[w.Length];
            for (int i = 0; i < matrix.RowCount; i++)
            {
                for (int a = 0; a < k; a++)
                {
                    denominator[(i * k) + a] = rowSums[a];
                }
            }
        }
        else
        {
            numerator = MatrixTimesTranspose(matrix, h, k);          // X Hᵀ
            double[] hht = Gram(h, k, features);                     // H Hᵀ
            denominator = DenseProduct(w, matrix.RowCount, k, hht, k);
        }

        Scale(w, numerator, denominator);
    }

    internal static void UpdateComponents(
        CsrMatrix matrix, double[] w, double[] h, int k, NmfBetaLoss loss)
    {
        int features = matrix.ColumnCount;
        double[] numerator;
        double[] denominator;

        if (loss == NmfBetaLoss.KullbackLeibler)
        {
            double[] ratio = SparseRatio(matrix, w, h, k);
            numerator = TransposeTimesSparsePattern(matrix, ratio, w, k);
            double[] columnSums = ColumnSums(w, matrix.RowCount, k);
            denominator = new double[h.Length];
            for (int a = 0; a < k; a++)
            {
                for (int j = 0; j < features; j++)
                {
                    denominator[(a * features) + j] = columnSums[a];
                }
            }
        }
        else
        {
            numerator = TransposeTimesMatrix(matrix, w, k);           // Wᵀ X
            double[] wtw = DenseBlock.TransposeGram(w, matrix.RowCount, k);
            denominator = DenseProduct(wtw, k, k, h, features);
        }

        Scale(h, numerator, denominator);

        // scikit-learn snaps H below machine epsilon to zero for β ≤ 1, and only there.
        if (loss == NmfBetaLoss.KullbackLeibler)
        {
            for (int i = 0; i < h.Length; i++)
            {
                if (h[i] < BetaDivergence.MachineEpsilon)
                {
                    h[i] = 0;
                }
            }
        }
    }

    /// <summary><c>X / (W H)</c> at X's non-zeros, floored so the division cannot blow up.</summary>
    private static double[] SparseRatio(CsrMatrix matrix, double[] w, double[] h, int k)
    {
        int features = matrix.ColumnCount;
        double[] ratio = new double[matrix.Values.Length];
        for (int row = 0; row < matrix.RowCount; row++)
        {
            for (int index = matrix.RowPointers[row]; index < matrix.RowPointers[row + 1]; index++)
            {
                int column = matrix.ColumnIndices[index];
                double product = 0;
                for (int a = 0; a < k; a++)
                {
                    product += w[(row * k) + a] * h[(a * features) + column];
                }
                ratio[index] = matrix.Values[index] / Math.Max(product, BetaDivergence.MachineEpsilon);
            }
        }
        return ratio;
    }

    private static double[] MatrixTimesTranspose(CsrMatrix matrix, double[] h, int k) =>
        SparsePatternTimesTranspose(matrix, matrix.Values, h, k);

    /// <summary><c>S Hᵀ</c> where S shares the matrix's sparsity and carries <paramref name="data"/>.</summary>
    private static double[] SparsePatternTimesTranspose(
        CsrMatrix matrix, double[] data, double[] h, int k)
    {
        int features = matrix.ColumnCount;
        double[] result = new double[checked(matrix.RowCount * k)];
        for (int row = 0; row < matrix.RowCount; row++)
        {
            for (int index = matrix.RowPointers[row]; index < matrix.RowPointers[row + 1]; index++)
            {
                double value = data[index];
                int column = matrix.ColumnIndices[index];
                for (int a = 0; a < k; a++)
                {
                    result[(row * k) + a] += value * h[(a * features) + column];
                }
            }
        }
        return result;
    }

    private static double[] TransposeTimesMatrix(CsrMatrix matrix, double[] w, int k) =>
        TransposeTimesSparsePattern(matrix, matrix.Values, w, k);

    /// <summary><c>Wᵀ S</c> where S shares the matrix's sparsity and carries <paramref name="data"/>.</summary>
    private static double[] TransposeTimesSparsePattern(
        CsrMatrix matrix, double[] data, double[] w, int k)
    {
        int features = matrix.ColumnCount;
        double[] result = new double[checked(k * features)];
        for (int row = 0; row < matrix.RowCount; row++)
        {
            for (int index = matrix.RowPointers[row]; index < matrix.RowPointers[row + 1]; index++)
            {
                double value = data[index];
                int column = matrix.ColumnIndices[index];
                for (int a = 0; a < k; a++)
                {
                    result[(a * features) + column] += w[(row * k) + a] * value;
                }
            }
        }
        return result;
    }

    /// <summary><c>B Bᵀ</c> for a row-major <c>rows × columns</c> block.</summary>
    private static double[] Gram(double[] block, int rows, int columns)
    {
        double[] result = new double[checked(rows * rows)];
        for (int a = 0; a < rows; a++)
        {
            for (int b = 0; b < rows; b++)
            {
                double sum = 0;
                for (int j = 0; j < columns; j++)
                {
                    sum += block[(a * columns) + j] * block[(b * columns) + j];
                }
                result[(a * rows) + b] = sum;
            }
        }
        return result;
    }

    private static double[] DenseProduct(
        double[] left, int leftRows, int inner, double[] right, int rightColumns)
    {
        double[] result = new double[checked(leftRows * rightColumns)];
        for (int i = 0; i < leftRows; i++)
        {
            for (int t = 0; t < inner; t++)
            {
                double value = left[(i * inner) + t];
                for (int j = 0; j < rightColumns; j++)
                {
                    result[(i * rightColumns) + j] += value * right[(t * rightColumns) + j];
                }
            }
        }
        return result;
    }

    private static double[] RowSums(double[] block, int rows, int columns)
    {
        double[] sums = new double[rows];
        for (int i = 0; i < rows; i++)
        {
            double sum = 0;
            for (int j = 0; j < columns; j++)
            {
                sum += block[(i * columns) + j];
            }
            sums[i] = sum;
        }
        return sums;
    }

    private static double[] ColumnSums(double[] block, int rows, int columns)
    {
        double[] sums = new double[columns];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                sums[j] += block[(i * columns) + j];
            }
        }
        return sums;
    }

    /// <summary><c>factor *= numerator / denominator</c>, with a zero denominator floored.</summary>
    private static void Scale(double[] factor, double[] numerator, double[] denominator)
    {
        for (int i = 0; i < factor.Length; i++)
        {
            // S1244: the comparison is exact on purpose, and so is scikit-learn's own
            // `denominator[denominator == 0] = EPSILON` — a denominator merely near zero
            // still divides, and replacing it would move the answer away from the reference.
#pragma warning disable S1244
            double bottom = denominator[i] == 0 ? BetaDivergence.MachineEpsilon : denominator[i];
#pragma warning restore S1244
            factor[i] *= numerator[i] / bottom;
        }
    }
}
