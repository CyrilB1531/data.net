using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The Jaccard similarity coefficient — the equivalent of
/// <c>sklearn.metrics.jaccard_score</c>.
/// </summary>
/// <remarks>
/// Intersection over union: of every sample that is in the true class or was predicted
/// into it, what share is in both. <see cref="Precision"/>'s numerator over a larger
/// denominator, which is why it reads below both precision and recall whenever they
/// disagree.
/// </remarks>
public static class JaccardScore
{
    /// <summary>The coefficient, counting the matrix on the way.</summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="average">How per-class scores are reduced.</param>
    /// <param name="posLabel">The class reported under <see cref="Averaging.Binary"/>.</param>
    /// <param name="zeroDivision">What to return when neither side holds the class.</param>
    /// <param name="labels">The label set and its order. Omit for the sorted union of both inputs.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <exception cref="ArgumentException">The inputs disagree in length, or the weights do not match.</exception>
    /// <exception cref="UndefinedMetricException">A class is empty on both sides and <paramref name="zeroDivision"/> is <see cref="ZeroDivision.Throw"/>.</exception>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        Averaging average = Averaging.Binary,
        int posLabel = 1,
        ZeroDivision zeroDivision = ZeroDivision.Zero,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default) =>
        Prf.Aggregate(
            ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight),
            PrfMetric.Jaccard, 1.0, average, posLabel, zeroDivision);

    /// <summary>The coefficient for every class, in label order — <c>jaccard_score(average=None)</c>.</summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="zeroDivision">What to return when neither side holds the class.</param>
    /// <param name="labels">The label set and its order. Omit for the sorted union of both inputs.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <exception cref="ArgumentException">The inputs disagree in length, or the weights do not match.</exception>
    /// <exception cref="UndefinedMetricException">A class is empty on both sides and <paramref name="zeroDivision"/> is <see cref="ZeroDivision.Throw"/>.</exception>
    public static double[] PerClass(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        ZeroDivision zeroDivision = ZeroDivision.Zero,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default) =>
        Prf.PerClass(
            ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight), PrfMetric.Jaccard, 1.0, zeroDivision);
}
