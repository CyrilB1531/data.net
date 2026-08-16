# RootMeanSquaredLogError.PerOutput

One root mean squared log error per output, unreduced.

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
using Lodestar.Metrics;

double[] yTrue = [3.0, 5.0, 2.5, 7.0];
double[] yPred = [2.5, 5.0, 4.0, 8.0];

double[] perOutput = RootMeanSquaredLogError.PerOutput(yTrue, yPred, outputCount: 2);
double first = perOutput[0];    // => 0.2693…
double second = perOutput[1];   // => 0.0832…
```

**Remarks** — the entries are the square roots of `MeanSquaredLogError.PerOutput`'s, and they are
the
array `Score` reduces. Every entry is in log units, so unlike `RootMeanSquaredError.PerOutput`
these
really are comparable across outputs even when the targets count different things — which is one
of
the better reasons to model counts in log space in the first place.

The trap is inherited whole from the squared form: one value at or below `−1` **anywhere** refuses
the call, per span and not per output, so a column that can legitimately go negative has to be
scored
separately.

**Applies to** — net10.0, netstandard2.0.

**See also** — `RootMeanSquaredLogError.Score`, `MeanSquaredLogError.PerOutput`,
`RootMeanSquaredError.PerOutput`, the [Python equivalence table](../../../equivalence.md).
