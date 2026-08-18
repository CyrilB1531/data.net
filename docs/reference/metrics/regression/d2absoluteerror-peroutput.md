# D2AbsoluteError.PerOutput

The explained fraction, one number per output column —
`d2_absolute_error_score(…, multioutput="raw_values")`.

<!-- docs-declaration -->

```csharp
public static double[] PerOutput(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default, ZeroDivision zeroDivision = ZeroDivision.NaN)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major. `outputCount` is
how many outputs each row holds. `sampleWeight` is one weight per row. `zeroDivision` decides the
answer for fewer than two samples; see [`D2AbsoluteError.Score`](d2absoluteerror-score.md).

**Returns** — a fresh `double[]` of `outputCount` entries, in column order. Each column gets its own
median baseline, which is why the entries are not recoverable from the averaged score.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty, or
it holds a non-finite value. `ArgumentOutOfRangeException` when `outputCount` is below one.
`UndefinedMetricException` when there are fewer than two samples and `zeroDivision` is
`ZeroDivision.Throw`.

**Example** — three samples over two outputs.

```csharp
using Lodestar.Metrics;

double[] truth = [0.5, 1.0, 1.0, 1.0, 7.0, -6.0];
double[] predicted = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];

double[] columns = D2AbsoluteError.PerOutput(truth, predicted, outputCount: 2);
double first = columns[0];  // => 0.4615…
```

The two columns score `0.4615…` and `0.5714…`, whose plain mean is the `0.5164…` that
[`D2AbsoluteError.Score`](d2absoluteerror-score.md) reports with no `outputWeights`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`D2AbsoluteError.Score`](d2absoluteerror-score.md),
[`D2Pinball.PerOutput`](d2pinball-peroutput.md), the
[Python equivalence table](../../../equivalence.md).
