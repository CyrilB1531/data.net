# MeanSquaredError.PerOutput

One mean squared error per output, unreduced.

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

**Example** — three samples, two outputs.

```csharp
using Lodestar.Metrics;

double[] yTrue = [0.5, 1.0, -1.0, 1.0, 7.0, -6.0];
double[] yPred = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];

double[] perOutput = MeanSquaredError.PerOutput(yTrue, yPred, outputCount: 2);
double first = perOutput[0];    // => 0.4166…
double second = perOutput[1];   // => 1
```

**Remarks** — the per-output array is where a multioutput model is actually diagnosed, because
squared errors in different units cannot be averaged into anything meaningful. Two outputs, one in
euros and one in days, give a `Score` in "euros-squared and days-squared", which is not a
quantity.

The trap follows from that: `outputWeights` on `Score` is often used to fix it, and it does not.
Weighting a euro-squared against a day-squared still leaves a number with no units. If the outputs
are on different scales, the fix is to normalise the targets or to report `R2.PerOutput`, which is
unitless by construction.

**Applies to** — net10.0, netstandard2.0.

**See also** — `MeanSquaredError.Score`, `RootMeanSquaredError.PerOutput`, `R2.PerOutput`,
the [Python equivalence table](../../../equivalence.md).
