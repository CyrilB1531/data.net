# DetCurve.Compute

Draws the detection error tradeoff curve — `sklearn.metrics.det_curve`.

<!-- docs-declaration -->

```csharp
public static DetCurve Compute(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int posLabel = 1, ReadOnlySpan<double> sampleWeight = default, bool dropIntermediate = false)
```

**Parameters** — `yTrue` is the true labels, one per sample. `yScore` is a score per sample.
`posLabel` is the label counted as positive, `1` by default. `sampleWeight` is one weight per sample,
or empty. `dropIntermediate` drops points whose true-positive count matches both neighbours; `false`
here, as the reference has it.

**Returns** — a `DetCurve` whose `FalsePositiveRate`, `FalseNegativeRate` and `Thresholds` are three
parallel arrays of equal length, ordered by **ascending** threshold.

**Exceptions** — `ArgumentException` when the inputs disagree in length, are empty, or hold a `NaN`
score.

**Example** — the shortest of the three curves on the same four samples.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 0, 1, 1];
double[] scores = [0.1, 0.4, 0.35, 0.8];

DetCurve curve = DetCurve.Compute(truth, scores);
int points = curve.Thresholds.Count;  // => 3
```

**Remarks** — the false-negative rate is one minus the true-positive rate
[`RocCurve`](roccurve.md) reports at the same threshold, so the two curves carry the same information
and differ only in what they make easy to see.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`RocCurve.Compute`](roccurve-compute.md),
[`PrecisionRecallCurve.Compute`](precisionrecallcurve-compute.md), the
[Python equivalence table](../../../equivalence.md).
