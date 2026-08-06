using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// Precision and recall combined with a tunable balance — the equivalent of
/// <c>sklearn.metrics.fbeta_score</c>. <c>beta &lt; 1</c> favours precision,
/// <c>beta &gt; 1</c> favours recall, <c>beta = 1</c> is <see cref="F1"/>.
/// </summary>
public static class FBeta
{
    /// <summary>F-beta read off an existing matrix (<c>fbeta_score</c>).</summary>
    /// <param name="cm">The matrix to read.</param>
    /// <param name="beta">The weight of recall relative to precision. Must be finite and non-negative; <c>0</c> yields precision.</param>
    /// <param name="average">How per-class scores are reduced.</param>
    /// <param name="posLabel">The class reported under <see cref="Averaging.Binary"/>.</param>
    /// <param name="zeroDivision">What to return when the metric is undefined.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="beta"/> is negative, NaN or infinite.</exception>
    public static double Score(
        ConfusionMatrix cm,
        double beta,
        Averaging average = Averaging.Binary,
        int posLabel = 1,
        ZeroDivision zeroDivision = ZeroDivision.Zero)
    {
        Guard.NotNull(cm);
        Prf.ValidateBeta(beta);
        return Prf.Aggregate(cm, PrfMetric.FScore, beta, average, posLabel, zeroDivision);
    }

    /// <summary>F-beta for every class, in label order (<c>fbeta_score(average=None)</c>).</summary>
    /// <param name="cm">The matrix to read.</param>
    /// <param name="beta">The weight of recall relative to precision.</param>
    /// <param name="zeroDivision">What to return when the metric is undefined.</param>
    public static double[] PerClass(ConfusionMatrix cm, double beta, ZeroDivision zeroDivision = ZeroDivision.Zero)
    {
        Guard.NotNull(cm);
        Prf.ValidateBeta(beta);
        return Prf.PerClass(cm, PrfMetric.FScore, beta, zeroDivision);
    }

    /// <summary>F-beta straight from the labels, counting the matrix on the way.</summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="beta">The weight of recall relative to precision.</param>
    /// <param name="average">How per-class scores are reduced.</param>
    /// <param name="posLabel">The class reported under <see cref="Averaging.Binary"/>.</param>
    /// <param name="zeroDivision">What to return when the metric is undefined.</param>
    /// <param name="labels">The label set and its order.</param>
    /// <param name="sampleWeight">A weight per sample.</param>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        double beta,
        Averaging average = Averaging.Binary,
        int posLabel = 1,
        ZeroDivision zeroDivision = ZeroDivision.Zero,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default) =>
        Score(ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight), beta, average, posLabel, zeroDivision);

    /// <summary>Per-class F-beta straight from the labels.</summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="beta">The weight of recall relative to precision.</param>
    /// <param name="zeroDivision">What to return when the metric is undefined.</param>
    /// <param name="labels">The label set and its order.</param>
    /// <param name="sampleWeight">A weight per sample.</param>
    public static double[] PerClass(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        double beta,
        ZeroDivision zeroDivision = ZeroDivision.Zero,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default) =>
        PerClass(ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight), beta, zeroDivision);
}
