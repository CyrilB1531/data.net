namespace Lodestar.Decomposition.Internal;

/// <summary>The economic QR of a tall block, by Householder reflections.</summary>
/// <remarks>
/// Gram–Schmidt is shorter but loses orthogonality on nearly-parallel columns — exactly what a
/// range finder produces; Householder is unconditionally stable at the same cost. Its factors'
/// signs are not LAPACK's: a flip <c>Q → QD</c> leaves <c>B = QᵀA</c> as <c>DB</c>, whose SVD
/// returns <c>DŨ</c>, and <c>QD · DŨ = QŨ</c> — invariant to what the composed algorithm relies on.
/// </remarks>
internal static class HouseholderQr
{
    /// <summary>Factors a row-major <c>rows × columns</c> block with <c>rows >= columns</c>.</summary>
    internal static (double[] Q, double[] R) Decompose(ReadOnlySpan<double> a, int rows, int columns)
    {
        if (rows < columns)
        {
            throw new ArgumentException(
                $"An economic QR needs at least as many rows as columns; got {rows} × {columns}.",
                nameof(a));
        }
        if (a.Length != checked(rows * columns))
        {
            throw new ArgumentException(
                $"Block length {a.Length} != {rows} × {columns}.", nameof(a));
        }

        // Work in place on a copy: the reflectors are applied to it, and what is left
        // above the diagonal is R.
        double[] work = a.ToArray();
        Reflector[] reflectors = Factorize(work, rows, columns);
        return (ComposeQ(reflectors, rows, columns), ExtractR(work, columns));
    }

    /// <summary>Reduces <paramref name="work"/> to upper-triangular in place, one column at a time.</summary>
    private static Reflector[] Factorize(double[] work, int rows, int columns)
    {
        var reflectors = new Reflector[columns];
        for (int k = 0; k < columns; k++)
        {
            Reflector reflector = BuildReflector(work, rows, columns, k);
            reflectors[k] = reflector;

            // S1244: whether the reflector is the identity (an already-zero column), not
            // whether two computed quantities are close.
#pragma warning disable S1244
            if (reflector.NormSquared != 0)
#pragma warning restore S1244
            {
                ApplyLeft(work, rows, columns, k, reflector.V, reflector.NormSquared);
            }
        }
        return reflectors;
    }

    /// <summary>The Householder vector that zeroes column <paramref name="k"/> below its diagonal.</summary>
    private static Reflector BuildReflector(double[] work, int rows, int columns, int k)
    {
        double[] v = new double[rows - k];
        double norm = 0;
        for (int i = k; i < rows; i++)
        {
            double value = work[(i * columns) + k];
            v[i - k] = value;
            norm += value * value;
        }
        norm = Math.Sqrt(norm);

        // A zero column is already reduced. Skipping it keeps a rank-deficient block
        // finite instead of dividing by zero and filling R with NaN.
        //
        // S1244: whether the column vanished entirely, not whether two computed
        // quantities are close.
#pragma warning disable S1244
        if (norm == 0)
#pragma warning restore S1244
        {
            return new Reflector(v, 0);
        }

        double alpha = v[0] >= 0 ? -norm : norm;
        v[0] -= alpha;
        return new Reflector(v, SquaredNorm(v));
    }

    /// <summary>Q is the reflectors applied, in reverse, to the identity's leading columns.</summary>
    /// <remarks>
    /// Never formed as a <c>rows × rows</c> matrix, which is the whole point of "thin".
    /// </remarks>
    private static double[] ComposeQ(Reflector[] reflectors, int rows, int columns)
    {
        double[] q = new double[rows * columns];
        for (int j = 0; j < columns; j++)
        {
            q[(j * columns) + j] = 1.0;
        }
        for (int k = columns - 1; k >= 0; k--)
        {
            Reflector reflector = reflectors[k];

            // S1244: whether the reflector is the identity (an already-zero column), not
            // whether two computed quantities are close.
#pragma warning disable S1244
            if (reflector.NormSquared != 0)
#pragma warning restore S1244
            {
                ApplyLeft(q, rows, columns, k, reflector.V, reflector.NormSquared);
            }
        }
        return q;
    }

    /// <summary>Copies the upper triangle work was reduced to into its own <c>columns × columns</c> block.</summary>
    private static double[] ExtractR(double[] work, int columns)
    {
        double[] r = new double[columns * columns];
        for (int i = 0; i < columns; i++)
        {
            for (int j = i; j < columns; j++)
            {
                r[(i * columns) + j] = work[(i * columns) + j];
            }
        }
        return r;
    }

    /// <summary>Applies <c>I - 2vvᵀ/vᵀv</c> to the trailing rows of every column.</summary>
    private static void ApplyLeft(
        double[] block, int rows, int columns, int from, double[] v, double vNormSquared)
    {
        for (int j = 0; j < columns; j++)
        {
            double dot = 0;
            for (int i = from; i < rows; i++)
            {
                dot += v[i - from] * block[(i * columns) + j];
            }
            double scale = 2.0 * dot / vNormSquared;
            for (int i = from; i < rows; i++)
            {
                block[(i * columns) + j] -= scale * v[i - from];
            }
        }
    }

    private static double SquaredNorm(double[] v)
    {
        double sum = 0;
        foreach (double value in v)
        {
            sum += value * value;
        }
        return sum;
    }

    /// <summary>One column's Householder vector, and its squared norm so <see cref="ApplyLeft"/>
    /// never recomputes it.</summary>
    /// <remarks>
    /// <c>NormSquared == 0</c> marks a column already reduced to zero — an exact test, not an
    /// approximate one: it is what tells <see cref="ApplyLeft"/> to skip the reflector rather
    /// than divide by it.
    /// </remarks>
    private readonly struct Reflector(double[] v, double normSquared)
    {
        internal double[] V { get; } = v;

        internal double NormSquared { get; } = normSquared;
    }
}
