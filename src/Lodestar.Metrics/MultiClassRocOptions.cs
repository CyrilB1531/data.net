namespace Lodestar.Metrics;

/// <summary>
/// The optional settings of <see cref="RocAuc.MultiClass"/> — <c>roc_auc_score</c>'s
/// <c>multi_class</c>, <c>average</c>, <c>labels</c> and <c>sample_weight</c> —
/// plus the parallelism scikit-learn has no equivalent for.
/// </summary>
/// <remarks>
/// A <c>ref struct</c>, because <see cref="Labels"/> and
/// <see cref="SampleWeight"/> are spans: build it at the call site. <c>default</c>
/// reproduces scikit-learn's own defaults. Both argued in <c>docs/decisions/0018</c>.
/// </remarks>
public readonly ref struct MultiClassRocOptions
{
    /// <summary>
    /// One-vs-rest or one-vs-one (<c>multi_class=</c>). Defaults to
    /// <see cref="MultiClassStrategy.OneVsRest"/>.
    /// </summary>
    public MultiClassStrategy Strategy { get; init; }

    /// <summary>
    /// <see cref="Averaging.Macro"/> or <see cref="Averaging.Weighted"/>
    /// (<c>average=</c>). <see langword="null"/> — the default — means
    /// <see cref="Averaging.Macro"/>, and is nullable for a reason:
    /// <c>default(Averaging)</c> is <see cref="Averaging.Binary"/>, which
    /// multiclass ROC-AUC refuses, so a non-nullable property would make
    /// <c>default</c> of this type throw instead of meaning the default.
    /// </summary>
    public Averaging? Average { get; init; }

    /// <summary>
    /// The classes the score columns stand for, sorted ascending and unique
    /// (<c>labels=</c>). Empty — the default — reads the sorted distinct labels of
    /// <c>yTrue</c>. Pass it when a class is absent from <c>yTrue</c>.
    /// </summary>
    public ReadOnlySpan<int> Labels { get; init; }

    /// <summary>
    /// A weight per sample (<c>sample_weight=</c>). Empty — the default — weights
    /// every sample by 1. Refused with <see cref="MultiClassStrategy.OneVsOne"/>,
    /// which scikit-learn also refuses.
    /// </summary>
    public ReadOnlySpan<double> SampleWeight { get; init; }

    /// <summary>
    /// How many workers run the per-class loop (one-vs-rest) or the per-pair loop
    /// (one-vs-one). 0 and 1 — the default — are sequential: the sequential path
    /// reads the caller's spans directly and takes no private copy of them.
    /// </summary>
    /// <remarks>
    /// Bit-identical at any setting; above 1 the inputs are copied. No sentinel
    /// for "all cores": write <see cref="Environment.ProcessorCount"/>. Argued
    /// in <c>docs/decisions/0018-multiclass-roc-auc-parallelism-is-opt-in.md</c>.
    /// </remarks>
    public int MaxDegreeOfParallelism { get; init; }
}
