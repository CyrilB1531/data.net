# MedianAbsoluteError.PerOutput

One median absolute error per output, unreduced.

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

**Example** — three samples, two outputs; each column's median is taken separately.

```csharp
using DataNet.Metrics;

double[] yTrue = [0.5, 1.0, -1.0, 1.0, 7.0, -6.0];
double[] yPred = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];

double[] perOutput = MedianAbsoluteError.PerOutput(yTrue, yPred, outputCount: 2);
double first = perOutput[0];    // => 0.5
double second = perOutput[1];   // => 1
```

**Remarks** — the median is taken **per column**, which is the only definition that makes sense
and
is worth stating because it is not what "the median of a matrix" would mean. Each output is sorted
on
its own and its own middle value taken.

The trap is that a median does not decompose the way a mean does. The mean of the per-output
medians
that `Score` returns is not the median of anything, so it is a summary of summaries rather than a
statistic of the data. On multioutput targets, read the array.

Internally each column is selected rather than fully sorted, which is what keeps this from costing
an
`n log n` per output —
[decision 0025](../../../decisions/0025-quickselect-replaces-a-full-sort-for-the-median.md).
Nothing
about the answer depends on it.

**Applies to** — net10.0, netstandard2.0.

**See also** — `MedianAbsoluteError.Score`, `MeanAbsoluteError.PerOutput`,
[decision 0025](../../../decisions/0025-quickselect-replaces-a-full-sort-for-the-median.md),
the [Python equivalence table](../../../equivalence.md).
