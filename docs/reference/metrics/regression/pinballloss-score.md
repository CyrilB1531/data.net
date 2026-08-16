# PinballLoss.Score

The mean pinball loss at the quantile `alpha`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, double alpha = 0.5, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default, ReadOnlySpan<double> outputWeights = default)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major when there is
more
than one output. `alpha` is the quantile being scored, in `[0, 1]`, `0.5` by default.
`outputCount`
is how many outputs each row holds, `sampleWeight` weights the rows, and `outputWeights` weights
the
outputs in the reduction.

**Returns** — `double`, never negative, `0` only for an exact prediction. In the target's own
units.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty,
or
it holds a non-finite value; `ArgumentOutOfRangeException` when `outputCount` is below one, or
`alpha` is outside `[0, 1]` — including `NaN`.

**Example** — the same four predictions, scored at the median and at the 90th percentile. The
model
over-predicts twice and under-predicts twice, so asking for a high quantile forgives it.

```csharp
using Lodestar.Metrics;

double[] yTrue = [3.0, -0.5, 2.0, 7.0];
double[] yPred = [2.5, 0.0, 2.0, 8.0];

double median = PinballLoss.Score(yTrue, yPred);              // => 0.25
double ninetieth = PinballLoss.Score(yTrue, yPred, 0.9);      // => 0.15
```

**Remarks** — this is the metric for **prediction intervals** rather than point forecasts. If you
are
training a model to output "the 90th percentile of tomorrow's demand", no symmetric error can
score
it: the model is *supposed* to over-predict most of the time, and mean absolute error would punish
it
for doing its job. Pinball loss charges an under-prediction `alpha` per unit and an
over-prediction
`1 - alpha`, so it is minimised exactly by the true quantile.

`alpha = 0.5` charges both sides at 0.5, which makes it precisely **half** the mean absolute error
—
`0.25` above against `MeanAbsoluteError.Score`'s `0.5`. That factor of two is not a normalization
anyone chose; it falls out of the definition, and it means the default is not interchangeable with
mean absolute error even though it ranks models identically.

Two traps. `alpha` is the quantile you asked the model for, not a tuning knob: scoring a median
forecast at `alpha = 0.9` produces a smaller number and tells you nothing. And the number is only
comparable between models asked for the *same* quantile — a 0.9 loss and a 0.5 loss on the same
data
are different scales, as the example shows.

The name drops Python's `mean_` prefix to match the other ten types here; `alpha` outside `[0, 1]`
raises `ArgumentOutOfRangeException` where scikit-learn raises `InvalidParameterError`.

**Applies to** — net10.0, netstandard2.0.

**See also** — `PinballLoss.PerOutput`, `MeanAbsoluteError.Score`, `MedianAbsoluteError.Score`,
the [Python equivalence table](../../../equivalence.md).
