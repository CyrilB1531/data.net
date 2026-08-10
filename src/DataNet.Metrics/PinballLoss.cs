using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// The mean pinball (quantile) loss — the equivalent of
/// <c>sklearn.metrics.mean_pinball_loss</c>.
/// </summary>
public static class PinballLoss
{
    /// <summary>
    /// One number for the whole prediction —
    /// <c>mean_pinball_loss(y_true, y_pred, alpha=…, sample_weight=…, multioutput=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true values, row-major when there is more than one output.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="alpha">The quantile being scored, in <c>[0, 1]</c>. <c>0.5</c>, the default, is half the mean absolute error.</param>
    /// <param name="outputCount">How many outputs each row holds. One, the default, is the ordinary case.</param>
    /// <param name="sampleWeight">A weight per sample — per <em>row</em>, not per value. Omit to weight every sample by 1.</param>
    /// <param name="outputWeights">A weight per output (<c>multioutput=[…]</c>). Omit for <c>multioutput="uniform_average"</c>.</param>
    /// <exception cref="ArgumentException">A length disagrees with the shape, the input is empty, or it holds a non-finite value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below one, or <paramref name="alpha"/> is outside <c>[0, 1]</c> (including <c>NaN</c>).</exception>
    public static double Score(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        double alpha = 0.5,
        int outputCount = 1,
        ReadOnlySpan<double> sampleWeight = default,
        ReadOnlySpan<double> outputWeights = default)
    {
        RequireUnitInterval(alpha);
        int samples = Outputs.Validate(yTrue, yPred, outputCount, sampleWeight, outputWeights);
        return Outputs.Reduce(Compute(yTrue, yPred, alpha, outputCount, sampleWeight, samples), outputWeights);
    }

    /// <summary>
    /// One number per output — <c>multioutput="raw_values"</c>.
    /// </summary>
    /// <param name="yTrue">The true values, row-major.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="alpha">The quantile being scored, in <c>[0, 1]</c>. <c>0.5</c>, the default, is half the mean absolute error.</param>
    /// <param name="outputCount">How many outputs each row holds.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <returns>A fresh array of <paramref name="outputCount"/> entries, in column order.</returns>
    /// <exception cref="ArgumentException">A length disagrees with the shape, the input is empty, or it holds a non-finite value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below one, or <paramref name="alpha"/> is outside <c>[0, 1]</c> (including <c>NaN</c>).</exception>
    public static double[] PerOutput(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        double alpha = 0.5,
        int outputCount = 1,
        ReadOnlySpan<double> sampleWeight = default)
    {
        RequireUnitInterval(alpha);
        int samples = Outputs.Validate(yTrue, yPred, outputCount, sampleWeight, default);
        return Compute(yTrue, yPred, alpha, outputCount, sampleWeight, samples);
    }

    private static double[] Compute(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        double alpha,
        int outputCount,
        ReadOnlySpan<double> sampleWeight,
        int samples)
    {
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
                double residual = yTrue[offset + col] - yPred[offset + col];
                double under = alpha * residual;
                double over = (alpha - 1.0) * residual;
                result[col] += weight * (under > over ? under : over);
            }
        }

        for (int col = 0; col < outputCount; col++)
        {
            result[col] /= totalWeight;
        }
        return result;
    }

    /// <summary>
    /// Refuses a quantile outside the closed unit interval, <c>NaN</c> included.
    /// </summary>
    /// <remarks>
    /// Written as <c>!(alpha &gt;= 0.0 &amp;&amp; alpha &lt;= 1.0)</c> rather than
    /// <c>alpha &lt; 0.0 || alpha &gt; 1.0</c>: every comparison against
    /// <c>NaN</c> is false, so the negated form is the only one of the two that
    /// rejects it. scikit-learn's range is closed, so <c>0.0</c> and <c>1.0</c>
    /// are legal quantiles.
    /// </remarks>
    private static void RequireUnitInterval(double alpha)
    {
        if (!(alpha >= 0.0 && alpha <= 1.0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(alpha), alpha, "The quantile must be in [0, 1].");
        }
    }
}
