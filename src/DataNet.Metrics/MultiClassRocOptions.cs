namespace DataNet.Metrics;

/// <summary>
/// The optional settings of <see cref="RocAuc.MultiClass"/> — scikit-learn's
/// <c>multi_class</c>, <c>average</c>, <c>labels</c> and <c>sample_weight</c>
/// arguments to <c>roc_auc_score</c>, plus the parallelism this library adds and
/// scikit-learn has no equivalent for.
/// </summary>
/// <remarks>
/// <para>
/// A <c>ref struct</c> because <see cref="Labels"/> and <see cref="SampleWeight"/>
/// are spans, which nothing else can hold as a field; any other shape would turn
/// them into arrays and impose an allocation on every caller. Build it at the call
/// site — it cannot be stored in a field, captured by a lambda, or held across an
/// <c>await</c>.
/// </para>
/// <para>
/// <c>default</c> reproduces scikit-learn's own defaults: one-vs-rest, macro
/// averaging, labels read from <c>yTrue</c>, no sample weights, one thread.
/// </para>
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
    /// (one-vs-one). 0 and 1 — the default — are sequential, and the sequential
    /// path is unchanged: it reads the caller's spans in place and copies nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The result is bit-identical whatever this is set to: every class and every
    /// pair writes its own slot, and the averaging runs afterwards on the calling
    /// thread in array order.
    /// </para>
    /// <para>
    /// Above 1, the inputs are copied. A span cannot be handed to another thread,
    /// so the parallel path rents a copy of <c>yTrue</c>, of the sample weights if
    /// any, and a transposed copy of the score matrix — about
    /// <c>samples × classes × 8</c> bytes, returned to the pool on the way out.
    /// That is the price of the opt-in, which is why the default does not pay it.
    /// </para>
    /// <para>
    /// The setting is honoured as given, at any input size, and there is no
    /// sentinel for "all cores": write <see cref="Environment.ProcessorCount"/> if
    /// that is what is meant, so the number is visible at the call site.
    /// scikit-learn does not parallelise <c>roc_auc_score</c> at all — see
    /// <c>docs/decisions/0017-multiclass-roc-auc-parallelism-is-opt-in.md</c>.
    /// </para>
    /// </remarks>
    public int MaxDegreeOfParallelism { get; init; }
}
