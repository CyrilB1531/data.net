using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// The mean of the absolute residuals scaled by the truth — the equivalent of
/// <c>sklearn.metrics.mean_absolute_percentage_error</c>.
/// </summary>
public static class MeanAbsolutePercentageError
{
    /// <summary>
    /// numpy's machine epsilon, <c>np.finfo(np.float64).eps</c> — the value
    /// scikit-learn clamps the denominator to.
    /// </summary>
    /// <remarks>
    /// This is <em>not</em> <see cref="double.Epsilon"/>, which is the smallest
    /// positive subnormal, about 4.94e-324 — 292 orders of magnitude smaller.
    /// Both would compile and both would "clamp"; only the frozen oracle says
    /// which reproduces scikit-learn. There is no built-in .NET constant for
    /// machine epsilon, which is why this one is written out.
    /// </remarks>
    private const double MachineEpsilon = 2.220446049250313e-16;

    /// <summary>
    /// One number for the whole prediction —
    /// <c>mean_absolute_percentage_error(y_true, y_pred, sample_weight=…, multioutput=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true values, row-major when there is more than one output.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="outputCount">How many outputs each row holds. One, the default, is the ordinary case.</param>
    /// <param name="sampleWeight">A weight per sample — per <em>row</em>, not per value. Omit to weight every sample by 1.</param>
    /// <param name="outputWeights">A weight per output (<c>multioutput=[…]</c>). Omit for <c>multioutput="uniform_average"</c>.</param>
    /// <exception cref="ArgumentException">A length disagrees with the shape, the input is empty, or it holds a non-finite value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below one.</exception>
    public static double Score(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount = 1,
        ReadOnlySpan<double> sampleWeight = default,
        ReadOnlySpan<double> outputWeights = default) =>
        Outputs.Score<ClampedRatio>(yTrue, yPred, outputCount, sampleWeight, outputWeights);

    /// <summary>
    /// One number per output — <c>multioutput="raw_values"</c>.
    /// </summary>
    /// <param name="yTrue">The true values, row-major.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="outputCount">How many outputs each row holds.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <returns>A fresh array of <paramref name="outputCount"/> entries, in column order.</returns>
    /// <exception cref="ArgumentException">A length disagrees with the shape, the input is empty, or it holds a non-finite value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below one.</exception>
    public static double[] PerOutput(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount = 1,
        ReadOnlySpan<double> sampleWeight = default) =>
        Outputs.PerOutput<ClampedRatio>(yTrue, yPred, outputCount, sampleWeight);

    /// <summary>
    /// The absolute residual over the absolute truth, with the denominator
    /// clamped so that a truth of zero returns a finite number rather than
    /// infinity.
    /// </summary>
    private readonly struct ClampedRatio : IResidualKernel
    {
        public double Apply(double truth, double prediction)
        {
            double magnitude = Math.Abs(truth);
            double denominator = magnitude > MachineEpsilon ? magnitude : MachineEpsilon;
            return Math.Abs(truth - prediction) / denominator;
        }
    }
}
