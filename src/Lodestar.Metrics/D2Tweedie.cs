using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The fraction of Tweedie deviance explained — <c>sklearn.metrics.d2_tweedie_score</c>.
/// </summary>
/// <remarks>
/// <see cref="R2"/>'s idea over a deviance instead of a squared error: one minus
/// the model's mean deviance over the mean deviance of predicting the weighted
/// average of the truth. At power 0 the two coincide exactly, since that regime's
/// deviance is the squared error.
/// </remarks>
public static class D2Tweedie
{
    /// <summary>
    /// The explained fraction — <c>d2_tweedie_score(y_true, y_pred, power=…, sample_weight=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true values.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="power">Which Tweedie deviance to explain; see <see cref="TweedieDeviance.Score"/> for the regimes and what each allows.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <param name="zeroDivision">What to answer for fewer than two samples, the case scikit-learn leaves undefined. The default reproduces its <c>nan</c>.</param>
    /// <returns><c>1</c> for a perfect prediction, <c>0</c> for one no better than the constant average, and negative below that.</returns>
    /// <exception cref="ArgumentException">The lengths disagree, the input is empty or non-finite, or an operand is outside <paramref name="power"/>'s domain.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="power"/> lies in the open interval (0, 1).</exception>
    /// <exception cref="UndefinedMetricException">Every truth is the same value, so the constant model is already perfect and there is no deviance to explain; or there are fewer than two samples and <paramref name="zeroDivision"/> is <see cref="ZeroDivision.Throw"/>.</exception>
    public static double Score(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        double power = 0.0,
        ReadOnlySpan<double> sampleWeight = default,
        ZeroDivision zeroDivision = ZeroDivision.NaN)
    {
        Inputs.Validate(yTrue, yPred, sampleWeight);
        Tweedie.Require(yTrue, yPred, power);

        if (yTrue.Length < 2)
        {
            return Prf.Undefined(zeroDivision, "D² Tweedie");
        }

        double numerator = Outputs.Score(yTrue, yPred, 1, sampleWeight, default, new Kernel(power));
        double denominator = Tweedie.DevianceAgainst(
            yTrue, Tweedie.Mean(yTrue, sampleWeight), power, sampleWeight);

        // S1244: whether the constant model left any deviance at all, which is what
        // scikit-learn divides by unguarded -- it raises ZeroDivisionError here
        // where d2_absolute_error_score masks the same case and answers 0.
#pragma warning disable S1244
        if (denominator == 0.0)
#pragma warning restore S1244
        {
            throw new UndefinedMetricException(
                "D² Tweedie is undefined here: every truth is the same value, so predicting their "
                + "average is already perfect and there is no deviance to explain. scikit-learn "
                + "divides by that zero and raises ZeroDivisionError.");
        }

        return 1.0 - (numerator / denominator);
    }

    private readonly struct Kernel(double power) : IResidualKernel
    {
        public double Apply(double truth, double prediction) => Tweedie.Deviance(truth, prediction, power);
    }
}
