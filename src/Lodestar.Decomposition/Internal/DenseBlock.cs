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
