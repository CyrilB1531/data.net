# MeanSquaredLogError.PerOutput

One mean squared log error per output, unreduced.

<!-- docs-declaration -->

```csharp
public static double[] PerOutput(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major, every value
above
`−1`. `outputCount` is how many outputs each row holds, and `sampleWeight` weights the rows.

**Returns** — a fresh `double[]` of `outputCount` entries, in column order.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty,
it
holds a non-finite value, or either array holds a value at or below `−1`;
`ArgumentOutOfRangeException` when `outputCount` is below one.

**Example** — the same four counts read as two samples of two outputs.

```csharp
using DataNet.Metrics;

double[] yTrue = [3.0, 5.0, 2.5, 7.0];
double[] yPred = [2.5, 5.0, 4.0, 8.0];

double[] perOutput = MeanSquaredLogError.PerOutput(yTrue, yPred, outputCount: 2);
double first = perOutput[0];    // => 0.0725…
double second = perOutput[1];   // => 0.0069…
```

**Remarks** — log units are at least the *same* units for every output, so unlike
`MeanSquaredError.PerOutput` these entries can honestly be compared and averaged even when the
targets are counts of different things.

The trap is the `−1` rule applying to the **whole span**, not per output. One negative value
anywhere
in `yTrue` or `yPred` refuses the call, so a multioutput target with one column that can
legitimately
go negative cannot use this at all — split the columns and score them separately.

**Applies to** — net10.0, netstandard2.0.

**See also** — `MeanSquaredLogError.Score`, `RootMeanSquaredLogError.PerOutput`,
the [Python equivalence table](../../../equivalence.md).
