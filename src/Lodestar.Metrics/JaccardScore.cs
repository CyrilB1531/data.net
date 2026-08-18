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
    /// <summary>Reads a matrix you already have — <c>jaccard_score(…, average=…)</c>.</summary>
    /// <param name="cm">The matrix to read.</param>
    /// <param name="average">How per-class scores are reduced.</param>
    /// <param name="posLabel">The class reported under <see cref="Averaging.Binary"/>.</param>
    /// <param name="zeroDivision">What to return when neither the truth nor the prediction holds the class.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cm"/> is null.</exception>
    /// <exception cref="UndefinedMetricException">A class is empty on both sides and <paramref name="zeroDivision"/> is <see cref="ZeroDivision.Throw"/>.</exception>
    public static double Score(
        ConfusionMatrix cm,
        Averaging average = Averaging.Binary,
        int posLabel = 1,
        ZeroDivision zeroDivision = ZeroDivision.Zero)
    {
        Guard.NotNull(cm);
        return Prf.Aggregate(cm, PrfMetric.Jaccard, 1.0, average, posLabel, zeroDivision);
    }

    /// <summary>The coefficient straight from the labels, counting the matrix on the way.</summary>
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
        Score(ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight), average, posLabel, zeroDivision);

    /// <summary>The coefficient for every class, in label order — <c>jaccard_score(average=None)</c>.</summary>
    /// <param name="cm">The matrix to read.</param>
    /// <param name="zeroDivision">What to return when neither side holds the class.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cm"/> is null.</exception>
    /// <exception cref="UndefinedMetricException">A class is empty on both sides and <paramref name="zeroDivision"/> is <see cref="ZeroDivision.Throw"/>.</exception>
    public static double[] PerClass(ConfusionMatrix cm, ZeroDivision zeroDivision = ZeroDivision.Zero)
    {
        Guard.NotNull(cm);
        return Prf.PerClass(cm, PrfMetric.Jaccard, 1.0, zeroDivision);
    }

    /// <summary>The same, straight from the labels.</summary>
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
        PerClass(ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight), zeroDivision);
}
