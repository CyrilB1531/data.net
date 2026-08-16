# RootMeanSquaredError.PerOutput

One root mean squared error per output, unreduced.

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

**Example** — the roots `Score` then reduces.

```csharp
using Lodestar.Metrics;

double[] yTrue = [0.5, 1.0, -1.0, 1.0, 7.0, -6.0];
double[] yPred = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];

double[] perOutput = RootMeanSquaredError.PerOutput(yTrue, yPred, outputCount: 2);
double first = perOutput[0];    // => 0.6454…
double second = perOutput[1];   // => 1
```

**Remarks** — this is exactly the square root of each entry of `MeanSquaredError.PerOutput`, and
it
is the array `Score` averages. Reading it is how you find out which output the headline number is
being dragged down by, and unlike the squared version its entries are in each output's own units,
so
they can be compared against those outputs' scales.

The trap is that they still cannot be compared against **each other** unless the outputs share a
unit. Two outputs, one in euros and one in days, give two numbers whose ratio means nothing —
which
is what `R2.PerOutput` is for.

**Applies to** — net10.0, netstandard2.0.

**See also** — `RootMeanSquaredError.Score`, `MeanSquaredError.PerOutput`, `R2.PerOutput`,
the [Python equivalence table](../../../equivalence.md).
