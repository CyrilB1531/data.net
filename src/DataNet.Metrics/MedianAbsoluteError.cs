using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// The median of the absolute residuals — the equivalent of
/// <c>sklearn.metrics.median_absolute_error</c>.
/// </summary>
/// <remarks>
/// Under sample weights this is not the value at the halfway point: scikit-learn
/// takes an averaged weighted percentile, computed by <see cref="WeightedPercentile"/>.
/// </remarks>
public static class MedianAbsoluteError
{
    /// <summary>
    /// One number for the whole prediction —
    /// <c>median_absolute_error(y_true, y_pred, sample_weight=…, multioutput=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true values, row-major when there is more than one output.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="outputCount">How many outputs each row holds. One, the default, is the ordinary case.</param>
    /// <param name="sampleWeight">A weight per sample — per <em>row</em>, not per value. Omit to weight every sample by 1.</param>
    /// <param name="outputWeights">A weight per output (<c>multioutput=[…]</c>). Omit for <c>multioutput="uniform_average"</c>.</param>
    /// <exception cref="ArgumentException">A length disagrees with the shape, the input is empty, or it holds a non-finite value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below one.</exception>
    public static double Score(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount = 1,
        ReadOnlySpan<double> sampleWeight = default,
        ReadOnlySpan<double> outputWeights = default)
    {
        int samples = Outputs.Validate(yTrue, yPred, outputCount, sampleWeight, outputWeights);
        return Outputs.Reduce(Compute(yTrue, yPred, outputCount, sampleWeight, samples), outputWeights);
    }

    /// <summary>
    /// One number per output — <c>multioutput="raw_values"</c>.
    /// </summary>
    /// <param name="yTrue">The true values, row-major.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="outputCount">How many outputs each row holds.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <returns>A fresh array of <paramref name="outputCount"/> entries, in column order.</returns>
    /// <exception cref="ArgumentException">A length disagrees with the shape, the input is empty, or it holds a non-finite value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below one.</exception>
    public static double[] PerOutput(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount = 1,
        ReadOnlySpan<double> sampleWeight = default)
    {
        int samples = Outputs.Validate(yTrue, yPred, outputCount, sampleWeight, default);
        return Compute(yTrue, yPred, outputCount, sampleWeight, samples);
    }

    private static double[] Compute(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount,
        ReadOnlySpan<double> sampleWeight,
        int samples)
    {
        double[] result = new double[outputCount];
        double[] column = new double[samples];
        double[] weights = sampleWeight.IsEmpty ? [] : new double[samples];

        for (int col = 0; col < outputCount; col++)
        {
            for (int row = 0; row < samples; row++)
            {
                int offset = (row * outputCount) + col;
                column[row] = Math.Abs(yTrue[offset] - yPred[offset]);
                if (weights.Length != 0)
                {
                    // Re-copied per column: WeightedPercentile sorts both arrays,
                    // so the previous column left this one in residual order.
                    weights[row] = sampleWeight[row];
                }
            }

            result[col] = WeightedPercentile.Median(column, weights);
        }

        return result;
    }
}
