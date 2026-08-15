# ExplainedVariance.PerOutput

One score per output, unreduced.

<!-- docs-declaration -->

```csharp
public static double[] PerOutput(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default, bool forceFinite = true)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major. `outputCount`
is
how many outputs each row holds, `sampleWeight` weights the rows, and `forceFinite` clamps the
zero-variance case.

**Returns** — a fresh `double[]` of `outputCount` entries, in column order.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty,
or
it holds a non-finite value; `ArgumentOutOfRangeException` when `outputCount` is below one.

**Example** — three samples, two outputs; the second output is predicted with a constant offset
and
therefore scores perfectly here.

```csharp
using DataNet.Metrics;

double[] yTrue = [0.5, 1.0, -1.0, 1.0, 7.0, -6.0];
double[] yPred = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];

double[] perOutput = ExplainedVariance.PerOutput(yTrue, yPred, outputCount: 2);
double first = perOutput[0];    // => 0.9677…
double second = perOutput[1];   // => 1
```

**Remarks** — this is scikit-learn's `multioutput="raw_values"`, and the reason to want it is that
a
mean over outputs hides which output is failing. The array is in column order: entry `i` is the
score of the value at offset `i` of every row.

The trap is the layout of the input rather than of the output. `yTrue` and `yPred` are
**row-major**
— one sample's outputs are contiguous — which is the transpose of a column-per-output table.
Passing
a column-major array with the right `outputCount` produces numbers rather than an error, and they
are
meaningless.

**Applies to** — net10.0, netstandard2.0.

**See also** — `ExplainedVariance.Score`, `ExplainedVariance.VarianceWeighted`, `R2.PerOutput`,
the [Python equivalence table](../../../equivalence.md).
