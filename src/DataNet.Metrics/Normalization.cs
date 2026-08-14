namespace DataNet.Metrics;

/// <summary>
/// How <see cref="ConfusionMatrix.ToArray(Normalization)"/> scales the cells —
/// the equivalent of <c>normalize=</c> on <c>sklearn.metrics.confusion_matrix</c>.
/// </summary>
/// <remarks>
/// A projection, not a state — argued in
/// <c>docs/decisions/0020-normalize-is-a-projection-not-a-parameter.md</c>.
/// </remarks>
public enum Normalization
{
    /// <summary>Raw counts, or weights when the matrix is weighted (<c>normalize=None</c>).</summary>
    None,

    /// <summary>Each row divided by its own sum: recall per true class (<c>normalize="true"</c>).</summary>
    True,

    /// <summary>Each column divided by its own sum: precision per predicted class (<c>normalize="pred"</c>).</summary>
    Pred,

    /// <summary>Every cell divided by the total (<c>normalize="all"</c>).</summary>
    All,
}
