# MeanAbsolutePercentageError.PerOutput

One mean absolute percentage error per output, unreduced.

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

**Example** — the same four numbers read as two samples of two outputs.

```csharp
using Lodestar.Metrics;

double[] yTrue = [100.0, 50.0, 200.0, 25.0];
double[] yPred = [110.0, 45.0, 180.0, 30.0];

double[] perOutput = MeanAbsolutePercentageError.PerOutput(yTrue, yPred, outputCount: 2);
double first = perOutput[0];    // => 0.1
double second = perOutput[1];   // => 0.15…
```

**Remarks** — because this metric is already scale-free, its per-output array is one of the few
here
whose entries are directly comparable with one another: two outputs measured in different units
still
produce two percentages. That makes `Score`'s plain mean of them meaningful in a way that
`MeanAbsoluteError.Score`'s is not.

The trap is that "scale-free" is a claim about the units, not about the data. An output whose
truth
sits near zero for some samples still explodes, and its entry will then dominate the mean over
outputs exactly as it would dominate a mean over samples.

**Applies to** — net10.0, netstandard2.0.

**See also** — `MeanAbsolutePercentageError.Score`, `MeanAbsoluteError.PerOutput`,
the [Python equivalence table](../../../equivalence.md).
