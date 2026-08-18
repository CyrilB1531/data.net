# D2AbsoluteError.Score

The fraction of absolute error explained — `sklearn.metrics.d2_absolute_error_score`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default, ReadOnlySpan<double> outputWeights = default, ZeroDivision zeroDivision = ZeroDivision.NaN)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major when there is more
than one output. `outputCount` is how many outputs each row holds. `sampleWeight` is one weight per
row — per sample, not per value. `outputWeights` is a weight per output, `multioutput=[…]`; omit it
for `multioutput="uniform_average"`. `zeroDivision` decides the answer for fewer than two samples.

**Returns** — `double`. `1` for a perfect prediction, `0` for one no better than always predicting
the weighted median, and negative below that.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty, or
it holds a non-finite value. `ArgumentOutOfRangeException` when `outputCount` is below one.
`UndefinedMetricException` when there are fewer than two samples and `zeroDivision` is
`ZeroDivision.Throw`.

**Example** — the worked case, and the same weights read differently.

```csharp
using Lodestar.Metrics;

double[] truth = [1.0, 2.0, 3.0, 4.0];
double[] predicted = [1.5, 2.5, 2.0, 4.5];

double explained = D2AbsoluteError.Score(truth, predicted);  // => 0.375
```

```csharp
using Lodestar.Metrics;

double[] truth = [1.0, 2.0, 3.0, 4.0];
double[] predicted = [1.5, 2.5, 2.0, 4.5];
double[] weights = [1.0, 2.0, 3.0, 4.0];

double weighted = D2AbsoluteError.Score(truth, predicted, 1, weights);  // => 0.1875
```

Weighting the later samples more halves the score, because the third prediction — the worst of the
four — is where most of the weight now sits.

**Remarks** — [`D2Pinball.Score`](d2pinball-score.md) at `alpha: 0.5`, asserted across the whole
frozen corpus rather than on one pair, since the two reach their baseline through different code.

A truth that never varies answers `0` rather than raising: the reference masks that denominator
here, where `d2_tweedie_score` divides by it —
[`D2Tweedie.Score`](d2tweedie-score.md) reproduces that side of the split.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`D2AbsoluteError.PerOutput`](d2absoluteerror-peroutput.md),
[`D2Pinball.Score`](d2pinball-score.md), [`R2.Score`](r2-score.md),
[`MeanAbsoluteError.Score`](meanabsoluteerror-score.md), the
[Python equivalence table](../../../equivalence.md).
