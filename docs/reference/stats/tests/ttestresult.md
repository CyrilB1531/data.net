# TTestResult

A t-test's result: the statistic, the p-value and the degrees of freedom.

<!-- docs-declaration -->

```csharp
public sealed record TTestResult(double Statistic, double PValue, double Df)
```

**Properties** — `Statistic` is the t statistic. `PValue` is the p-value on the requested tail.
`Df` is the degrees of freedom: integral for Student and for the paired and one-sample tests,
fractional for Welch, whose Satterthwaite denominator is not a count of anything.

**Example** — a one-sample test's degrees of freedom, which is always `sample.Length - 1`.

```csharp
using Lodestar.Stats;

double[] sample = [12.1, 9.4, 15.0, 11.2, 8.8, 13.9, 10.5];

TTestResult result = TTest.OneSample(sample, populationMean: 10.0);

double df = result.Df;   // => 6
```

**Remarks** — `Df` is a `double`, not an `int`, because [`TTest.Independent`](ttest-independent.md)
under [`Variance.Welch`](variance.md) needs to report a fractional value; every other entry
point happens to land on a whole number, which is why this example's `6` prints with no decimal
point at all — it is still the same `double` field.

Alongside the three public properties, a `TTestResult` privately carries the estimate its test
compared and its standard error — internal, since scikit-learn-style callers only reach them
through [`ConfidenceInterval`](ttestresult-confidenceinterval.md), which is where they surface.

Being a `record`, two results with the same statistic, p-value and degrees of freedom are equal.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TTest.Independent`](ttest-independent.md), [`TTest.Paired`](ttest-paired.md),
[`TTest.OneSample`](ttest-onesample.md), [`TestResult`](testresult.md), the
[Python equivalence table](../../../equivalence.md).

## Members

| Member | What it does |
| --- | --- |
| [`TTestResult.ConfidenceInterval`](ttestresult-confidenceinterval.md) | The confidence interval for the difference this test measured. |
