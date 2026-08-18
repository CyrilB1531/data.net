# RocCurve.Compute

Draws the ROC curve — `sklearn.metrics.roc_curve`.

<!-- docs-declaration -->

```csharp
public static RocCurve Compute(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int posLabel = 1, ReadOnlySpan<double> sampleWeight = default, bool dropIntermediate = true)
```

**Parameters** — `yTrue` is the true labels, one per sample. `yScore` is a score per sample: the
higher, the more the model believes `posLabel`. `posLabel` is the label counted as positive, `1` by
default, where scikit-learn infers it. `sampleWeight` is one weight per sample, or empty.
`dropIntermediate` drops points the curve does not bend at; `true` here, matching the reference's
default for this curve and **not** for the other two.

**Returns** — a `RocCurve` whose `FalsePositiveRate`, `TruePositiveRate` and `Thresholds` are three
parallel arrays of equal length, the first point being the origin at an infinite threshold.

**Exceptions** — `ArgumentException` when the inputs disagree in length, are empty, or hold a `NaN`
score.

**Example** — four samples, and the area under what it draws.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 0, 1, 1];
double[] scores = [0.1, 0.4, 0.35, 0.8];

RocCurve curve = RocCurve.Compute(truth, scores);
int points = curve.Thresholds.Count;  // => 5
```

```csharp
using Lodestar.Metrics;

int[] truth = [0, 0, 1, 1];
double[] scores = [0.1, 0.4, 0.35, 0.8];

RocCurve curve = RocCurve.Compute(truth, scores);
double[] x = [.. curve.FalsePositiveRate];
double[] y = [.. curve.TruePositiveRate];

double area = Auc.Trapezoid(x, y);  // => 0.75
```

That is [`RocAuc.Score`](rocauc-score.md) on the same input, to the last bit — an invariant the test
suite asserts over every fixture of the frozen corpus, because no oracle can state it.

**Remarks** — a class the reference never sees is absent from the denominator, and the rate is `NaN`
rather than a division by zero, which is what scikit-learn warns about and returns.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`PrecisionRecallCurve.Compute`](precisionrecallcurve-compute.md),
[`DetCurve.Compute`](detcurve-compute.md), [`Auc.Trapezoid`](auc-trapezoid.md),
[`RocAuc.Score`](rocauc-score.md), the [Python equivalence table](../../../equivalence.md).
