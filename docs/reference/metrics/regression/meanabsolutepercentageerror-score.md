# MeanAbsolutePercentageError.Score

The mean of `|yTrue - yPred| / |yTrue|`, with the denominator clamped away from zero.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default, ReadOnlySpan<double> outputWeights = default)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major when there is
more
than one output. `outputCount` is how many outputs each row holds, `sampleWeight` weights the
rows,
and `outputWeights` weights the outputs in the reduction.

**Returns** — `double`, never negative, and a **fraction rather than a percentage** despite the
name: `0.125` means 12.5%. It has no upper bound.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty,
or
it holds a non-finite value; `ArgumentOutOfRangeException` when `outputCount` is below one.

**Example** — four quantities of very different sizes, each predicted about 10% out.

```csharp
using DataNet.Metrics;

double[] yTrue = [100.0, 50.0, 200.0, 25.0];
double[] yPred = [110.0, 45.0, 180.0, 30.0];

double error = MeanAbsolutePercentageError.Score(yTrue, yPred);   // => 0.125
```

**Remarks** — this is the metric for targets that span orders of magnitude, where being 10 out on
a
sale of 100 and 1000 out on a sale of 10 000 are the same mistake. `MeanAbsoluteError.Score` would
report the second as a hundred times worse; this reports them as equal, which is usually what a
business means by "how accurate is the forecast".

Three traps, and the first one bites everybody.

**The result is a fraction, not a percentage.** Multiply by 100 before putting a `%` on it.

**It is asymmetric, and it rewards under-prediction.** The denominator is the truth, so a
prediction
of `0` on a truth of `100` scores `1.0` — the worst a prediction can score by under-shooting —
while
a prediction of `300` on the same truth scores `2.0`. A model tuned to minimise this will predict
low
on purpose.

**A truth near zero explodes it.** The denominator is clamped at numpy's machine epsilon, `2^-52`,
which is not the same thing as .NET's `double.Epsilon` — that is 292 orders of magnitude smaller —
so `MeanAbsolutePercentageError.Score([0.0], [1.0])` is `4503599627370496.0` rather than infinity.
The number is finite, matches scikit-learn exactly, and is still meaningless: one sample whose
truth
is zero will dominate any average it lands in. Filter them out or use another metric.

**Applies to** — net10.0, netstandard2.0.

**See also** — `MeanAbsolutePercentageError.PerOutput`, `MeanAbsoluteError.Score`,
`MeanSquaredLogError.Score`, the [Python equivalence table](../../../equivalence.md).
