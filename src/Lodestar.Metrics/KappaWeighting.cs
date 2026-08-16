namespace Lodestar.Metrics;

/// <summary>
/// How far apart two classes count as being, for Cohen's kappa — the equivalent
/// of <c>weights=</c> on <c>sklearn.metrics.cohen_kappa_score</c>.
/// </summary>
/// <remarks>
/// Named <c>weighting</c>, not scikit-learn's <c>weights</c>: <c>sampleWeight</c>
/// sits in the same signature and the two are unrelated senses of the word. The
/// distance is between class <em>positions</em>, so the weighted forms depend on
/// label order; the unweighted form does not.
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
