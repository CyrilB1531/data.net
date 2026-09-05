# TTest.OneSample

The one-sample *t*-test against a stated population mean.

<!-- docs-declaration -->

```csharp
public static TTestResult OneSample(ReadOnlySpan<double> sample, double populationMean, Alternative alternative = Alternative.TwoSided)
```

**Parameters** — `sample` is the data, at least two values; the span is read, never modified.
`populationMean` is the mean the null hypothesis states. `alternative` says which tail the
p-value covers.

**Returns** — `TTestResult`: the t statistic, the p-value, and the degrees of freedom,
`sample.Length - 1`.

**Exceptions** — `ArgumentException` when `sample` holds fewer than two values.
`ArgumentOutOfRangeException` when `populationMean` is `NaN` or infinite.

**Example** — a sample tested against a stated mean of `10.0`.

```csharp
using Lodestar.Stats;

double[] sample = [12.1, 9.4, 15.0, 11.2, 8.8, 13.9, 10.5];

TTestResult result = TTest.OneSample(sample, populationMean: 10.0);

double t = Math.Round(result.Statistic, 4);   // => 1.8085
double df = result.Df;                        // => 6
```

**Remarks** — the confidence interval [`TTestResult.ConfidenceInterval`](ttestresult-confidenceinterval.md)
returns brackets the population mean itself, not the statistic's offset from it — scipy does the
same. `populationMean` may be any finite number, including one nowhere near the sample: the test
answers "how surprising is this sample if the true mean were this?", and a wildly wrong guess
just produces a wildly small p-value.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TTest.Independent`](ttest-independent.md), [`TTest.Paired`](ttest-paired.md),
[`TTestResult.ConfidenceInterval`](ttestresult-confidenceinterval.md), the
[Python equivalence table](../../../equivalence.md).
