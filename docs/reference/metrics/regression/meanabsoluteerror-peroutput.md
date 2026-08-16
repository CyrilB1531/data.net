# MeanAbsoluteError.PerOutput

One mean absolute error per output, unreduced.

<!-- docs-declaration -->

```csharp
public static double[] PerOutput(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major. `outputCount`
is
how many outputs each row holds, and `sampleWeight` weights the rows.

**Returns** — a fresh `double[]` of `outputCount` entries, in column order.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty,
or
it holds a non-finite value; `ArgumentOutOfRangeException` when `outputCount` is below one.

**Example** — three samples, two outputs, the second predicted twice as badly as the first.

```csharp
using Lodestar.Metrics;

double[] yTrue = [0.5, 1.0, -1.0, 1.0, 7.0, -6.0];
double[] yPred = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];

double[] perOutput = MeanAbsoluteError.PerOutput(yTrue, yPred, outputCount: 2);
double first = perOutput[0];    // => 0.5
double second = perOutput[1];   // => 1
```

**Remarks** — scikit-learn's `multioutput="raw_values"`. Reach for it whenever the outputs are not
interchangeable — a model predicting both a price and a delay has no useful average of the two,
and
`Score`'s plain mean of them is a number in no units at all.

`outputWeights` on `Score` is the middle ground when the outputs *are* commensurable but not
equally
important, and it is applied to exactly this array. There is no separate weighted-array form
because
weighting an array you are not reducing means nothing.

The trap is `outputCount` silently succeeding. It is only checked against the total length, so
passing `2` for data that is really three outputs wide will slice the span into pairs and return
two
numbers computed from the wrong columns.

**Applies to** — net10.0, netstandard2.0.

**See also** — `MeanAbsoluteError.Score`, `MeanSquaredError.PerOutput`,
`MedianAbsoluteError.PerOutput`,
the [Python equivalence table](../../../equivalence.md).
