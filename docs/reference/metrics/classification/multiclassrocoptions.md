# MultiClassRocOptions

Everything optional about `RocAuc.MultiClass`, in one `ref struct` so the spans can travel with
the
rest.

<!-- docs-declaration -->

```csharp
public readonly ref struct MultiClassRocOptions
```

**Properties** — `Strategy` is one-vs-rest or one-vs-one, `MultiClassStrategy.OneVsRest` by
default.
`Average` is `Averaging.Macro` or `Averaging.Weighted`, and is nullable so that `default` can mean
macro: `default(Averaging)` is `Averaging.Binary`, which multiclass ROC-AUC refuses. `Labels`
names
the classes the score columns stand for, sorted ascending and unique; empty reads them off
`yTrue`,
which is wrong when a class is absent from it. `SampleWeight` weights the samples and is refused
with one-vs-one, as scikit-learn refuses it. `MaxDegreeOfParallelism` is how many workers run the
per-class or per-pair loop; `0` and `1` are sequential, and there is no sentinel for "all cores" —
write `Environment.ProcessorCount`.

**Example** — the same scores under both strategies and both averages.

```csharp
using Lodestar.Metrics;

int[] yTrue = [0, 1, 2, 2, 2, 1];
double[] yScore =
[
    0.6, 0.3, 0.1,
    0.3, 0.5, 0.2,
    0.2, 0.5, 0.3,
    0.1, 0.2, 0.7,
    0.4, 0.4, 0.2,
    0.2, 0.3, 0.5,
];

MultiClassRocOptions weightedOptions = new() { Average = Averaging.Weighted };
MultiClassRocOptions pairwise = new() { Strategy = MultiClassStrategy.OneVsOne };

double macro = RocAuc.MultiClass(yTrue, yScore, 3);   // => 0.7824…
double weighted = RocAuc.MultiClass(yTrue, yScore, 3, weightedOptions);   // => 0.7361…
double pairs = RocAuc.MultiClass(yTrue, yScore, 3, pairwise);   // => 0.8194…
```

**Remarks** — `default` reproduces scikit-learn's own defaults, so the three-argument call is the
one to write until you need something else. Being a `ref struct` is what lets `Labels` and
`SampleWeight` be spans rather than arrays: build it at the call site, and do not try to store it
in
a field.

`MaxDegreeOfParallelism` is the one setting with no Python counterpart, and it is opt-in rather
than
automatic on purpose — the result is bit-identical at any setting, and above `1` the inputs are
copied, so it is a trade a caller should make knowingly.
[Decision 0018](../../../decisions/0018-multiclass-roc-auc-parallelism-is-opt-in.md) has the
argument.

The trap is `Labels`. Leaving it empty means the score columns are matched to the **sorted
distinct
labels of `yTrue`**, so if your model has five classes and only four of them occur in this
evaluation set, the columns silently shift by one and the number that comes back is meaningless
rather than wrong-looking. Pass `Labels` whenever the label set is fixed by the model rather than
by
the data.

**Applies to** — net10.0, netstandard2.0.

**See also** — `RocAuc.MultiClass`, `MultiClassStrategy`, `Averaging`,
[decision 0018](../../../decisions/0018-multiclass-roc-auc-parallelism-is-opt-in.md),
the [Python equivalence table](../../../equivalence.md).

## Members

| Member | What it does |
| --- | --- |
