# R2.PerOutput

One score per output, unreduced.

<!-- docs-declaration -->

```csharp
public static double[] PerOutput(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default, bool forceFinite = true, ZeroDivision zeroDivision = ZeroDivision.NaN)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major. `outputCount`
is
how many outputs each row holds, `sampleWeight` weights the rows, `forceFinite` answers a truth of
zero variance, and `zeroDivision` answers fewer than two samples.

**Returns** — a fresh `double[]` of `outputCount` entries, in column order.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty,
or
it holds a non-finite value; `ArgumentOutOfRangeException` when `outputCount` is below one;
`UndefinedMetricException` when there are fewer than two samples and `zeroDivision` is
`ZeroDivision.Throw`.

**Example** — three samples, two outputs.

```csharp
using DataNet.Metrics;

double[] yTrue = [0.5, 1.0, -1.0, 1.0, 7.0, -6.0];
double[] yPred = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];

double[] perOutput = R2.PerOutput(yTrue, yPred, outputCount: 2);
double first = perOutput[0];    // => 0.9654…
double second = perOutput[1];   // => 0.9081…
```

**Remarks** — because `R2` is unitless, this is the multioutput array whose entries really are
comparable with one another, which is what makes it the honest way to look at a model predicting
several unrelated things. `MeanSquaredError.PerOutput` cannot do that.

There is one shape divergence from scikit-learn, and it is stated rather than hidden: on fewer
than
two samples with more than one output, this returns **one `NaN` per output**, where `r2_score`
returns a single scalar `nan` before it ever consults `multioutput`. No number differs — every
scalar-returning path here still gives `NaN` — and a one-element array would break this method's
own
contract of one value per output.

The trap is the same one `Score` has, one level down: a negative entry is not a bug. It means that
output is predicted worse than its own mean would be, and on a multioutput model that is usually
one
column with almost no variance rather than a broken model.

**Applies to** — net10.0, netstandard2.0.

**See also** — `R2.Score`, `R2.VarianceWeighted`, `ExplainedVariance.PerOutput`,
the [Python equivalence table](../../../equivalence.md).
