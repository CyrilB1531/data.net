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
        RequireAboveMinusOne(yTrue, nameof(yTrue));
        RequireAboveMinusOne(yPred, nameof(yPred));
        return Outputs.Reduce(
            Outputs.WeightedMean<LogResidual>(yTrue, yPred, outputCount, sampleWeight, samples),
            outputWeights);
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
        RequireAboveMinusOne(yTrue, nameof(yTrue));
        RequireAboveMinusOne(yPred, nameof(yPred));
        return Outputs.WeightedMean<LogResidual>(yTrue, yPred, outputCount, sampleWeight, samples);
    }

    /// <summary>The squared residual in log space, which is what the log error is.</summary>
    private readonly struct LogResidual : IResidualKernel
    {
        public double Apply(double truth, double prediction)
        {
            double residual = Log1P(truth) - Log1P(prediction);
            return residual * residual;
        }

        /// <summary>
        /// <c>log(1 + x)</c> without the cancellation that spelling it that way
        /// costs — numpy's <c>log1p</c>, which is what scikit-learn calls.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Writing <c>Math.Log(1.0 + value)</c> loses the low bits of a small
        /// <paramref name="value"/> in the addition, before the logarithm ever sees
        /// them. Measured against scikit-learn on targets around 1e-9, that spelling
        /// is out by 1.4e-8 relative; this one agrees to a unit in the last place.
        /// </para>
        /// <para>
        /// Kahan's identity recovers the lost bits by scaling by the ratio the
        /// rounded addition actually represents: <c>u = 1 + v</c> rounds, but
        /// <c>u - 1</c> recovers exactly what was added, so <c>log(u)·v/(u - 1)</c>
        /// corrects for it. The <c>u == 1</c> branch is where <c>v</c> vanished
        /// entirely and <c>log(1 + v) ≈ v</c> to full precision. Written out rather
        /// than delegated because <c>netstandard2.0</c> has no <c>log1p</c> of any
        /// name, and one implementation for both targets is what keeps them from
        /// disagreeing in the last place.
        /// </para>
        /// </remarks>
        private static double Log1P(double value)
        {
            double shifted = 1.0 + value;

            // S1244: the test is whether the addition rounded `value` away
            // completely, which is a question about this exact double and not about
            // two computed quantities being close. It is also what guards the
            // division by `shifted - 1.0` on the next line.
#pragma warning disable S1244
            return shifted == 1.0
#pragma warning restore S1244
                ? value
                : Math.Log(shifted) * value / (shifted - 1.0);
        }
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
