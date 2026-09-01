namespace Lodestar.Decomposition.Internal;

/// <summary>scikit-learn's <c>svd_flip</c>, so two runs agree on more than a subspace.</summary>
/// <remarks>
/// An SVD is unique only up to flipping the sign of a matched pair of vectors. <c>TruncatedSVD</c>
/// pins it on the <em>right</em> vectors — <c>svd_flip(..., u_based_decision=False)</c>, which is
/// what it has asked for since 1.6 — by making the largest-magnitude entry of each of them
/// positive. Every number this package reports downstream inherits that convention, and it is the
/// reason a component and its scikit-learn counterpart can be compared entry by entry.
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
}
