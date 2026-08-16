# MeanAbsoluteError.Score

The mean of the absolute residuals.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default, ReadOnlySpan<double> outputWeights = default)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major when there is
more
than one output. `outputCount` is how many outputs each row holds. `sampleWeight` weights the rows
—
one weight per sample, not per value. `outputWeights` weights the outputs in the reduction; omit
it
for a plain mean.

**Returns** — `double`, never negative, `0` only for an exact prediction. In the target's own
units.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty,
or
it holds a non-finite value; `ArgumentOutOfRangeException` when `outputCount` is below one.

**Example** — four predictions, out by 0.5, 0.5, 0 and 1.

```csharp
using Lodestar.Metrics;

double[] yTrue = [3.0, -0.5, 2.0, 7.0];
double[] yPred = [2.5, 0.0, 2.0, 8.0];

double error = MeanAbsoluteError.Score(yTrue, yPred);   // => 0.5
```

**Remarks** — start here. It is the only error on this page that reads directly as a sentence a
non-specialist understands — "on average we are half a unit out" — and it charges every unit of
error the same, which is what most costs actually do.

Its defining property is what it does **not** do: nothing is squared, so one prediction that is
ten
times worse counts ten times, not a hundred times. That is the whole choice between this and
`MeanSquaredError.Score`. On `[1, 2, 3, 100]` against `[1, 2, 3, 4]` this reports `24` and mean
squared error reports `2304`, and neither is wrong — they answer "how far out on average" and "how
badly does the worst case hurt".

Two things worth knowing. It is not differentiable at zero, which is why models are so often
trained
on squared error and then reported with this one; that mismatch is normal and not a mistake. And
the
accumulation is Neumaier-compensated, so the answer is at least as accurate as numpy's pairwise
reduction rather than merely close to it —
[decision 0033](../../../decisions/0033-compensated-sum-is-neumaiers-variant.md).

The trap is comparing it across targets. `0.5` is excellent on a target that ranges over thousands
and hopeless on one that ranges over one; it carries units, so it cannot rank two different
problems.
`R2.Score` is what does that.

**Applies to** — net10.0, netstandard2.0.

**See also** — `MeanAbsoluteError.PerOutput`, `MeanSquaredError.Score`,
`MedianAbsoluteError.Score`,
`R2.Score`, the [Python equivalence table](../../../equivalence.md).
