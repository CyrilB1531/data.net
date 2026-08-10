using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// The mean of the squared residuals of <c>log(1 + y)</c> — the equivalent of
/// <c>sklearn.metrics.mean_squared_log_error</c>.
/// </summary>
public static class MeanSquaredLogError
{
    /// <summary>
    /// One number for the whole prediction —
    /// <c>mean_squared_log_error(y_true, y_pred, sample_weight=…, multioutput=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true values, row-major when there is more than one output.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="outputCount">How many outputs each row holds. One, the default, is the ordinary case.</param>
    /// <param name="sampleWeight">A weight per sample — per <em>row</em>, not per value. Omit to weight every sample by 1.</param>
    /// <param name="outputWeights">A weight per output (<c>multioutput=[…]</c>). Omit for <c>multioutput="uniform_average"</c>.</param>
    /// <exception cref="ArgumentException">
    /// A length disagrees with the shape, the input is empty, it holds a
    /// non-finite value, or either array holds a value at or below −1.
    /// </exception>
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
    /// <exception cref="ArgumentException">
    /// A length disagrees with the shape, the input is empty, it holds a
    /// non-finite value, or either array holds a value at or below −1.
    /// </exception>
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
        RequireAboveMinusOne(yTrue, nameof(yTrue));
        RequireAboveMinusOne(yPred, nameof(yPred));

        double[] result = new double[outputCount];
        bool weighted = !sampleWeight.IsEmpty;
        double totalWeight = 0.0;

        for (int row = 0; row < samples; row++)
        {
            double weight = weighted ? sampleWeight[row] : 1.0;
            totalWeight += weight;
            int offset = row * outputCount;
            for (int col = 0; col < outputCount; col++)
            {
                double residual = Math.Log(1.0 + yTrue[offset + col]) - Math.Log(1.0 + yPred[offset + col]);
                result[col] += weight * residual * residual;
            }
        }

        for (int col = 0; col < outputCount; col++)
        {
            result[col] /= totalWeight;
        }
        return result;
    }

    /// <summary>
    /// Refuses a target scikit-learn refuses, naming the side it found it on.
    /// </summary>
    /// <remarks>
    /// scikit-learn raises one message for both sides. Naming the side is an
    /// improvement on a message that leaves the caller to work it out, and costs
    /// no parity: no value is returned either way. The comparison is
    /// <c>&lt;= -1</c> and not <c>&lt; -1</c> because <c>log(1 + -1)</c> is
    /// <c>log(0)</c>, which is negative infinity rather than an error.
    /// </remarks>
    private static void RequireAboveMinusOne(ReadOnlySpan<double> values, string paramName)
    {
        foreach (double value in values)
        {
            if (value <= -1.0)
            {
                throw new ArgumentException(
                    "Mean Squared Logarithmic Error cannot be used when targets contain values "
                    + $"less than or equal to -1; {paramName} holds {value}.",
                    paramName);
            }
        }
    }
}
