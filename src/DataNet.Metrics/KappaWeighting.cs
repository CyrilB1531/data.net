namespace DataNet.Metrics;

/// <summary>
/// How far apart two classes count as being, for Cohen's kappa — the equivalent
/// of <c>weights=</c> on <c>sklearn.metrics.cohen_kappa_score</c>.
/// </summary>
/// <remarks>
/// Named <c>weighting</c> where scikit-learn says <c>weights</c>, because
/// <c>sampleWeight</c> sits in the same signature and the two are unrelated
/// senses of the word. The distance is between class <em>positions</em>, so the
/// weighted forms depend on the order of the labels; the unweighted form does
/// not.
/// </remarks>
public enum KappaWeighting
{
    /// <summary>Every disagreement counts the same (<c>weights=None</c>).</summary>
    None,

    /// <summary>A disagreement counts its distance in positions (<c>weights="linear"</c>).</summary>
    Linear,

    /// <summary>A disagreement counts the square of that distance (<c>weights="quadratic"</c>).</summary>
    Quadratic,
}
