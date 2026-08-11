using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// The largest single residual — the equivalent of
/// <c>sklearn.metrics.max_error</c>.
/// </summary>
public static class MaxError
{
    /// <summary>
    /// The largest absolute residual — <c>max_error(y_true, y_pred)</c>.
    /// </summary>
    /// <param name="yTrue">The true values.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <remarks>
    /// This metric takes **neither** a sample weight nor a multioutput mode, and
    /// the omission is fidelity rather than an oversight: <c>max_error</c>'s own
    /// signature accepts no <c>sample_weight</c>, and it refuses a
    /// two-dimensional target outright with <c>Multioutput not supported in
    /// max_error</c>. A worst case is not an average, so there is nothing for a
    /// weight to scale.
    /// </remarks>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, or hold a non-finite value.</exception>
    public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred)
    {
        Inputs.Validate(yTrue, yPred, default);

        double worst = 0.0;
        for (int i = 0; i < yTrue.Length; i++)
        {
            double residual = Math.Abs(yTrue[i] - yPred[i]);
            if (residual > worst)
            {
                worst = residual;
            }
        }
        return worst;
    }
}
