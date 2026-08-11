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
        return Outputs.Score(yTrue, yPred, outputCount, sampleWeight, outputWeights, new Quantile(alpha));
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
        return Outputs.PerOutput(yTrue, yPred, outputCount, sampleWeight, new Quantile(alpha));
    }

    /// <summary>
    /// The pinball loss at one quantile: the residual charged <c>alpha</c> when
    /// the prediction fell short and <c>1 - alpha</c> when it overshot.
    /// </summary>
    /// <remarks>
    /// Written as the larger of two products rather than through
    /// <c>Math.Sign</c>, which is the same number on both sides of zero and one
    /// comparison instead of a branch and a multiply. The struct carries
    /// <c>alpha</c>, which is why this kernel is constructed where the other
    /// four are <see langword="default"/>.
    /// </remarks>
    private readonly struct Quantile(double alpha) : IResidualKernel
    {
        public double Apply(double truth, double prediction)
        {
            double residual = truth - prediction;
            double under = alpha * residual;
            double over = (alpha - 1.0) * residual;
            return under > over ? under : over;
        }
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
