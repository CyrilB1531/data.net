using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The mean Tweedie deviance — <c>sklearn.metrics.mean_tweedie_deviance</c>.
/// </summary>
/// <remarks>
/// One function with a <c>power</c>, not three: <see cref="PoissonDeviance"/> and
/// <see cref="GammaDeviance"/> are this at 1 and 2, which is how the reference
/// defines them too. The domain of validity moves with the power, and the table
/// of what each regime allows lives in <see cref="Score"/>.
/// </remarks>
public static class TweedieDeviance
{
    /// <summary>
    /// The mean deviance — <c>mean_tweedie_deviance(y_true, y_pred, power=…, sample_weight=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true values.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="power">Which Tweedie distribution's deviance to take: 0 normal, 1 Poisson, (1, 2) compound Poisson-gamma, 2 gamma, 3 inverse gaussian. The open interval (0, 1) names no distribution and is refused.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <returns><c>0</c> for a perfect prediction, and larger the worse it is. Unbounded above.</returns>
    /// <exception cref="ArgumentException">The lengths disagree, the input is empty or non-finite, or an operand is outside what <paramref name="power"/>'s regime allows: any real against a strictly positive prediction below 0, anything at all at 0, a non-negative truth against a strictly positive prediction in [1, 2), and both strictly positive from 2 up.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="power"/> lies in the open interval (0, 1).</exception>
    public static double Score(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        double power = 0.0,
        ReadOnlySpan<double> sampleWeight = default)
    {
        Inputs.Validate(yTrue, yPred, sampleWeight);
        Tweedie.Require(yTrue, yPred, power);
        return Outputs.Score(yTrue, yPred, 1, sampleWeight, default, new Kernel(power));
    }

    private readonly struct Kernel(double power) : IResidualKernel
    {
        public double Apply(double truth, double prediction) => Tweedie.Deviance(truth, prediction, power);
    }
}
