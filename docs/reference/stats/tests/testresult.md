# TestResult

A test statistic and the p-value that goes with it.

<!-- docs-declaration -->

```csharp
public sealed record TestResult(double Statistic, double PValue)
```

**Properties** — `Statistic` is the test statistic, on whichever scale the family defines.
`PValue` is the probability of a statistic at least this extreme under the null.

**Example** — the two numbers a rank-based test hands back.

```csharp
using Lodestar.Stats;

double[] control = [7.0, 3.0, 6.0, 2.0, 8.0, 5.0];
double[] treated = [9.0, 12.0, 8.0, 11.0, 15.0, 10.0];

TestResult result = MannWhitney.Test(control, treated);

double statistic = result.Statistic;                // => 0.5
double p = Math.Round(result.PValue, 6);             // => 0.006392
```

**Remarks** — eight of the ten families return exactly this, because eight of the ten `scipy`
calls return exactly this — measured, not assumed:
[`MannWhitney.Test`](mannwhitney-test.md), [`Wilcoxon.Paired`](wilcoxon-paired.md) and
[`Wilcoxon.OneSample`](wilcoxon-onesample.md), [`ChiSquare.GoodnessOfFit`](chisquare-goodnessoffit.md),
[`FisherExact.Test`](fisherexact-test.md), [`OneWayAnova.Test`](onewayanova-test.md),
[`KruskalWallis.Test`](kruskalwallis-test.md) and [`ShapiroWilk.Test`](shapirowilk-test.md). The
three that carry more — a *t*-test's degrees of freedom, a contingency table's expected
frequencies, a Kolmogorov-Smirnov result's location and sign — have their own record,
[`TTestResult`](ttestresult.md), [`Chi2ContingencyResult`](chi2contingencyresult.md) and
[`KsResult`](ksresult.md), rather than making the other eight pay for fields they would leave
empty.

Being a `record`, two results with the same statistic and p-value are equal.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TTestResult`](ttestresult.md), [`Chi2ContingencyResult`](chi2contingencyresult.md),
[`KsResult`](ksresult.md), the [Python equivalence table](../../../equivalence.md).
