using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The fraction of pinball loss explained — <c>sklearn.metrics.d2_pinball_score</c>.
/// </summary>
/// <remarks>
/// One minus the model's <see cref="PinballLoss"/> over the loss of predicting a
/// constant: the weighted quantile of the truth at the same alpha. At
/// <c>alpha = 0.5</c> it is <see cref="D2AbsoluteError"/> exactly, which is an
/// invariant a test holds rather than an oracle.
/// </remarks>
public static class D2Pinball
{
    /// <summary>
    /// One number for the whole prediction —
    /// <c>d2_pinball_score(y_true, y_pred, alpha=…, sample_weight=…, multioutput=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true values, row-major when there is more than one output.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="alpha">The quantile being scored, in <c>[0, 1]</c>. <c>0.5</c>, the default, is <see cref="D2AbsoluteError"/>.</param>
    /// <param name="outputCount">How many outputs each row holds. One, the default, is the ordinary case.</param>
    /// <param name="sampleWeight">A weight per sample — per <em>row</em>, not per value. Omit to weight every sample by 1.</param>
    /// <param name="outputWeights">A weight per output (<c>multioutput=[…]</c>). Omit for <c>multioutput="uniform_average"</c>.</param>
    /// <param name="zeroDivision">What to answer for fewer than two samples, the case scikit-learn leaves undefined. The default reproduces its <c>nan</c>.</param>
    /// <returns><c>1</c> for a perfect prediction, <c>0</c> for one no better than the best constant, and negative below that. A column whose truth never varies scores <c>0</c>.</returns>
    /// <exception cref="ArgumentException">A length disagrees with the shape, the input is empty, or it holds a non-finite value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below one, or <paramref name="alpha"/> is outside <c>[0, 1]</c> (including <c>NaN</c>).</exception>
    /// <exception cref="UndefinedMetricException">There are fewer than two samples and <paramref name="zeroDivision"/> is <see cref="ZeroDivision.Throw"/>.</exception>
    public static double Score(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        double alpha = 0.5,
        int outputCount = 1,
        ReadOnlySpan<double> sampleWeight = default,
        ReadOnlySpan<double> outputWeights = default,
        ZeroDivision zeroDivision = ZeroDivision.NaN)
    {
        RequireUnitInterval(alpha);
        Outputs.Validate(yTrue, yPred, outputCount, sampleWeight, outputWeights);
        return Outputs.Reduce(
            D2Quantile.PerOutput(yTrue, yPred, alpha, outputCount, sampleWeight, zeroDivision), outputWeights);
    }

    /// <summary>One number per output — <c>multioutput="raw_values"</c>.</summary>
    /// <param name="yTrue">The true values, row-major.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="alpha">The quantile being scored, in <c>[0, 1]</c>.</param>
    /// <param name="outputCount">How many outputs each row holds.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <param name="zeroDivision">The answer for fewer than two samples. See <see cref="Score"/>.</param>
    /// <returns>A fresh array of <paramref name="outputCount"/> entries, in column order.</returns>
    /// <exception cref="ArgumentException">A length disagrees with the shape, the input is empty, or it holds a non-finite value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below one, or <paramref name="alpha"/> is outside <c>[0, 1]</c> (including <c>NaN</c>).</exception>
    /// <exception cref="UndefinedMetricException">There are fewer than two samples and <paramref name="zeroDivision"/> is <see cref="ZeroDivision.Throw"/>.</exception>
    public static double[] PerOutput(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        double alpha = 0.5,
        int outputCount = 1,
        ReadOnlySpan<double> sampleWeight = default,
        ZeroDivision zeroDivision = ZeroDivision.NaN)
    {
        RequireUnitInterval(alpha);
        return D2Quantile.PerOutput(yTrue, yPred, alpha, outputCount, sampleWeight, zeroDivision);
    }

    private static void RequireUnitInterval(double alpha)
    {
        if (!(alpha >= 0.0 && alpha <= 1.0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(alpha), alpha, "The quantile must be in [0, 1].");
        }
    }
}
