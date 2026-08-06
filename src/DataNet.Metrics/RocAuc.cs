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
}
