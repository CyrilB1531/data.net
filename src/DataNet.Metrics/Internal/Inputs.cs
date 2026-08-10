namespace DataNet.Metrics.Internal;

/// <summary>
/// Validates the <c>(yTrue, yPred, sampleWeight)</c> triple every metric in this
/// package accepts, in one place — so the metric types landing in later tasks
/// do not each restate the same three checks.
/// </summary>
internal static class Inputs
{
    /// <summary>
    /// Checks that <paramref name="yTrue"/> and <paramref name="yPred"/> agree in
    /// length and are not empty, and that <paramref name="sampleWeight"/>, when
    /// supplied, agrees in length with them.
    /// </summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, expected to be the same length as <paramref name="yTrue"/>.</param>
    /// <param name="sampleWeight">A weight per sample, or empty when every sample is weighted 1.</param>
    /// <exception cref="ArgumentException">The inputs disagree in length or are empty.</exception>
    public static void Validate(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, ReadOnlySpan<double> sampleWeight)
    {
        if (yTrue.Length != yPred.Length)
        {
            throw new ArgumentException(
                $"yTrue has {yTrue.Length} entries and yPred has {yPred.Length}; they must agree.",
                nameof(yPred));
        }
        if (yTrue.Length == 0)
        {
            throw new ArgumentException("yTrue and yPred are empty; there is nothing to score.", nameof(yTrue));
        }
        if (!sampleWeight.IsEmpty && sampleWeight.Length != yTrue.Length)
        {
            throw new ArgumentException(
                $"sampleWeight has {sampleWeight.Length} entries but there are {yTrue.Length} samples.",
                nameof(sampleWeight));
        }
    }

    /// <summary>
    /// The regression counterpart: the same three checks, plus the one
    /// classification never needed — a non-finite value is refused.
    /// </summary>
    /// <param name="yTrue">The true values.</param>
    /// <param name="yPred">The predicted values, expected to be the same length as <paramref name="yTrue"/>.</param>
    /// <param name="sampleWeight">A weight per sample, or empty when every sample is weighted 1.</param>
    /// <remarks>
    /// The finiteness scan is an extra <c>O(n)</c> pass, and it is what parity
    /// costs: scikit-learn's <c>check_array</c> refuses <c>NaN</c> and infinity
    /// before any metric runs, with two distinct messages, and a caller who gets
    /// a silent <c>NaN</c> back instead has no way to tell it from a genuine one.
    /// </remarks>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, or hold a non-finite value.</exception>
    public static void Validate(
        ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, ReadOnlySpan<double> sampleWeight)
    {
        if (yTrue.Length != yPred.Length)
        {
            throw new ArgumentException(
                $"yTrue has {yTrue.Length} entries and yPred has {yPred.Length}; they must agree.",
                nameof(yPred));
        }
        if (yTrue.Length == 0)
        {
            throw new ArgumentException("yTrue and yPred are empty; there is nothing to score.", nameof(yTrue));
        }

        RequireFinite(yTrue, nameof(yTrue));
        RequireFinite(yPred, nameof(yPred));
        if (!sampleWeight.IsEmpty)
        {
            RequireFinite(sampleWeight, nameof(sampleWeight));
        }
    }

    /// <summary>Reproduces scikit-learn's two <c>check_array</c> messages, which differ.</summary>
    private static void RequireFinite(ReadOnlySpan<double> values, string paramName)
    {
        foreach (double value in values)
        {
            if (double.IsNaN(value))
            {
                throw new ArgumentException("Input contains NaN.", paramName);
            }
            if (double.IsInfinity(value))
            {
                throw new ArgumentException(
                    "Input contains infinity or a value too large for dtype('float64').", paramName);
            }
        }
    }
}
