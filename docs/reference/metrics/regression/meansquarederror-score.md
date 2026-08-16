# MeanSquaredError.Score

The mean of the squared residuals.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default, ReadOnlySpan<double> outputWeights = default)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major when there is
more
than one output. `outputCount` is how many outputs each row holds, `sampleWeight` weights the
rows,
and `outputWeights` weights the outputs in the reduction.

**Returns** — `double`, never negative, `0` only for an exact prediction. In the **square** of the
target's units.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty,
or
it holds a non-finite value; `ArgumentOutOfRangeException` when `outputCount` is below one.

**Example** — the same four predictions `MeanAbsoluteError.Score` scores `0.5`.

```csharp
using Lodestar.Metrics;

double[] yTrue = [3.0, -0.5, 2.0, 7.0];
double[] yPred = [2.5, 0.0, 2.0, 8.0];

double error = MeanSquaredError.Score(yTrue, yPred);   // => 0.375
```

**Remarks** — squaring is the whole design, and it has two consequences that pull in opposite
directions. It makes the metric differentiable everywhere, which is why almost every regression
model
minimises it during training. And it makes one bad prediction count out of all proportion: on
`[1, 2, 3, 100]` against `[1, 2, 3, 4]` this is `2304` where `MeanAbsoluteError.Score` is `24`. If
your costs really do grow faster than the error does — a bridge, a dosage — that is the right
behaviour. If they do not, it is a metric that will let one bad label decide which model you ship.

The trap is the units. This is in the **square** of the target's units, so a mean squared error of
`0.375` on a target measured in metres is `0.375` square metres, which is not a distance and is
not
comparable to a mean absolute error of `0.5`. `RootMeanSquaredError.Score` puts it back into
metres,
and is what you should report to anyone who is going to read the number rather than optimise it.

The accumulation is Neumaier-compensated, at least as accurate as numpy's pairwise reduction
rather
than merely close to it —
[decision 0033](../../../decisions/0033-compensated-sum-is-neumaiers-variant.md).

**Applies to** — net10.0, netstandard2.0.

**See also** — `MeanSquaredError.PerOutput`, `RootMeanSquaredError.Score`,
`MeanAbsoluteError.Score`,
`R2.Score`, the [Python equivalence table](../../../equivalence.md).
