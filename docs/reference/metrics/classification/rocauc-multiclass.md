# RocAuc.MultiClass

Area under the ROC curve for more than two classes, by reducing to binary problems.

<!-- docs-declaration -->

```csharp
public static double MultiClass(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int classCount, MultiClassRocOptions options = default)
```

**Parameters** — `yTrue` holds one true label per sample. `yScore` holds the class probabilities
row-major — sample 0's classes, then sample 1's — so its length is `classCount` times the sample
count, and each row must sum to 1. `classCount` is how many classes each row scores. `options`
carries the strategy, the averaging, the label set, the sample weights and the worker count;
`default` is scikit-learn's own defaults, on one thread.

**Returns** — `double` in `[0, 1]`, larger meaning a better ranking.

**Exceptions** — `ArgumentException` when any of the shape rules is broken — a length that does
not
match, a row that does not sum to 1, a `NaN`, a sample weight under one-vs-one;
`ArgumentOutOfRangeException` when `classCount` is below two or
`MultiClassRocOptions.MaxDegreeOfParallelism` is negative.

**Example** — six samples over three classes, one probability row each.

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

double auc = RocAuc.MultiClass(yTrue, yScore, 3);   // => 0.7824…
```

**Remarks** — a separate method rather than an overload of `RocAuc.Score`, because the two
parameter
lists would be indistinguishable to the C# compiler and a call like `Score(y, s, 3)` would stop
compiling in consumer code. Everything optional lives in `MultiClassRocOptions`.

Three traps, and the first two are about the shape of `yScore`. It is **probabilities, not
scores**:
each row has to sum to 1, and the call refuses it otherwise, so a raw logit or a decision-function
output has to go through a softmax first. And it is **row-major** — one sample's classes are
contiguous — which is the transpose of what you get from a column-per-class table; there is no
two-dimensional overload because a span cannot carry one.

The third is the class-to-column mapping. With `Labels` left empty the columns are matched to the
sorted distinct labels of `yTrue`, so a class the model knows about but this evaluation set
happens
not to contain will shift every later column. Pass `MultiClassRocOptions.Labels` whenever the
label
set comes from the model rather than from the data.

The exception behaviour is worth one line for anyone raising `MaxDegreeOfParallelism`: the
parallel
path rethrows the original exception instance — same type, message and `ParamName`, from the
lowest-numbered class or pair that failed — so a `catch` written against the sequential path keeps
working, and no `AggregateException` ever escapes.

**Applies to** — net10.0, netstandard2.0.

**See also** — `RocAuc.Score`, `MultiClassRocOptions`, `MultiClassStrategy`,
[decision 0018](../../../decisions/0018-multiclass-roc-auc-parallelism-is-opt-in.md),
the [Python equivalence table](../../../equivalence.md).
