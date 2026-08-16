namespace Lodestar.Metrics;

/// <summary>
/// How a per-class score is reduced to a single number — the equivalent of
/// scikit-learn's <c>average=</c> parameter on <c>precision_score</c>,
/// <c>recall_score</c>, <c>f1_score</c> and <c>fbeta_score</c>.
/// </summary>
/// <remarks>
/// scikit-learn's <c>average=None</c> has no member here: it changes the return
/// type rather than the value. Call <c>PerClass</c> instead.
/// </remarks>
public enum Averaging
{
    /// <summary>Report the positive class only (<c>average="binary"</c>). Valid for two-class problems.</summary>
    Binary,

    /// <summary>Sum the true positives, false positives and false negatives over all classes, then divide once (<c>average="micro"</c>).</summary>
    Micro,

    /// <summary>Unweighted mean of the per-class scores (<c>average="macro"</c>).</summary>
    Macro,

    /// <summary>Mean of the per-class scores weighted by support (<c>average="weighted"</c>).</summary>
    Weighted,
}
