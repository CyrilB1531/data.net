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
}
