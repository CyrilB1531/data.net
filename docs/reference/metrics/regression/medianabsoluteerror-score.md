# MedianAbsoluteError.Score

The median of the absolute residuals.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default, ReadOnlySpan<double> outputWeights = default)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major when there is
more
than one output. `outputCount` is how many outputs each row holds, `sampleWeight` weights the
rows,
and `outputWeights` weights the outputs in the reduction.

**Returns** — `double`, never negative, in the target's own units.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty,
or
it holds a non-finite value; `ArgumentOutOfRangeException` when `outputCount` is below one.

**Example** — three exact predictions and one catastrophic one. `MeanAbsoluteError.Score` on this
data is `24`.

```csharp
using Lodestar.Metrics;

double[] yTrue = [1.0, 2.0, 3.0, 100.0];
double[] yPred = [1.0, 2.0, 3.0, 4.0];

double typical = MedianAbsoluteError.Score(yTrue, yPred);   // => 0
```

**Remarks** — reach for this when your data has outliers you do not believe in — mistyped labels,
a
sensor that dropped out, a fraud in the training set. Its breakdown point is 50%: half your
samples
can be arbitrarily wrong and this number does not move. That is a genuinely different question
from
the one `MeanAbsoluteError.Score` answers, and reporting the two side by side is the fastest way
to
see whether a dataset has a tail.

Which is the trap, stated as bluntly as the example above puts it: **`0` here does not mean the
model
is good.** It means at least half the predictions are exact, and says nothing about the other
half.
Never report this alone; pair it with `MeanAbsoluteError.Score` or `MaxError.Score`.

Under `sampleWeight` this stops being the value at the halfway point. scikit-learn takes an
*averaged* weighted percentile — the mean of the first value whose cumulative weight reaches half
the
total and the one just past the last that comes within one machine epsilon of it — and that
tolerance
is load-bearing rather than decoration: a uniform weight is *usually* the ordinary median and not
always. Measured, `[0.7] * 10` gives `5.0` on the weighted path against `4.5` unweighted, while
`[0.1] * 10` gives `4.5` on both. Both agree, divergently, with scikit-learn —
[decision 0024](../../../decisions/0024-weighted-median-averages-within-scikit-learns-epsilon.md).

**Applies to** — net10.0, netstandard2.0.

**See also** — `MedianAbsoluteError.PerOutput`, `MeanAbsoluteError.Score`, `MaxError.Score`,
[decision 0024](../../../decisions/0024-weighted-median-averages-within-scikit-learns-epsilon.md),
the [Python equivalence table](../../../equivalence.md).
