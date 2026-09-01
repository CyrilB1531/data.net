using Lodestar.Abstractions;

namespace Lodestar.Decomposition.Internal;

/// <summary>An orthonormal basis for the range of <c>A</c>, found through a thin random block.</summary>
/// <remarks>
/// This is the only place the sparse matrix is read. Everything after it works on a block of
/// <c>k + p</c> columns, which is why the rank asked for — and not the size of the matrix —
/// decides the cost of the whole method.
/// </remarks>
internal static class RandomizedRangeFinder
{
    /// <summary>Returns <c>Q</c>, row-major <c>matrix.RowCount × w</c> with orthonormal columns.</summary>
    /// <remarks>
    /// <c>w</c> is <paramref name="size"/> unless a normalizer narrowed the block on the way —
    /// see <see cref="Orthonormalize"/> — so a caller reads it back off the length rather than
    /// assuming it.
    /// </remarks>
    internal static double[] Find(
        CsrMatrix matrix,
        ReadOnlySpan<double> omega,
        int size,
        int powerIterations,
        PowerIterationNormalizer normalizer)
    {
        PowerIterationNormalizer resolved = Resolve(normalizer, powerIterations);

        double[] block = matrix.Multiply(omega, size);
        int width = size;
        for (int iteration = 0; iteration < powerIterations; iteration++)
        {
            (block, width) = Normalize(block, matrix.RowCount, width, resolved);
            block = matrix.TransposeMultiply(block, width);
            (block, width) = Normalize(block, matrix.ColumnCount, width, resolved);
            block = matrix.Multiply(block, width);
        }

        return Orthonormalize(block, matrix.RowCount, width).Block;
    }

    /// <summary>scikit-learn's <c>auto</c>: no normalizer below three iterations, LU above.</summary>
    internal static PowerIterationNormalizer Resolve(
        PowerIterationNormalizer normalizer, int powerIterations)
    {
        if (normalizer != PowerIterationNormalizer.Auto)
        {
            return normalizer;
        }

        return powerIterations <= 2 ? PowerIterationNormalizer.None : PowerIterationNormalizer.Lu;
    }

    private static (double[] Block, int Width) Normalize(
        double[] block, int rows, int columns, PowerIterationNormalizer normalizer) =>
        normalizer switch
        {
            PowerIterationNormalizer.Qr => Orthonormalize(block, rows, columns),
            PowerIterationNormalizer.Lu => PermutedLower(block, rows, columns),
            _ => (block, columns),
        };

    /// <summary>An economic QR's <c>Q</c>, which is <c>min(rows, columns)</c> columns wide.</summary>
    /// <remarks>
    /// LAPACK builds one reflector per leading column and stops at <c>min(rows, columns)</c>, and
    /// reflector <c>k</c> is read off column <c>k</c> of what the earlier ones left — so the factor
    /// of a wide block is the factor of its leading square, which is what makes the trim below
    /// scipy's answer rather than an approximation of it.
    /// </remarks>
    private static (double[] Block, int Width) Orthonormalize(double[] block, int rows, int columns)
    {
        int keep = Math.Min(rows, columns);
        (double[] basis, _) = HouseholderQr.Decompose(Leading(block, rows, columns, keep), rows, keep);
        return (basis, keep);
    }

    /// <summary>The same trim for <c>P L</c>: pivots and multipliers read the leading columns too.</summary>
    private static (double[] Block, int Width) PermutedLower(double[] block, int rows, int columns)
    {
        int keep = Math.Min(rows, columns);
        return (PartialPivotLu.PermutedLower(Leading(block, rows, columns, keep), rows, keep), keep);
    }

    private static double[] Leading(double[] block, int rows, int columns, int keep)
    {
        if (keep == columns)
        {
            return block;
        }

        double[] trimmed = new double[checked(rows * keep)];
        for (int row = 0; row < rows; row++)
        {
            Array.Copy(block, row * columns, trimmed, row * keep, keep);
        }
        return trimmed;
    }
}
