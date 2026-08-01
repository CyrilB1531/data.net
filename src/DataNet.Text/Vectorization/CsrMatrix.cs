namespace DataNet.Text.Vectorization;

/// <summary>The vector norm used when normalizing rows of a <see cref="CsrMatrix"/>.</summary>
public enum SparseNorm
{
    /// <summary>Sum of absolute values.</summary>
    L1,

    /// <summary>Euclidean norm.</summary>
    L2,
}

/// <summary>
/// A compressed sparse row (CSR) matrix of <see cref="double"/> values.
/// </summary>
/// <remarks>
/// <para>
/// Stores only non-zero entries, row by row: <see cref="Values"/> and
/// <see cref="ColumnIndices"/> hold the non-zeros, and <see cref="RowPointers"/>
/// (length <c>RowCount + 1</c>) delimits each row. This is the layout produced by
/// the vectorizers and consumed by cosine-similarity search.
/// </para>
/// <para>Instances are immutable except for <see cref="NormalizeRows"/>, which mutates values in place.</para>
/// </remarks>
public sealed class CsrMatrix
{
    /// <summary>Creates a CSR matrix from raw arrays (not copied).</summary>
    public CsrMatrix(int rowCount, int columnCount, double[] values, int[] columnIndices, int[] rowPointers)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(columnIndices);
        ArgumentNullException.ThrowIfNull(rowPointers);
        if (rowPointers.Length != rowCount + 1)
        {
            throw new ArgumentException("rowPointers length must be rowCount + 1.", nameof(rowPointers));
        }
        if (values.Length != columnIndices.Length)
        {
            throw new ArgumentException("values and columnIndices must have equal length.");
        }

        RowCount = rowCount;
        ColumnCount = columnCount;
        Values = values;
        ColumnIndices = columnIndices;
        RowPointers = rowPointers;
    }

    /// <summary>Number of rows.</summary>
    public int RowCount { get; }

    /// <summary>Number of columns.</summary>
    public int ColumnCount { get; }

    /// <summary>Non-zero values, ordered by row then by the order they were appended.</summary>
    public double[] Values { get; }

    /// <summary>Column index of each entry in <see cref="Values"/>.</summary>
    public int[] ColumnIndices { get; }

    /// <summary>Start offset of each row into <see cref="Values"/>; length <c>RowCount + 1</c>.</summary>
    public int[] RowPointers { get; }

    /// <summary>Number of stored (non-zero) entries.</summary>
    public int NonZeroCount => Values.Length;

    /// <summary>Materializes the matrix as a dense 2-D array.</summary>
    public double[,] ToDense()
    {
        var dense = new double[RowCount, ColumnCount];
        for (int row = 0; row < RowCount; row++)
        {
            for (int k = RowPointers[row]; k < RowPointers[row + 1]; k++)
            {
                dense[row, ColumnIndices[k]] = Values[k];
            }
        }
        return dense;
    }

    /// <summary>Computes the L1 norm (sum of absolute values) of a row.</summary>
    public double RowL1Norm(int row)
    {
        double sum = 0;
        for (int k = RowPointers[row]; k < RowPointers[row + 1]; k++)
        {
            sum += Math.Abs(Values[k]);
        }
        return sum;
    }

    /// <summary>Computes the L2 (Euclidean) norm of a row.</summary>
    public double RowL2Norm(int row)
    {
        double sum = 0;
        for (int k = RowPointers[row]; k < RowPointers[row + 1]; k++)
        {
            double v = Values[k];
            sum += v * v;
        }
        return Math.Sqrt(sum);
    }

    /// <summary>
    /// Normalizes each row in place to unit norm. Zero rows are left unchanged.
    /// Matches <c>sklearn.preprocessing.normalize</c>.
    /// </summary>
    public void NormalizeRows(SparseNorm norm)
    {
        for (int row = 0; row < RowCount; row++)
        {
            double n = norm == SparseNorm.L1 ? RowL1Norm(row) : RowL2Norm(row);
            if (n == 0)
            {
                continue;
            }
            for (int k = RowPointers[row]; k < RowPointers[row + 1]; k++)
            {
                Values[k] /= n;
            }
        }
    }

    /// <summary>Computes the matrix-vector product <c>this · vector</c>.</summary>
    public double[] Multiply(ReadOnlySpan<double> vector)
    {
        if (vector.Length != ColumnCount)
        {
            throw new ArgumentException($"vector length {vector.Length} != column count {ColumnCount}.", nameof(vector));
        }

        var result = new double[RowCount];
        for (int row = 0; row < RowCount; row++)
        {
            double acc = 0;
            for (int k = RowPointers[row]; k < RowPointers[row + 1]; k++)
            {
                acc += Values[k] * vector[ColumnIndices[k]];
            }
            result[row] = acc;
        }
        return result;
    }
}
