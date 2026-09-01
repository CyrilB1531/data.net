using Lodestar.Abstractions;

namespace Lodestar.Decomposition.Internal;

/// <summary>Non-negative double SVD: a deterministic starting point for NMF.</summary>
/// <remarks>
/// Each singular triplet after the first is split into its positive and its negative part, and
/// whichever carries more energy becomes the component. The leading triplet needs no such choice —
/// Perron–Frobenius makes it non-negative already for a non-negative matrix.
/// </remarks>
internal static class NndSvd
{
    // _initialize_nmf's eps default: anything below it is snapped to zero, which is what
    // keeps NndSvd sparse instead of dusted with rounding noise.
    private const double Epsilon = 1e-6;

    // _initialize_nmf calls randomized_svd with its own defaults, not TruncatedSVD's.
    private const int Oversampling = 10;

    /// <summary>Returns <c>W</c>, row-major <c>rows × componentCount</c>, and <c>H</c>, row-major <c>componentCount × features</c>.</summary>
    internal static (double[] W, double[] H) Initialize(
        CsrMatrix matrix,
        int componentCount,
        NmfInitialization initialization,
        int seed,
        double[]? randomMatrix)
    {
        int rows = matrix.RowCount;
        int features = matrix.ColumnCount;
        int size = componentCount + Oversampling;
        double[] omega = randomMatrix ?? new GaussianSampler(seed).Normal(features, size);

        (double[] u, double[] s, double[] vt, int rank) = RandomizedSvd.Compute(
            matrix, componentCount, Oversampling, PowerIterations(matrix, componentCount),
            PowerIterationNormalizer.Auto, omega);
        // _initialize_nmf calls _randomized_svd with flip_sign at its default, so the
        // initialisation inherits the LEFT-based convention, not the estimator's.
        SignFlip.Apply(u, rows, rank, vt, features);

        double[] w = new double[checked(rows * componentCount)];
        double[] h = new double[checked(componentCount * features)];

        double leading = Math.Sqrt(s[0]);
        for (int i = 0; i < rows; i++)
        {
            w[i * componentCount] = leading * Math.Abs(u[i * rank]);
        }
        for (int j = 0; j < features; j++)
        {
            h[j] = leading * Math.Abs(vt[j]);
        }

        for (int component = 1; component < componentCount; component++)
        {
            double[] left = Column(u, rows, rank, component);
            double[] right = Row(vt, features, component);

            (double[] leftPart, double leftNorm, double[] rightPart, double rightNorm) =
                Dominant(left, right);

            // scikit-learn's `lbd`, not its `sigma` -- there sigma is the product of the
            // two norms and lbd is the square root of it times the singular value.
            double lambda = Math.Sqrt(s[component] * leftNorm * rightNorm);
            for (int i = 0; i < rows; i++)
            {
                w[(i * componentCount) + component] = lambda * leftPart[i] / leftNorm;
            }
            for (int j = 0; j < features; j++)
            {
                h[(component * features) + j] = lambda * rightPart[j] / rightNorm;
            }
        }

        Snap(w);
        Snap(h);

        if (initialization == NmfInitialization.NndSvda)
        {
            double average = Average(matrix);
            Fill(w, average);
            Fill(h, average);
        }
        return (w, h);
    }

    /// <summary>scikit-learn's <c>n_iter="auto"</c>: 7 for a small rank, 4 otherwise.</summary>
    private static int PowerIterations(CsrMatrix matrix, int componentCount) =>
        componentCount < 0.1 * Math.Min(matrix.RowCount, matrix.ColumnCount) ? 7 : 4;

    /// <summary>The heavier of the positive and the negative part of a matched pair.</summary>
    private static (double[] Left, double LeftNorm, double[] Right, double RightNorm) Dominant(
        double[] left, double[] right)
    {
        (double[] leftPositive, double leftPositiveNorm) = Positive(left);
        (double[] rightPositive, double rightPositiveNorm) = Positive(right);
        (double[] leftNegative, double leftNegativeNorm) = Negative(left);
        (double[] rightNegative, double rightNegativeNorm) = Negative(right);

        return leftPositiveNorm * rightPositiveNorm > leftNegativeNorm * rightNegativeNorm
            ? (leftPositive, leftPositiveNorm, rightPositive, rightPositiveNorm)
            : (leftNegative, leftNegativeNorm, rightNegative, rightNegativeNorm);
    }

    private static (double[] Part, double Norm) Positive(double[] vector)
    {
        double[] part = new double[vector.Length];
        double sum = 0;
        for (int i = 0; i < vector.Length; i++)
        {
            part[i] = Math.Max(vector[i], 0);
            sum += part[i] * part[i];
        }
        return (part, Math.Sqrt(sum));
    }

    private static (double[] Part, double Norm) Negative(double[] vector)
    {
        double[] part = new double[vector.Length];
        double sum = 0;
        for (int i = 0; i < vector.Length; i++)
        {
            part[i] = Math.Abs(Math.Min(vector[i], 0));
            sum += part[i] * part[i];
        }
        return (part, Math.Sqrt(sum));
    }

    private static double[] Column(double[] block, int rows, int columns, int column)
    {
        double[] result = new double[rows];
        for (int i = 0; i < rows; i++)
        {
            result[i] = block[(i * columns) + column];
        }
        return result;
    }

    private static double[] Row(double[] block, int columns, int row)
    {
        double[] result = new double[columns];
        Array.Copy(block, row * columns, result, 0, columns);
        return result;
    }

    private static void Snap(double[] block)
    {
        for (int i = 0; i < block.Length; i++)
        {
            if (block[i] < Epsilon)
            {
                block[i] = 0;
            }
        }
    }

    private static void Fill(double[] block, double value)
    {
        for (int i = 0; i < block.Length; i++)
        {
            // S1244: the zeros this fills are the ones Snap wrote, an exact assignment,
            // and numpy's own `W[W == 0] = avg` compares the same way.
#pragma warning disable S1244
            if (block[i] == 0)
#pragma warning restore S1244
            {
                block[i] = value;
            }
        }
    }

    /// <summary>The mean over every cell, zeros included — <c>X.mean()</c>, not the non-zeros'.</summary>
    private static double Average(CsrMatrix matrix)
    {
        double sum = 0;
        foreach (double value in matrix.Values)
        {
            sum += value;
        }
        return sum / ((double)matrix.RowCount * matrix.ColumnCount);
    }
}
