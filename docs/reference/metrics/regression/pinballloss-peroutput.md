# PinballLoss.PerOutput

One pinball loss per output, unreduced.

<!-- docs-declaration -->

```csharp
public static double[] PerOutput(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, double alpha = 0.5, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major. `alpha` is the
quantile being scored, in `[0, 1]`. `outputCount` is how many outputs each row holds, and
`sampleWeight` weights the rows.

**Returns** — a fresh `double[]` of `outputCount` entries, in column order.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty,
or
it holds a non-finite value; `ArgumentOutOfRangeException` when `outputCount` is below one, or
`alpha` is outside `[0, 1]` — including `NaN`.

**Example** — three samples, two outputs, at the median.

```csharp
using DataNet.Metrics;

double[] yTrue = [0.5, 1.0, -1.0, 1.0, 7.0, -6.0];
double[] yPred = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];

double[] perOutput = PinballLoss.PerOutput(yTrue, yPred, alpha: 0.5, outputCount: 2);
double first = perOutput[0];    // => 0.25
double second = perOutput[1];   // => 0.5
```

**Remarks** — the array is what you want when a model emits several quantiles of the same target
and
you laid them out as outputs. It is also the one place the shared multioutput shape is slightly
awkward: `alpha` is a single value applied to **every** output, so scoring a 10th, a 50th and a
90th
percentile means three calls rather than one.

The trap is exactly that. A single call with three quantile columns and `alpha: 0.5` will return
three numbers, none of which is wrong arithmetic and only one of which means anything.

**Applies to** — net10.0, netstandard2.0.

**See also** — `PinballLoss.Score`, `MeanAbsoluteError.PerOutput`,
the [Python equivalence table](../../../equivalence.md).
