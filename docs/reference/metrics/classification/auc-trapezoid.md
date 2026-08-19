# Auc.Trapezoid

The trapezoidal area under a curve given as points — `sklearn.metrics.auc`.

<!-- docs-declaration -->

```csharp
public static double Trapezoid(ReadOnlySpan<double> x, ReadOnlySpan<double> y)
```

**Parameters** — `x` is the x coordinates, monotonic in either direction, and `y` the y coordinates,
one per x.

**Returns** — `double`, the area. A curve given right to left gives the same magnitude as the same
curve given left to right, which is what the reference does too.

**Exceptions** — `ArgumentException` when the lengths disagree, when fewer than two points are
given, or when `x` is neither increasing nor decreasing — a sequence that turns has no single area
under it.

**Example** — integrating a ROC curve this package drew.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 0, 1, 1];
double[] scores = [0.1, 0.4, 0.35, 0.8];
RocCurve curve = RocCurve.Compute(truth, scores);

double area = Auc.Trapezoid([.. curve.FalsePositiveRate], [.. curve.TruePositiveRate]);  // => 0.75
```

**Remarks** — reaching for this over a precision-recall curve is the mistake
[`AveragePrecision`](../ranking/averageprecision.md) exists to avoid; see
[the type page](auc.md).

**Applies to** — net10.0, netstandard2.0.

**See also** — [`RocCurve.Compute`](roccurve-compute.md), [`RocAuc.Score`](rocauc-score.md),
[`AveragePrecision.Score`](../ranking/averageprecision-score.md), the
[Python equivalence table](../../../equivalence.md).
