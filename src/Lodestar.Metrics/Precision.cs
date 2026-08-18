using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// Of everything predicted into a class, how much belonged there — the
/// equivalent of <c>sklearn.metrics.precision_score</c>.
/// </summary>
public static class Precision
{
    /// <summary>Precision read off an existing matrix (<c>precision_score</c>).</summary>
    /// <param name="cm">The matrix to read.</param>
    /// <param name="average">How per-class scores are reduced. <c>Binary</c>, the default, matches scikit-learn.</param>
    /// <param name="posLabel">The class reported under <see cref="Averaging.Binary"/>.</param>
    /// <param name="zeroDivision">What to return when nothing was predicted into a class.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cm"/> is null.</exception>
    /// <exception cref="ArgumentException"><see cref="Averaging.Binary"/> on a target with more than two classes, or a <paramref name="posLabel"/> that does not occur.</exception>
    /// <exception cref="UndefinedMetricException"><paramref name="zeroDivision"/> is <see cref="ZeroDivision.Throw"/> and the metric is undefined.</exception>
    public static double Score(
        ConfusionMatrix cm,
        Averaging average = Averaging.Binary,
        int posLabel = 1,
        ZeroDivision zeroDivision = ZeroDivision.Zero)
    {
        Guard.NotNull(cm);
        return Prf.Aggregate(cm, PrfMetric.Precision, 1.0, average, posLabel, zeroDivision);
    }

    /// <summary>Precision straight from the labels, counting the matrix on the way.</summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="average">How per-class scores are reduced.</param>
    /// <param name="posLabel">The class reported under <see cref="Averaging.Binary"/>.</param>
    /// <param name="zeroDivision">What to return when nothing was predicted into a class.</param>
    /// <param name="labels">The label set and its order. Omit for the sorted union of both inputs.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        Averaging average = Averaging.Binary,
        int posLabel = 1,
        ZeroDivision zeroDivision = ZeroDivision.Zero,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default) =>
        Score(ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight), average, posLabel, zeroDivision);

    /// <summary>Precision for every class, in label order (<c>precision_score(average=None)</c>).</summary>
    /// <param name="cm">The matrix to read.</param>
    /// <param name="zeroDivision">What to return when nothing was predicted into a class.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cm"/> is null.</exception>
    public static double[] PerClass(ConfusionMatrix cm, ZeroDivision zeroDivision = ZeroDivision.Zero)
    {
        Guard.NotNull(cm);
        return Prf.PerClass(cm, PrfMetric.Precision, 1.0, zeroDivision);
    }

    /// <summary>Per-class precision straight from the labels.</summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="zeroDivision">What to return when nothing was predicted into a class.</param>
    /// <param name="labels">The label set and its order. Omit for the sorted union of both inputs.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <exception cref="ArgumentException">The label spans disagree in length or are empty.</exception>
    public static double[] PerClass(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        ZeroDivision zeroDivision = ZeroDivision.Zero,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default) =>
        PerClass(ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight), zeroDivision);
}
