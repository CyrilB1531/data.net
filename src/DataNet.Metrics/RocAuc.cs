using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// Area under the receiver-operating-characteristic curve — the equivalent of
/// <c>sklearn.metrics.roc_auc_score</c>.
/// </summary>
/// <remarks>
/// Two entry points rather than scikit-learn's single overloaded function: their
/// parameter lists would be indistinguishable to the C# compiler, and a call
/// like <c>Score(y, s, 3)</c> would fail to compile in consumer code.
/// </remarks>
public static class RocAuc
{
    /// <summary>
    /// The binary case — <c>roc_auc_score(y_true, y_score, sample_weight=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true labels. Exactly two distinct values must occur.</param>
    /// <param name="yScore">A score per sample: the higher, the more the model believes <paramref name="posLabel"/>.</param>
    /// <param name="posLabel">The label counted as positive. scikit-learn infers this; 1 is what it infers for 0/1 labels.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, contain a NaN score, or only one class occurs.</exception>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yScore,
        int posLabel = 1,
        ReadOnlySpan<double> sampleWeight = default) =>
        BinaryRoc.Score(yTrue, yScore, posLabel, sampleWeight);

    /// <summary>
    /// The multiclass case —
    /// <c>roc_auc_score(y_true, y_score, multi_class=…, average=…, labels=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true labels, one per sample.</param>
    /// <param name="yScore">Class probabilities, row-major: sample 0's classes, then sample 1's. Length must be <paramref name="classCount"/> times the sample count, and each row must sum to 1.</param>
    /// <param name="classCount">How many classes each row scores.</param>
    /// <param name="strategy">One-vs-rest or one-vs-one (Hand &amp; Till).</param>
    /// <param name="average">Only <see cref="Averaging.Macro"/> and <see cref="Averaging.Weighted"/>, as scikit-learn allows.</param>
    /// <param name="labels">The classes the columns stand for, sorted ascending and unique. Omit for the sorted distinct labels of <paramref name="yTrue"/>.</param>
    /// <param name="sampleWeight">A weight per sample. Not supported with <see cref="MultiClassStrategy.OneVsOne"/>, which scikit-learn also refuses.</param>
    /// <exception cref="ArgumentException">Any of the rules above is broken.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="classCount"/> is below two.</exception>
    public static double MultiClass(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yScore,
        int classCount,
        MultiClassStrategy strategy = MultiClassStrategy.OneVsRest,
        Averaging average = Averaging.Macro,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default) =>
        MultiClassRoc.Score(yTrue, yScore, classCount, strategy, average, labels, sampleWeight);
}
