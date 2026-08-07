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
    /// <summary>
    /// Creates a CSR matrix from raw arrays (not copied), checking that they
    /// describe a structurally valid matrix.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The arrays are validated in full: <paramref name="rowPointers"/> must be
    /// non-decreasing, start at 0 and end at <c>values.Length</c>, and every entry
    /// of <paramref name="columnIndices"/> must fall inside
    /// <paramref name="columnCount"/>. This costs one linear pass, and it is what
    /// makes the type safe to build from a file: without it, a single out-of-range
    /// column index turns <see cref="Multiply"/> or <see cref="ToDense"/> into an
    /// out-of-bounds write.
    /// </para>
    /// <para>
    /// The vectorizers build their arrays themselves and bypass this pass through
    /// an internal path — they cannot produce an invalid one.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">The arrays do not describe a valid CSR matrix.</exception>
    public CsrMatrix(int rowCount, int columnCount, double[] values, int[] columnIndices, int[] rowPointers)
        : this(rowCount, columnCount, values, columnIndices, rowPointers, validate: true)
    {
    }

    private CsrMatrix(int rowCount, int columnCount, double[] values, int[] columnIndices, int[] rowPointers, bool validate)
    {
        Guard.NotNull(values);
        Guard.NotNull(columnIndices);
        Guard.NotNull(rowPointers);
        Guard.NotLessThan(rowCount, 0);
        Guard.NotLessThan(columnCount, 0);
        if (rowPointers.Length != rowCount + 1)
        {
            throw new ArgumentException("rowPointers length must be rowCount + 1.", nameof(rowPointers));
        }
        if (values.Length != columnIndices.Length)
        {
            throw new ArgumentException("values and columnIndices must have equal length.");
        }
        if (validate)
        {
            ValidateStructure(columnCount, values.Length, columnIndices, rowPointers);
        }

        RowCount = rowCount;
        ColumnCount = columnCount;
        Values = values;
        ColumnIndices = columnIndices;
        RowPointers = rowPointers;
    }

    /// <summary>
    /// Creates a matrix from arrays this library has just built, skipping the
    /// structural pass. Never call it with data that came from outside.
    /// </summary>
    internal static CsrMatrix CreateUnchecked(int rowCount, int columnCount, double[] values, int[] columnIndices, int[] rowPointers) =>
        new(rowCount, columnCount, values, columnIndices, rowPointers, validate: false);

    private static void ValidateStructure(int columnCount, int nonZeroCount, int[] columnIndices, int[] rowPointers)
    {
        if (rowPointers[0] != 0)
        {
            throw new ArgumentException($"rowPointers[0] must be 0 but is {rowPointers[0]}.", nameof(rowPointers));
        }
        for (int row = 1; row < rowPointers.Length; row++)
        {
            if (rowPointers[row] < rowPointers[row - 1])
            {
                throw new ArgumentException(
                    $"rowPointers must be non-decreasing but rowPointers[{row}] = {rowPointers[row]} < rowPointers[{row - 1}] = {rowPointers[row - 1]}.",
                    nameof(rowPointers));
            }
        }
        if (rowPointers[rowPointers.Length - 1] != nonZeroCount)
        {
            throw new ArgumentException(
                $"rowPointers must end at the number of stored values ({nonZeroCount}) but ends at {rowPointers[rowPointers.Length - 1]}.",
                nameof(rowPointers));
        }
        for (int k = 0; k < columnIndices.Length; k++)
        {
            if ((uint)columnIndices[k] >= (uint)columnCount)
            {
                throw new ArgumentException(
                    $"columnIndices[{k}] = {columnIndices[k]} is outside the column range [0, {columnCount}).",
                    nameof(columnIndices));
            }
        }
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

            // SonarLint S1244: "zero rows are left unchanged" is the documented
            // contract and sklearn's behaviour, and a zero row is one whose norm is
            // exactly zero. A tolerance would silently skip rows that do have a
            // direction, which is a different normalization.
#pragma warning disable S1244
            if (n == 0)
#pragma warning restore S1244
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
