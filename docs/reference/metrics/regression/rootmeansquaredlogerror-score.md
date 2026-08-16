# RootMeanSquaredLogError.Score

The square root of the mean squared log error, taken per output before the outputs are reduced.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default, ReadOnlySpan<double> outputWeights = default)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major when there is
more
than one output, and every value must be above `−1`. `outputCount` is how many outputs each row
holds, `sampleWeight` weights the rows, and `outputWeights` weights the outputs in the reduction.

**Returns** — `double`, never negative, `0` only for an exact prediction. In log units.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty,
it
holds a non-finite value, or either array holds a value at or below `−1`;
`ArgumentOutOfRangeException` when `outputCount` is below one.

**Example** — the same four counts `MeanSquaredLogError.Score` scores `0.0397…`.

```csharp
using Lodestar.Metrics;

double[] yTrue = [3.0, 5.0, 2.5, 7.0];
double[] yPred = [2.5, 5.0, 4.0, 8.0];

double error = RootMeanSquaredLogError.Score(yTrue, yPred);   // => 0.1993…
```

**Remarks** — this is the one of the log pair to report, because a root log error has an
approximate
reading a squared one does not: for small values, `0.1993…` is roughly "typically out by about
20%".
That approximation breaks down as the number grows — it is `exp(x) - 1` that gives the ratio — but
it
is enough to make the metric quotable, which the squared version is not.

A type of its own rather than a flag, for the same reason `RootMeanSquaredError` is: scikit-learn
exposes it as its own function rather than as a `squared` parameter.

The trap is the same order-of-operations one: the root is taken **per output** before the
reduction,
so on more than one output this is not the root of `MeanSquaredLogError.Score`. And the asymmetry
the
logarithm introduces survives the root — under-prediction is still charged more than
over-prediction
— so this is not a symmetric relative error however much the "about 20%" reading makes it sound
like
one.

**Applies to** — net10.0, netstandard2.0.

**See also** — `RootMeanSquaredLogError.PerOutput`, `MeanSquaredLogError.Score`,
`MeanAbsolutePercentageError.Score`,
[decision 0028](../../../decisions/0028-log1p-is-kahans-identity-not-math-log-1-plus-x.md),
the [Python equivalence table](../../../equivalence.md).
