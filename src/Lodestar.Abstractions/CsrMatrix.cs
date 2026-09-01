namespace Lodestar.Abstractions;

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
/// Stores only non-zero entries, row by row: <see cref="Values"/> and <see cref="ColumnIndices"/> hold the
/// non-zeros, and <see cref="RowPointers"/> (length <c>RowCount + 1</c>) delimits each row — the layout
/// <c>CsrMatrixValidationTests</c> checks the constructor enforces. Instances are immutable except for
/// <see cref="NormalizeRows"/>, which mutates values in place.
/// </remarks>
public sealed class CsrMatrix
{
    /// <summary>
    /// Creates a CSR matrix from raw arrays (not copied), checking that they
    /// describe a structurally valid matrix.
    /// </summary>
    /// <remarks>
    /// This constructor is the boundary a deserialized matrix crosses, so it validates in full — every
    /// rejected shape is a case in <c>CsrMatrixValidationTests</c>. The vectorizers build their own arrays
    /// and bypass this pass through <see cref="CreateUnchecked"/>: they cannot produce an invalid one.
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
    // CA1819 (properties should not return arrays): Values, ColumnIndices and
    // RowPointers *are* the compressed-sparse-row format — a consumer indexes
    // them directly, which is the reason to expose CSR at all. They have been
    // public since 0.1.0, so wrapping them is a breaking change for a rule about
    // defensive copies this type deliberately does not make.
#pragma warning disable CA1819
    public double[] Values { get; }

    /// <summary>Column index of each entry in <see cref="Values"/>.</summary>
    public int[] ColumnIndices { get; }

    /// <summary>Start offset of each row into <see cref="Values"/>; length <c>RowCount + 1</c>.</summary>
    public int[] RowPointers { get; }
#pragma warning restore CA1819

    /// <summary>Number of stored (non-zero) entries.</summary>
    public int NonZeroCount => Values.Length;

    /// <summary>Materializes the matrix as a dense 2-D array.</summary>
    // CA1814 (prefer jagged arrays): a confusion matrix and a densified CSR
    // matrix are rectangular by construction, and double[,] is the shape every
    // consumer expects to interop with. A jagged array would cost one allocation
    // per row and let a caller build a ragged one.
#pragma warning disable CA1814
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
#pragma warning restore CA1814

    /// <exception cref="IndexOutOfRangeException"><paramref name="row"/> is negative or not below <see cref="RowCount"/>.</exception>
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

    /// <exception cref="IndexOutOfRangeException"><paramref name="row"/> is negative or not below <see cref="RowCount"/>.</exception>
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

    /// <exception cref="ArgumentException"><paramref name="vector"/> is not <see cref="ColumnCount"/> long.</exception>
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

    /// <summary>Computes the matrix-block product <c>this · block</c>.</summary>
    /// <remarks>
    /// The dense operand is row-major and <paramref name="columnCount"/> wide, so its length is
    /// <see cref="ColumnCount"/> times that; the result is <see cref="RowCount"/> rows of the same
    /// width. One pass over the non-zeros rather than <paramref name="columnCount"/> passes: each
    /// column index is read once and the inner loop walks contiguous memory on both sides.
    /// </remarks>
    /// <param name="block">The dense right operand, row-major.</param>
    /// <param name="columnCount">How many columns <paramref name="block"/> holds.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="columnCount"/> is not positive, or the product would not fit in one array.</exception>
    /// <exception cref="ArgumentException"><paramref name="block"/> is not <see cref="ColumnCount"/> rows of that width.</exception>
    public double[] Multiply(ReadOnlySpan<double> block, int columnCount)
    {
        GuardBlock(block, ColumnCount, columnCount);

        double[] result = new double[ProductLength(RowCount, columnCount)];
        for (int row = 0; row < RowCount; row++)
        {
            int target = row * columnCount;
            for (int k = RowPointers[row]; k < RowPointers[row + 1]; k++)
            {
                double value = Values[k];
                int source = ColumnIndices[k] * columnCount;
                for (int column = 0; column < columnCount; column++)
                {
                    result[target + column] += value * block[source + column];
                }
            }
        }
        return result;
    }

    /// <summary>Computes the transposed product <c>this^T · block</c>, without transposing.</summary>
    /// <remarks>
    /// The dense operand is <see cref="RowCount"/> rows of <paramref name="columnCount"/>, and the
    /// result is <see cref="ColumnCount"/> of them. Materializing the transpose would cost a second
    /// matrix; scattering into the result instead reads each non-zero once, which is the same work.
    /// </remarks>
    /// <param name="block">The dense right operand, row-major.</param>
    /// <param name="columnCount">How many columns <paramref name="block"/> holds.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="columnCount"/> is not positive, or the product would not fit in one array.</exception>
    /// <exception cref="ArgumentException"><paramref name="block"/> is not <see cref="RowCount"/> rows of that width.</exception>
    public double[] TransposeMultiply(ReadOnlySpan<double> block, int columnCount)
    {
        GuardBlock(block, RowCount, columnCount);

        double[] result = new double[ProductLength(ColumnCount, columnCount)];
        for (int row = 0; row < RowCount; row++)
        {
            int source = row * columnCount;
            for (int k = RowPointers[row]; k < RowPointers[row + 1]; k++)
            {
                double value = Values[k];
                int target = ColumnIndices[k] * columnCount;
                for (int column = 0; column < columnCount; column++)
                {
                    result[target + column] += value * block[source + column];
                }
            }
        }
        return result;
    }

    /// <summary>Refuses a dense operand whose shape does not fit the side it multiplies.</summary>
    private static void GuardBlock(ReadOnlySpan<double> block, int expectedRows, int columnCount)
    {
        if (columnCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(columnCount), columnCount, "A dense block has at least one column.");
        }
        if (block.Length != (long)expectedRows * columnCount)
        {
            throw new ArgumentException(
                $"A block of {expectedRows} rows and {columnCount} columns holds "
                    + $"{(long)expectedRows * columnCount} values, not {block.Length}.",
                nameof(block));
        }
    }

    /// <summary>The result's length, refused rather than wrapped when it overflows an int.</summary>
    private static int ProductLength(int rows, int columnCount)
    {
        long length = (long)rows * columnCount;
        if (length > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(columnCount), columnCount, "The product would not fit in a single array.");
        }
        return (int)length;
    }
}
