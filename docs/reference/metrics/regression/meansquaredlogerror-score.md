# MeanSquaredLogError.Score

The mean of the squared differences of `log(1 + y)`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default, ReadOnlySpan<double> outputWeights = default)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major when there is
more
than one output, and **every value must be above −1**. `outputCount` is how many outputs each row
holds, `sampleWeight` weights the rows, and `outputWeights` weights the outputs in the reduction.

**Returns** — `double`, never negative, `0` only for an exact prediction. In squared log units,
which
are not the target's units and not a fraction either.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty,
it
holds a non-finite value, or either array holds a value at or below `−1`;
`ArgumentOutOfRangeException` when `outputCount` is below one.

**Example** — four counts, one of them predicted 60% high.

```csharp
using DataNet.Metrics;

double[] yTrue = [3.0, 5.0, 2.5, 7.0];
double[] yPred = [2.5, 5.0, 4.0, 8.0];

double error = MeanSquaredLogError.Score(yTrue, yPred);   // => 0.0397…
```

**Remarks** — taking the logarithm first turns a *ratio* into a *difference*, so this charges
"predicted twice the truth" the same whether the truth was 10 or 10 000. That makes it the natural
metric for counts, demand, page views — anything that grows multiplicatively and where the small
values are as interesting as the large ones. `MeanSquaredError.Score` on such a target is decided
entirely by the largest few samples.

It is also **asymmetric on purpose**, and this is the reason to choose it over
`MeanAbsolutePercentageError.Score`: because `log` compresses upward, under-predicting is charged
more than over-predicting by the same factor. If running out of stock is worse than holding too
much, that asymmetry is the feature.

Two traps. The units are not interpretable — `0.0397…` is neither a count nor a percentage — so
report `RootMeanSquaredLogError.Score` if a human is going to read it, and even then it is a log
ratio. And **negative targets are refused**, not clamped: any value at or below `−1` raises
`ArgumentException`, because `log(1 + y)` is undefined there. The exception additionally names
which
side the offending value was on, which scikit-learn's does not.

The logarithm is numpy's `log1p`, reached through Kahan's identity rather than `Math.Log(1.0 +
x)`.
That is not decoration: on targets around `1e-9` the naive spelling is out by `1.7e-8` relative,
where this agrees with scikit-learn to a unit in the last place —
[decision 0028](../../../decisions/0028-log1p-is-kahans-identity-not-math-log-1-plus-x.md).

**Applies to** — net10.0, netstandard2.0.

**See also** — `MeanSquaredLogError.PerOutput`, `RootMeanSquaredLogError.Score`,
`MeanAbsolutePercentageError.Score`,
[decision 0028](../../../decisions/0028-log1p-is-kahans-identity-not-math-log-1-plus-x.md),
the [Python equivalence table](../../../equivalence.md).
