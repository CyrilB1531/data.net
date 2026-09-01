using Lodestar.Abstractions;

namespace Lodestar.Decomposition.Internal;

/// <summary>Halko's randomized SVD — the kernel every fit in this package factors through.</summary>
/// <remarks>
/// The factors come back <em>unflipped</em> and <em>untruncated</em> because the two callers agree
/// on neither: the estimator flips on the right vectors and keeps <c>k</c> of them, while NMF's
/// initialisation flips on the left ones. scikit-learn is arranged the same way — <c>flip_sign</c>
/// is a parameter of <c>randomized_svd</c> and <c>TruncatedSVD</c> declines it.
/// </remarks>
internal static class RandomizedSvd
{
    /// <summary>Factors <paramref name="matrix"/> through a thin random block.</summary>
    /// <remarks>
    /// <c>U</c> is row-major <c>matrix.RowCount × Rank</c>, <c>Vt</c> row-major
    /// <c>Rank × matrix.ColumnCount</c>, and <c>Rank</c> is <c>S.Length</c> — which falls below
    /// <c>componentCount + oversampling</c> whenever a normalizer's economic factorization
    /// narrowed the block on the way, so a caller reads it back rather than assuming it.
    /// </remarks>
    internal static (double[] U, double[] S, double[] Vt, int Rank) Compute(
        CsrMatrix matrix,
        int componentCount,
        int oversampling,
        int powerIterations,
        PowerIterationNormalizer normalizer,
        ReadOnlySpan<double> omega)
    {
        int features = matrix.ColumnCount;
        int size = componentCount + oversampling;

        double[] basis = RandomizedRangeFinder.Find(
            matrix, omega, size, powerIterations, normalizer);
        int basisSize = basis.Length / matrix.RowCount;

        // B = Qᵀ A, reached as (Aᵀ Q)ᵀ so the sparse matrix is never transposed.
        double[] b = DenseBlock.Transpose(
            matrix.TransposeMultiply(basis, basisSize), features, basisSize);
        (double[] uhat, double[] s, double[] vt) = JacobiSvd.Decompose(b, basisSize, features);

        double[] u = Product(basis, uhat, matrix.RowCount, basisSize, s.Length);
        return (u, s, vt, s.Length);
    }

    /// <summary><c>U = Q Û</c>, one <c>m × basisSize × rank</c> product.</summary>
    /// <remarks>
    /// It is negligible beside the sparse products the range finder has already run, and it is
    /// what lets a caller that needs the left vectors share this path instead of forking it.
    /// </remarks>
    private static double[] Product(
        double[] left, double[] right, int rows, int inner, int columns)
    {
        double[] result = new double[checked(rows * columns)];
        for (int row = 0; row < rows; row++)
        {
            int target = row * columns;
            for (int middle = 0; middle < inner; middle++)
            {
                double scale = left[(row * inner) + middle];
                int source = middle * columns;
                for (int column = 0; column < columns; column++)
                {
                    result[target + column] += scale * right[source + column];
                }
            }
        }
        return result;
    }
}
