namespace Lodestar.Metrics.Internal;

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
    /// The regression counterpart: two of the three checks above — the length
    /// agreement and the emptiness — plus the two classification never needed,
    /// a non-finite value and a sample weight that is zero throughout.
    /// </summary>
    /// <param name="yTrue">The true values.</param>
    /// <param name="yPred">The predicted values, expected to be the same length as <paramref name="yTrue"/>.</param>
    /// <param name="sampleWeight">A weight per sample, or empty when every sample is weighted 1.</param>
    /// <remarks>
    /// The length check on <paramref name="sampleWeight"/> lives in
    /// <see cref="Outputs.Validate"/> instead, once <c>outputCount</c> gives the
    /// sample count. The finiteness scan matches scikit-learn's <c>check_array</c>.
    /// </remarks>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, hold a non-finite value, or the sample weight is zero throughout.</exception>
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
            RequireAnyNonZero(sampleWeight);
        }
    }

    /// <summary>
    /// Reproduces <c>_check_sample_weight</c>'s refusal of a weight that is zero
    /// throughout, with its message.
    /// </summary>
    /// <remarks>
    /// The test is <c>_check_sample_weight</c>'s own — <c>all(sample_weight == 0)</c>
    /// at <c>sklearn/utils/validation.py:2198</c> — not "the weights sum to zero":
    /// it accepts <c>[1, -1, 0]</c>, which numpy refuses one layer later instead.
    /// <see cref="Outputs.Validate"/>'s <c>RequireNormalizable</c> is that other rule.
    /// </remarks>
    private static void RequireAnyNonZero(ReadOnlySpan<double> sampleWeight)
    {
        foreach (double weight in sampleWeight)
        {
            // S1244: this is a comparison against the exact zero a caller wrote,
            // not against a computed quantity, and scikit-learn's own test is
            // the same exact `sample_weight == 0`. A tolerance would refuse a
            // legitimately tiny weight — 1e-320 is one scikit-learn accepts.
#pragma warning disable S1244
            if (weight != 0.0)
#pragma warning restore S1244
            {
                return;
            }
        }

        throw new ArgumentException(
            "Sample weights must contain at least one non-zero number.", nameof(sampleWeight));
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
