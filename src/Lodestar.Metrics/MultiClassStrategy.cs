namespace Lodestar.Metrics;

/// <summary>
/// How multiclass ROC-AUC reduces to binary problems — the equivalent of
/// scikit-learn's <c>multi_class=</c> parameter on <c>roc_auc_score</c>.
/// </summary>
public enum MultiClassStrategy
{
    /// <summary>One class against all the others (<c>multi_class="ovr"</c>).</summary>
    OneVsRest,

    /// <summary>Every pair of classes, averaged (<c>multi_class="ovo"</c>, Hand &amp; Till).</summary>
    OneVsOne,
}
