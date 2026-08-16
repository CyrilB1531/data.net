# RocAuc.Score

Area under the ROC curve for two classes.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int posLabel = 1, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` holds the true labels, and exactly two distinct values must occur.
`yScore`
holds one score per sample: the higher, the more the model believes `posLabel`. `posLabel` is the
label counted as positive, `1` by default, which is what scikit-learn infers for 0/1 labels.
`sampleWeight` weights the samples.

**Returns** — `double` in `[0, 1]`: `1` when every positive outranks every negative, `0.5` for a
random ranking, and below `0.5` when the ranking is inverted.

**Exceptions** — `ArgumentException` when the spans disagree in length, are empty, contain a `NaN`
score, or only one class occurs.

**Example** — four samples and the model's confidence in each.

```csharp
using Lodestar.Metrics;

int[] yTrue = [0, 0, 1, 1];
double[] yScore = [0.1, 0.4, 0.35, 0.8];

double auc = RocAuc.Score(yTrue, yScore);   // => 0.75
```

**Remarks** — everything else on this page scores a **decision**; this scores a **ranking**. That
is
the reason to pick it: it needs no threshold, so it measures the model rather than the cut-off
someone chose afterwards, and a model whose scores are well ordered but badly calibrated still
scores well. The number has a direct reading — take one positive and one negative at random, and
this is the probability the positive got the higher score.

`0.5` is the floor that matters, not `0`. A score below `0.5` does not mean a bad model so much as
a
sign flip: `1 - auc` is what you would get by ranking the other way.

Two traps. This is **insensitive to class imbalance in a way that can flatter a model**: with 1%
positives, a model can score `0.95` here and still have a precision near zero at every useful
threshold, because the negatives it ranks above the positives are so numerous. If you are choosing
an operating point rather than comparing models, look at precision and recall at the threshold you
will actually use. And `posLabel` is explicit here where scikit-learn infers it; the default of
`1`
is what it infers for 0/1 labels, so labels like `[-1, 1]` or `[1, 2]` need it said.

**Applies to** — net10.0, netstandard2.0.

**See also** — `RocAuc.MultiClass`, `Precision.Score`, `Recall.Score`,
the [Python equivalence table](../../../equivalence.md).
