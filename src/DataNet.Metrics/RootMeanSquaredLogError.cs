using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// The square root of the mean squared log error — the equivalent of
/// <c>sklearn.metrics.root_mean_squared_log_error</c>.
/// </summary>
/// <remarks>
/// A type of its own rather than a flag on <see cref="MeanSquaredLogError"/>,
/// for the same reason as <see cref="RootMeanSquaredError"/>: scikit-learn
/// exposes this as its own function rather than a <c>squared</c> parameter.
/// </remarks>
public static class RootMeanSquaredLogError
{
    /// <summary>
    /// One number for the whole prediction —
    /// <c>root_mean_squared_log_error(y_true, y_pred, sample_weight=…, multioutput=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true values, row-major when there is more than one output.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="outputCount">How many outputs each row holds.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <param name="outputWeights">A weight per output. Omit for <c>multioutput="uniform_average"</c>.</param>
    /// <remarks>
    /// The root is taken per output and the reduction runs on the roots, which
    /// is scikit-learn's order and is not the same number as the root of the
    /// reduced mean squared log error whenever the outputs differ.
    /// </remarks>
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
        // PerOutput already validates through MeanSquaredLogError.PerOutput, but
        // that call never sees outputWeights, so nothing checks it unless this
        // does too. The cost is a second O(n) finiteness pass on this type only
        // — MeanSquaredLogError, the one the benchmark measures, does not pay it.
        Outputs.Validate(yTrue, yPred, outputCount, sampleWeight, outputWeights);
        return Outputs.Reduce(PerOutput(yTrue, yPred, outputCount, sampleWeight), outputWeights);
    }

    /// <summary>One root per output — <c>multioutput="raw_values"</c>.</summary>
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
        ReadOnlySpan<double> sampleWeight = default) =>
        Outputs.SquareRoots(MeanSquaredLogError.PerOutput(yTrue, yPred, outputCount, sampleWeight));
}
