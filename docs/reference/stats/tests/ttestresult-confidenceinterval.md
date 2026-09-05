# TTestResult.ConfidenceInterval

The confidence interval for the difference this test measured.

<!-- docs-declaration -->

```csharp
public (double Low, double High) ConfidenceInterval(double level = 0.95)
```

**Parameters** — `level` is the confidence level, strictly between 0 and 1.

**Returns** — `(double Low, double High)`: the interval around the mean, or the difference of
means, the test compared. A one-sided test's interval is half-open — narrower in only one
direction, since the test spent its whole error budget on one side — so the far bound is
`double.PositiveInfinity` or `double.NegativeInfinity` rather than merely a larger number.

**Exceptions** — `ArgumentOutOfRangeException` when `level` is `NaN` or outside `(0, 1)`.

**Example** — a two-sided interval, and a one-sided one on the same test.

```csharp
using Lodestar.Stats;

double[] sample = [12.1, 9.4, 15.0, 11.2, 8.8, 13.9, 10.5];
TTestResult result = TTest.OneSample(sample, populationMean: 10.0);

(double low, double high) = result.ConfidenceInterval(0.95);
double roundedLow = Math.Round(low, 4);     // => 9.4503
double roundedHigh = Math.Round(high, 4);   // => 13.664

TTestResult oneSided = TTest.OneSample(sample, populationMean: 10.0, Alternative.Greater);
(double oneLow, double oneHigh) = oneSided.ConfidenceInterval(0.95);
double roundedOneLow = Math.Round(oneLow, 4);   // => 9.884
bool isInfinite = double.IsPositiveInfinity(oneHigh);   // => True
```

**Remarks** — the interval brackets the population mean itself for
[`TTest.OneSample`](ttest-onesample.md), and the difference of means for
[`TTest.Independent`](ttest-independent.md) and [`TTest.Paired`](ttest-paired.md) — never the
statistic's own scale. `Alternative.TwoSided` spends `level`'s error budget on both tails, so its
interval is narrower on each side than the one-sided interval's single open side is on its own;
`Alternative.Greater` and `Alternative.Less` each spend the whole budget on one side and leave the
other unbounded, which is why `oneHigh` above is infinite rather than merely larger than `high`.

This mirrors `scipy`'s own `TtestResult.confidence_interval`, which returns the equivalent of a
named tuple rather than a type of its own — a `(double, double)` here for the same reason.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TTestResult`](ttestresult.md), [`TTest.OneSample`](ttest-onesample.md),
[`TTest.Independent`](ttest-independent.md), [`TTest.Paired`](ttest-paired.md), the
[Python equivalence table](../../../equivalence.md).
