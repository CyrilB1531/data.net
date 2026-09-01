namespace Lodestar.Decomposition.Internal;

/// <summary>scikit-learn's <c>svd_flip</c>, so two runs agree on more than a subspace.</summary>
/// <remarks>
/// An SVD is unique only up to flipping the sign of a matched pair of vectors, and scikit-learn
/// pins it two different ways. <c>TruncatedSVD</c> asks for the <em>right</em> vectors — since 1.6
/// it takes <c>flip_sign=False</c> and then calls <c>svd_flip(..., u_based_decision=False)</c> —
/// while <c>randomized_svd</c>'s own default is the <em>left</em> ones, which is what NMF's
/// initialisation inherits. Both are correct and they disagree by a sign, so each is spelled out
/// here rather than left to whichever call site got there first.
/// </remarks>
internal static class SignFlip
{
    /// <summary>Flips each row of <paramref name="vt"/> whose largest-magnitude entry is negative.</summary>
    internal static void Apply(double[] vt, int rows, int columns)
    {
        for (int row = 0; row < rows; row++)
        {
            int offset = row * columns;
            int largest = 0;
            double best = -1;
            for (int column = 0; column < columns; column++)
            {
                double magnitude = Math.Abs(vt[offset + column]);
                if (magnitude > best)
                {
                    best = magnitude;
                    largest = column;
                }
            }

            // numpy multiplies by Math.Sign, which zeroes an all-zero row rather than
            // leaving it alone; a vector of a rank-deficient factor is left as it is.
            if (vt[offset + largest] >= 0)
            {
                continue;
            }

            for (int column = 0; column < columns; column++)
            {
                vt[offset + column] = -vt[offset + column];
            }
        }
    }

    /// <summary>Flips each column of <paramref name="u"/> whose largest-magnitude entry is negative, and the matching row of <paramref name="vt"/>.</summary>
    /// <remarks>
    /// <c>svd_flip(u, vt)</c> with <c>u_based_decision</c> at its default, which is what
    /// <c>randomized_svd</c> applies unless a caller opts out.
    /// </remarks>
    internal static void Apply(double[] u, int rows, int columns, double[] vt, int vtColumns)
    {
        for (int column = 0; column < columns; column++)
        {
            int largest = 0;
            double best = -1;
            for (int row = 0; row < rows; row++)
            {
                double magnitude = Math.Abs(u[(row * columns) + column]);
                if (magnitude > best)
                {
                    best = magnitude;
                    largest = row;
                }
            }

            // As above: numpy's sign zeroes an all-zero column instead of leaving it alone,
            // and a vector of a rank-deficient factor is left as it is.
            if (u[(largest * columns) + column] >= 0)
            {
                continue;
            }

            for (int row = 0; row < rows; row++)
            {
                u[(row * columns) + column] = -u[(row * columns) + column];
            }
            int offset = column * vtColumns;
            for (int feature = 0; feature < vtColumns; feature++)
            {
                vt[offset + feature] = -vt[offset + feature];
            }
        }
    }
}
