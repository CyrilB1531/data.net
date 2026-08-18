# D2Pinball.Score

The fraction of pinball loss explained — `sklearn.metrics.d2_pinball_score`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, double alpha = 0.5, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default, ReadOnlySpan<double> outputWeights = default, ZeroDivision zeroDivision = ZeroDivision.NaN)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major when there is more
than one output. `alpha` is the quantile being scored, in `[0, 1]`, `0.5` by default. `outputCount`
is how many outputs each row holds. `sampleWeight` is one weight per **row**, not per value.
`outputWeights` is a weight per output, `multioutput=[…]`; omit it for `multioutput="uniform_average"`.
`zeroDivision` decides the answer for fewer than two samples.

**Returns** — `double`. `1` for a perfect prediction, `0` for one no better than the best constant,
and negative below that. A column whose truth never varies contributes `0`.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty, or
it holds a non-finite value. `ArgumentOutOfRangeException` when `outputCount` is below one, or
`alpha` is outside `[0, 1]` — `NaN` included. `UndefinedMetricException` when there are fewer than
two samples and `zeroDivision` is `ZeroDivision.Throw`.

**Example** — the same four samples read at three quantiles.

```csharp
using Lodestar.Metrics;

double[] truth = [1.0, 2.0, 3.0, 4.0];
double[] predicted = [1.5, 2.5, 2.0, 4.5];

double median = D2Pinball.Score(truth, predicted);  // => 0.375
```

```csharp
using Lodestar.Metrics;

double[] truth = [1.0, 2.0, 3.0, 4.0];
double[] predicted = [1.5, 2.5, 2.0, 4.5];

double upper = D2Pinball.Score(truth, predicted, alpha: 0.9);  // => -0.75…
```

The upper quantile scores below zero: these predictions are a decent median and a poor 90th
percentile, which is the distinction the metric exists to draw.

**Remarks** — the denominator is the loss of predicting the weighted quantile of the truth at the
same `alpha`. Which of the two candidate order statistics that quantile takes cannot be observed
here: the two differ exactly where the quantile is ambiguous, and the pinball loss is flat across
that interval — measured over four fixtures at five alphas each, both readings give the same score
every time.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`D2Pinball.PerOutput`](d2pinball-peroutput.md),
[`D2AbsoluteError.Score`](d2absoluteerror-score.md), [`PinballLoss.Score`](pinballloss-score.md),
the [Python equivalence table](../../../equivalence.md).
