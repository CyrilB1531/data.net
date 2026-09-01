namespace Lodestar.Decomposition.Internal;

/// <summary>Gaussian elimination with partial pivoting, keeping only <c>P L</c>.</summary>
/// <remarks>
/// This is the <c>LU</c> power-iteration normalizer, and it is scikit-learn's default: the
/// <c>auto</c> rule resolves to <c>LU</c> whenever there are more than two power iterations, and
/// <c>TruncatedSVD</c> asks for five. <c>P L</c>'s columns are not orthonormal, unlike a QR's, but
/// it is cheaper and enough to stop the iteration collapsing onto the leading singular vector.
/// <c>U</c> is computed and dropped: forming it costs nothing beyond what the elimination already
/// wrote, and returning it would invite a caller to use a factorization this package never needs.
/// </remarks>
internal static class PartialPivotLu
{
    /// <summary>Returns <c>P L</c> for a row-major <c>rows × columns</c> block, <c>rows >= columns</c>.</summary>
    internal static double[] PermutedLower(ReadOnlySpan<double> a, int rows, int columns)
    {
        ValidateShape(a, rows, columns);

        double[] work = a.ToArray();
        int[] permutation = new int[rows];
        for (int i = 0; i < rows; i++)
        {
            permutation[i] = i;
        }

        Eliminate(work, permutation, rows, columns);

        return BuildPermutedLower(work, permutation, rows, columns);
    }

    private static void ValidateShape(ReadOnlySpan<double> a, int rows, int columns)
    {
        if (rows < columns)
        {
            throw new ArgumentException(
                $"This factorization needs at least as many rows as columns; got {rows} × {columns}.",
                nameof(a));
        }
        if (a.Length != checked(rows * columns))
        {
            throw new ArgumentException(
                $"Block length {a.Length} != {rows} × {columns}.", nameof(a));
        }
    }

    private static void Eliminate(double[] work, int[] permutation, int rows, int columns)
    {
        for (int k = 0; k < columns; k++)
        {
            int pivot = FindPivotRow(work, columns, rows, k);

            if (pivot != k)
            {
                SwapRows(work, columns, k, pivot);
                (permutation[k], permutation[pivot]) = (permutation[pivot], permutation[k]);
            }

            EliminateColumn(work, rows, columns, k);
        }
    }

    private static int FindPivotRow(double[] work, int columns, int rows, int k)
    {
        int pivot = k;
        double best = Math.Abs(work[(k * columns) + k]);
        for (int i = k + 1; i < rows; i++)
        {
            double candidate = Math.Abs(work[(i * columns) + k]);
            if (candidate > best)
            {
                best = candidate;
                pivot = i;
            }
        }
        return pivot;
    }

    private static void EliminateColumn(double[] work, int rows, int columns, int k)
    {
        double head = work[(k * columns) + k];
        // A zero pivot means the column is already eliminated; dividing by it would
        // fill the factor with NaN on a rank-deficient block, which the corpus carries.
        //
        // S1244: whether the pivot vanished entirely, not whether two computed
        // quantities are close.
#pragma warning disable S1244
        if (head == 0)
#pragma warning restore S1244
        {
            return;
        }

        // Reciprocal-multiply, not per-row division, mirrors LAPACK's dgetf2 (DSCAL by
        // ONE/AJJ): on the duplicated-column fixture it is what breaks a pivot tie scipy's way.
        double reciprocal = 1.0 / head;
        for (int i = k + 1; i < rows; i++)
        {
            double factor = work[(i * columns) + k] * reciprocal;
            work[(i * columns) + k] = factor;
            for (int j = k + 1; j < columns; j++)
            {
                work[(i * columns) + j] -= factor * work[(k * columns) + j];
            }
        }
    }

    private static double[] BuildPermutedLower(double[] work, int[] permutation, int rows, int columns)
    {
        // L is unit lower triangular in the eliminated block; P L puts each row back where
        // the pivoting took it from, which is what scipy's permute_l=True returns.
        double[] result = new double[rows * columns];
        for (int i = 0; i < rows; i++)
        {
            int target = permutation[i] * columns;
            for (int j = 0; j < columns && j < i; j++)
            {
                result[target + j] = work[(i * columns) + j];
            }
            if (i < columns)
            {
                result[target + i] = 1.0;
            }
        }
        return result;
    }

    private static void SwapRows(double[] block, int columns, int first, int second)
    {
        for (int j = 0; j < columns; j++)
        {
            (block[(first * columns) + j], block[(second * columns) + j]) =
                (block[(second * columns) + j], block[(first * columns) + j]);
        }
    }
}
