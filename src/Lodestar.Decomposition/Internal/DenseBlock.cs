namespace Lodestar.Decomposition.Internal;

/// <summary>The row-major helpers every kernel in this package shares.</summary>
/// <remarks>
/// A block of <c>r</c> rows and <c>c</c> columns is a <c>double[r * c]</c> where element
/// <c>(i, j)</c> lives at <c>i * c + j</c> — the layout <c>CsrMatrix</c>'s dense-block products
/// already take and return, so nothing in this package ever transposes to talk to it.
/// </remarks>
internal static class DenseBlock
{
    /// <summary>Transposes a row-major block into another row-major block.</summary>
    internal static double[] Transpose(ReadOnlySpan<double> block, int rows, int columns)
    {
        double[] result = new double[block.Length];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                result[(j * rows) + i] = block[(i * columns) + j];
            }
        }
        return result;
    }

    /// <summary><c>Bᵀ B</c> for a row-major <c>rows × columns</c> block, as <c>columns × columns</c>.</summary>
    /// <remarks>
    /// Reached for wherever a Gram matrix of the columns is wanted — the beta divergence and the
    /// Frobenius update of H both need <c>WᵀW</c> — because transposing first would allocate a
    /// second copy of the largest block in the fit to compute a matrix of the rank's size.
    /// </remarks>
    internal static double[] TransposeGram(ReadOnlySpan<double> block, int rows, int columns)
    {
        double[] result = new double[checked(columns * columns)];
        for (int a = 0; a < columns; a++)
        {
            for (int b = 0; b < columns; b++)
            {
                double sum = 0;
                for (int i = 0; i < rows; i++)
                {
                    sum += block[(i * columns) + a] * block[(i * columns) + b];
                }
                result[(a * columns) + b] = sum;
            }
        }
        return result;
    }

    /// <summary>The Euclidean norm of one column.</summary>
    internal static double ColumnNorm(ReadOnlySpan<double> block, int rows, int columns, int column)
    {
        double sum = 0;
        for (int i = 0; i < rows; i++)
        {
            double value = block[(i * columns) + column];
            sum += value * value;
        }
        return Math.Sqrt(sum);
    }
}
