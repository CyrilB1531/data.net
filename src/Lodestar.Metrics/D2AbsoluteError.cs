namespace Lodestar.Metrics;

/// <summary>
/// The fraction of absolute error explained —
/// <c>sklearn.metrics.d2_absolute_error_score</c>.
/// </summary>
/// <remarks>
/// <see cref="D2Pinball"/> at <c>alpha = 0.5</c>: one minus the model's mean
/// absolute error over that of predicting the weighted median of the truth. Where
/// <see cref="R2"/> compares against the mean and so is pulled by an outlier, this
/// compares against the median and is not.
/// </remarks>
public static class D2AbsoluteError
{
    /// <summary>
    /// One number for the whole prediction —
    /// <c>d2_absolute_error_score(y_true, y_pred, sample_weight=…, multioutput=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true values, row-major when there is more than one output.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="outputCount">How many outputs each row holds. One, the default, is the ordinary case.</param>
    /// <param name="sampleWeight">A weight per sample — per <em>row</em>, not per value. Omit to weight every sample by 1.</param>
    /// <param name="outputWeights">A weight per output (<c>multioutput=[…]</c>). Omit for <c>multioutput="uniform_average"</c>.</param>
    /// <param name="zeroDivision">What to answer for fewer than two samples. The default reproduces scikit-learn's <c>nan</c>.</param>
    /// <returns><c>1</c> for a perfect prediction, <c>0</c> for one no better than the median, and negative below that.</returns>
    /// <exception cref="ArgumentException">A length disagrees with the shape, the input is empty, or it holds a non-finite value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below one.</exception>
    /// <exception cref="UndefinedMetricException">There are fewer than two samples and <paramref name="zeroDivision"/> is <see cref="ZeroDivision.Throw"/>.</exception>
    public static double Score(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount = 1,
        ReadOnlySpan<double> sampleWeight = default,
        ReadOnlySpan<double> outputWeights = default,
        ZeroDivision zeroDivision = ZeroDivision.NaN) =>
        D2Pinball.Score(yTrue, yPred, 0.5, outputCount, sampleWeight, outputWeights, zeroDivision);

    /// <summary>One number per output — <c>multioutput="raw_values"</c>.</summary>
    /// <param name="yTrue">The true values, row-major.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="outputCount">How many outputs each row holds.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <param name="zeroDivision">The answer for fewer than two samples. See <see cref="Score"/>.</param>
    /// <returns>A fresh array of <paramref name="outputCount"/> entries, in column order.</returns>
    /// <exception cref="ArgumentException">A length disagrees with the shape, the input is empty, or it holds a non-finite value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below one.</exception>
    /// <exception cref="UndefinedMetricException">There are fewer than two samples and <paramref name="zeroDivision"/> is <see cref="ZeroDivision.Throw"/>.</exception>
    public static double[] PerOutput(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount = 1,
        ReadOnlySpan<double> sampleWeight = default,
        ZeroDivision zeroDivision = ZeroDivision.NaN) =>
        D2Pinball.PerOutput(yTrue, yPred, 0.5, outputCount, sampleWeight, zeroDivision);
}
