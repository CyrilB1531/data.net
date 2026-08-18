using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The mean Poisson deviance — <c>sklearn.metrics.mean_poisson_deviance</c>,
/// which is <see cref="TweedieDeviance"/> at power 1.
/// </summary>
/// <remarks>
/// A type of its own because the reference exposes one, and because a caller
/// counting events should not have to know that 1 is the Poisson. The refusal
/// sentence still names the power, as scikit-learn's does.
/// </remarks>
public static class PoissonDeviance
{
    /// <summary>The mean deviance — <c>mean_poisson_deviance(y_true, y_pred, sample_weight=…)</c>.</summary>
    /// <param name="yTrue">The true values. Must be non-negative.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>. Must be strictly positive.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <returns><c>0</c> for a perfect prediction, and larger the worse it is.</returns>
    /// <exception cref="ArgumentException">The lengths disagree, the input is empty or non-finite, a truth is negative, or a prediction is not strictly positive.</exception>
    public static double Score(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        ReadOnlySpan<double> sampleWeight = default) =>
        TweedieDeviance.Score(yTrue, yPred, 1.0, sampleWeight);
}
