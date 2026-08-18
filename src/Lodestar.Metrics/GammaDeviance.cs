using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The mean gamma deviance — <c>sklearn.metrics.mean_gamma_deviance</c>, which
/// is <see cref="TweedieDeviance"/> at power 2.
/// </summary>
/// <remarks>
/// Scale-invariant, unlike the Poisson: multiplying both arguments by the same
/// factor leaves the number where it was, which is what makes it the deviance
/// for a positive quantity with no natural unit.
/// </remarks>
public static class GammaDeviance
{
    /// <summary>The mean deviance — <c>mean_gamma_deviance(y_true, y_pred, sample_weight=…)</c>.</summary>
    /// <param name="yTrue">The true values. Must be strictly positive.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>. Must be strictly positive.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <returns><c>0</c> for a perfect prediction, and larger the worse it is.</returns>
    /// <exception cref="ArgumentException">The lengths disagree, the input is empty or non-finite, or either operand is not strictly positive.</exception>
    public static double Score(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        ReadOnlySpan<double> sampleWeight = default) =>
        TweedieDeviance.Score(yTrue, yPred, 2.0, sampleWeight);
}
