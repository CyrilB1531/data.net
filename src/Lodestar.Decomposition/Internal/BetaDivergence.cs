using Lodestar.Abstractions;

namespace Lodestar.Decomposition.Internal;

/// <summary>scikit-learn's <c>_beta_divergence(..., square_root=True)</c>.</summary>
/// <remarks>
/// Both branches avoid densifying <c>W H</c>: the Frobenius one expands the squared norm into
/// three traces, and the Kullback–Leibler one needs <c>W H</c> only where the matrix is non-zero
/// plus one rank-one correction for everywhere else.
/// </remarks>
internal static class BetaDivergence
{
    /// <summary><c>double.Epsilon</c> is not this: it is numpy's <c>finfo(float64).eps</c>.</summary>
    internal const double MachineEpsilon = 2.220446049250313e-16;

    internal static double Compute(
        CsrMatrix matrix, double[] w, double[] h, int componentCount, NmfBetaLoss loss)
    {
        double residual = loss == NmfBetaLoss.Frobenius
            ? Frobenius(matrix, w, h, componentCount)
            : KullbackLeibler(matrix, w, h, componentCount);

        // Rounding can push the residual just below zero on a near-perfect fit.
        return Math.Sqrt(2.0 * Math.Max(residual, 0));
    }

    private static double Frobenius(CsrMatrix matrix, double[] w, double[] h, int k)
    {
        // Expanded into three traces rather than a residual, so WH is never formed: the
        // squared norm of X, the trace of HᵀWᵀWH, and twice the trace of WᵀXHᵀ.
        double normX = 0;
        foreach (double value in matrix.Values)
        {
            normX += value * value;
        }

        return (normX + NormOfProduct(matrix, w, h, k) - (2.0 * Cross(matrix, w, h, k))) / 2.0;
    }

    /// <summary><c>tr(HᵀWᵀWH)</c>, through the two Gram matrices rather than through <c>W H</c>.</summary>
    private static double NormOfProduct(CsrMatrix matrix, double[] w, double[] h, int k)
    {
        double[] wtw = DenseBlock.TransposeGram(w, matrix.RowCount, k);

        double total = 0;
        int features = matrix.ColumnCount;
        for (int a = 0; a < k; a++)
        {
            for (int b = 0; b < k; b++)
            {
                double factor = wtw[(a * k) + b];
                double inner = 0;
                for (int j = 0; j < features; j++)
                {
                    inner += h[(a * features) + j] * h[(b * features) + j];
                }
                total += factor * inner;
            }
        }
        return total;
    }

    /// <summary><c>tr(WᵀXHᵀ)</c>, over the matrix's non-zeros only.</summary>
    private static double Cross(CsrMatrix matrix, double[] w, double[] h, int k)
    {
        int features = matrix.ColumnCount;
        double total = 0;
        for (int row = 0; row < matrix.RowCount; row++)
        {
            for (int index = matrix.RowPointers[row]; index < matrix.RowPointers[row + 1]; index++)
            {
                double value = matrix.Values[index];
                int column = matrix.ColumnIndices[index];
                for (int a = 0; a < k; a++)
                {
                    total += value * w[(row * k) + a] * h[(a * features) + column];
                }
            }
        }
        return total;
    }

    private static double KullbackLeibler(CsrMatrix matrix, double[] w, double[] h, int k)
    {
        int features = matrix.ColumnCount;

        double residual = 0;
        double dataSum = 0;
        for (int row = 0; row < matrix.RowCount; row++)
        {
            for (int index = matrix.RowPointers[row]; index < matrix.RowPointers[row + 1]; index++)
            {
                double value = matrix.Values[index];
                // A zero entry contributes nothing: 0 · log(0/x) is defined as 0 here, which
                // is what skipping it means.
                if (value <= MachineEpsilon)
                {
                    continue;
                }
                int column = matrix.ColumnIndices[index];
                double product = 0;
                for (int a = 0; a < k; a++)
                {
                    product += w[(row * k) + a] * h[(a * features) + column];
                }
                residual += value * Math.Log(value / Math.Max(product, MachineEpsilon));
                dataSum += value;
            }
        }

        // Σ WH over every cell, as (Σ columns of W) · (Σ rows of H) — a rank-one identity,
        // so the zeros cost nothing.
        double sumWh = 0;
        for (int a = 0; a < k; a++)
        {
            double columnSum = 0;
            for (int i = 0; i < matrix.RowCount; i++)
            {
                columnSum += w[(i * k) + a];
            }
            double rowSum = 0;
            for (int j = 0; j < features; j++)
            {
                rowSum += h[(a * features) + j];
            }
            sumWh += columnSum * rowSum;
        }

        return residual + sumWh - dataSum;
    }
}
