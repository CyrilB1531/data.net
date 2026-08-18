# D2Pinball.PerOutput

The explained fraction, one number per output column — `d2_pinball_score(…, multioutput="raw_values")`.

<!-- docs-declaration -->

```csharp
public static double[] PerOutput(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, double alpha = 0.5, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default, ZeroDivision zeroDivision = ZeroDivision.NaN)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major. `alpha` is the
quantile being scored, in `[0, 1]`. `outputCount` is how many outputs each row holds. `sampleWeight`
is one weight per row. `zeroDivision` decides the answer for fewer than two samples; see
[`D2Pinball.Score`](d2pinball-score.md).

**Returns** — a fresh `double[]` of `outputCount` entries, in column order. Each is what
[`D2Pinball.Score`](d2pinball-score.md) would return for that column on its own, quantile and all —
the baseline is computed per column, not once for the whole matrix.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty, or
it holds a non-finite value. `ArgumentOutOfRangeException` when `outputCount` is below one, or
`alpha` is outside `[0, 1]`. `UndefinedMetricException` when there are fewer than two samples and
`zeroDivision` is `ZeroDivision.Throw`.

**Example** — three samples over two outputs, scored at the median.

```csharp
using Lodestar.Metrics;

double[] truth = [0.5, 1.0, 1.0, 1.0, 7.0, -6.0];
double[] predicted = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];

double[] columns = D2Pinball.PerOutput(truth, predicted, alpha: 0.5, outputCount: 2);
double second = columns[1];  // => 0.5714…
```

Averaging the two gives what [`D2Pinball.Score`](d2pinball-score.md) returns with no
`outputWeights` — `0.5164…` here.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`D2Pinball.Score`](d2pinball-score.md),
[`D2AbsoluteError.PerOutput`](d2absoluteerror-peroutput.md), the
[Python equivalence table](../../../equivalence.md).
