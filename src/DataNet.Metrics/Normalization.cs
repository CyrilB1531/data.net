namespace DataNet.Metrics;

/// <summary>
/// How <see cref="ConfusionMatrix.ToArray(Normalization)"/> scales the cells —
/// the equivalent of <c>normalize=</c> on
/// <c>sklearn.metrics.confusion_matrix</c>.
/// </summary>
/// <remarks>
/// This is a projection, not a state: a <see cref="ConfusionMatrix"/> is never
/// normalized and never remembers having been. That is deliberate. Several
/// metrics in this package read a matrix — <see cref="Accuracy.Score(ConfusionMatrix, bool)"/>
/// computes the diagonal over the total weight — and handing any of them
/// fractions would return a number that is neither accuracy nor anything else,
/// with no error to notice. scikit-learn can afford <c>normalize=</c> on the
/// matrix itself because its metrics take labels rather than matrices.
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
