# SplitConformal.Quantile

The calibrated quantile: the score a new point must not exceed to fall inside the prediction.

<!-- docs-declaration -->

```csharp
public static double Quantile(ReadOnlySpan<double> scores, double alpha)
```

**Parameters** — `scores` are the calibration scores, in any order; the span is read, never
modified. `alpha` is the miscoverage level, strictly between 0 and 1: `0.1` asks for 90 % coverage.

**Returns** — `double`, the `k`-th smallest score with `k = ceil((n + 1)(1 − alpha))`, 1-based; or
`double.PositiveInfinity` when `k` exceeds the number of scores.

**Exceptions** — `ArgumentException` when `scores` is empty. `ArgumentOutOfRangeException` when
`alpha` is `NaN` or outside `(0, 1)`.

**Example** — nine scores at 20 % miscoverage. `k = ceil(10 × 0.8) = 8`, so the answer is the
eighth smallest, which is `0.4`.

```csharp
using Lodestar.Conformal;

double[] scores = [0.2, 0.1, 0.4, 0.3, 0.5, 0.1, 0.4, 0.3, 0.1];

double q = SplitConformal.Quantile(scores, 0.2);   // => 0.4
```

**Remarks** — the `+ 1` is not a rounding fudge. It is the new point counting itself among the
calibration points, and it is what makes the coverage guarantee finite-sample rather than
asymptotic: the probability that a fresh exchangeable point's score falls at or below the `k`-th of
`n` is at least `k / (n + 1)`, whatever the model and whatever the distribution.

**It is not a numpy quantile.** `numpy.quantile(scores, (1 − alpha)(n + 1)/n, method="higher")`
indexes a different order statistic and disagrees with this rule on about a fifth of random
`(n, alpha)` pairs; `method="inverted_cdf"` is the same rule algebraically and still disagrees
where evaluating the level in floating point moves the product across an integer. MAPIE follows the
ceiling rule, and so does this.
[Decision 0070](../../../decisions/0070-k-greater-than-n-returns-an-infinite-interval.md) has the
measurement.

When `alpha < 1 / (n + 1)` the rule asks for a score the calibration set does not hold, and the
answer is `double.PositiveInfinity` — a trivial prediction, with real coverage. MAPIE raises there,
and under `allow_infinite_bounds` returns the largest score instead, which is *narrower* than the
level asked for. If an infinite interval is unacceptable at your call site, test
`double.IsInfinity(q)` and collect more calibration data; there is no third answer.

**The guarantee assumes exchangeability** between the calibration and the test data. See the
guide's [*Exchangeability*](../../../guides/conformal.md#exchangeability) section, which is the
part of this documentation worth reading before the API.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SplitConformal.Interval`](splitconformal-interval.md),
[`SplitConformal.PredictionSet`](splitconformal-predictionset.md), the
[Python equivalence table](../../../equivalence.md).
