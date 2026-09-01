# SplitConformal.AbsoluteResiduals

A regressor's calibration scores: how far each prediction missed, without a sign.

<!-- docs-declaration -->

```csharp
public static double[] AbsoluteResiduals(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPredicted)
```

**Parameters** — `yTrue` are the observed values and `yPredicted` the model's predictions for the
same points, in the same order and of the same length.

**Returns** — a fresh `double[]`, one `|y − ŷ|` per point, in the input's order. Hand it to
[`Quantile`](splitconformal-quantile.md).

**Exceptions** — `ArgumentException` when the two spans have different lengths.

**Example** — three calibration points: one missed by 1, one by 0.5, one exactly right.

```csharp
using Lodestar.Conformal;

double[] yTrue = [1.0, 2.0, 3.0];
double[] yPredicted = [2.0, 1.5, 3.0];

double[] residuals = SplitConformal.AbsoluteResiduals(yTrue, yPredicted);
double first = residuals[0];    // => 1
double second = residuals[1];   // => 0.5
double third = residuals[2];    // => 0
```

**Remarks** — this is MAPIE's `AbsoluteConformityScore`, what `SplitConformalRegressor` uses unless
told otherwise. Nothing here is fitted: the calibration set must be data the model did **not**
train on, and this method has no way to check that for you.

The absolute value is what makes the resulting interval symmetric, and that is a real limitation
rather than a simplification. Every prediction gets the same width, so a model whose error grows
with the target — most of them — gets intervals too wide where it is confident and too narrow where
it is not, while still covering at the rate asked for overall. A normalised score fixes that by
dividing the residual by a second model's estimate of the local spread; this package does not ship
one yet, and reaching for a signed residual instead does not help — it produces a one-sided
interval, not an adaptive one.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SplitConformal.Quantile`](splitconformal-quantile.md),
[`SplitConformal.Interval`](splitconformal-interval.md), the
[Python equivalence table](../../../equivalence.md).
