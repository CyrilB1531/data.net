# ExplainedVariance.Score

One number for the whole prediction: the share of the truth's variance the residuals do not carry.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default, ReadOnlySpan<double> outputWeights = default, bool forceFinite = true)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, the same length and
row-major when there is more than one output. `outputCount` is how many outputs each row holds,
`1`
by default. `sampleWeight` weights the rows. `outputWeights` weights the outputs when the
per-output
scores are reduced; omit it for a plain mean. `forceFinite` clamps the zero-variance case to `1`
or
`0` rather than letting it be `nan` or `-inf`.

**Returns** — `double` at most `1`: `1` for a prediction that tracks the truth exactly up to a
constant, `0` for one no better than the mean, and negative for one that is worse.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty,
or
it holds a non-finite value; `ArgumentOutOfRangeException` when `outputCount` is below one.

**Example** — a prediction that is right about every change and wrong by exactly `1` every time.
`R2.Score` on the same data is `-0.5`.

```csharp
using DataNet.Metrics;

double[] yTrue = [1.0, 2.0, 3.0];
double[] yPred = [2.0, 3.0, 4.0];

double explained = ExplainedVariance.Score(yTrue, yPred);   // => 1
```

**Remarks** — one term separates this from `R2`, and the example above is it: the residuals are
centred on their own mean before being squared, so a **uniform bias costs nothing here and costs
`R2` everything**. That makes this the right metric when the offset is going to be calibrated away
later — a sensor with an unknown zero, a forecast you will recentre — and the wrong one when the
offset is the error you are trying to measure.

Because a bias is free, this is always at least as large as `R2` on the same data, and the gap
between the two is exactly the bias. Reporting both is a cheap and unusually informative pair:
equal
numbers mean the model is unbiased, and a wide gap says the shape is right and the level is not.

The trap is quoting it alone as though it were `R2`. A model that predicts `y + 1000` scores `1`
here
and is useless. If a reader is going to see one number, `R2` is the safer one.

Unlike `R2`, this takes no `ZeroDivision`: it has no fewer-than-two-samples case to route, so
`ExplainedVariance.Score([3.0], [5.0])` is `1.0` where `R2.Score` on the same input is `NaN`. The
reasoning is in
[decision
0026](../../../decisions/0026-r2-and-explainedvariance-split-their-undefined-cases-differently.md).

**Applies to** — net10.0, netstandard2.0.

**See also** — `ExplainedVariance.PerOutput`, `ExplainedVariance.VarianceWeighted`, `R2.Score`,
[decision
0026](../../../decisions/0026-r2-and-explainedvariance-split-their-undefined-cases-differently.md),
the [Python equivalence table](../../../equivalence.md).
