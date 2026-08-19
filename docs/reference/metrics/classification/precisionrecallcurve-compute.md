# PrecisionRecallCurve.Compute

Draws the precision-recall curve — `sklearn.metrics.precision_recall_curve`.

<!-- docs-declaration -->

```csharp
public static PrecisionRecallCurve Compute(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int posLabel = 1, ReadOnlySpan<double> sampleWeight = default, bool dropIntermediate = false)
```

**Parameters** — `yTrue` is the true labels, one per sample. `yScore` is a score per sample.
`posLabel` is the label counted as positive, `1` by default. `sampleWeight` is one weight per sample,
or empty. `dropIntermediate` drops points whose true-positive count matches both neighbours; `false`
here, as the reference has it.

**Returns** — a `PrecisionRecallCurve` whose `Precision` and `Recall` are the same length and whose
`Thresholds` is **one shorter**, for the reason [the type page](precisionrecallcurve.md) gives.

**Exceptions** — `ArgumentException` when the inputs disagree in length, are empty, or hold a `NaN`
score.

**Example** — the asymmetry, in one line.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 0, 1, 1];
double[] scores = [0.1, 0.4, 0.35, 0.8];

PrecisionRecallCurve curve = PrecisionRecallCurve.Compute(truth, scores);
int missing = curve.Precision.Count - curve.Thresholds.Count;  // => 1
```

**Remarks** — with no positive sample the recall is taken as `1` at every threshold, which is what
the reference warns about and returns; [`AveragePrecision.Score`](../ranking/averageprecision-score.md)
reproduces the same substitution and its page says so.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`RocCurve.Compute`](roccurve-compute.md), [`DetCurve.Compute`](detcurve-compute.md),
[`AveragePrecision.Score`](../ranking/averageprecision-score.md), the
[Python equivalence table](../../../equivalence.md).
