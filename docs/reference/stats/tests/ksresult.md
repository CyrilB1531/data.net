# KsResult

A two-sample Kolmogorov-Smirnov result.

<!-- docs-declaration -->

```csharp
public sealed record KsResult(double Statistic, double PValue, double StatisticLocation, int StatisticSign)
```

**Properties** — `Statistic` is the supremum distance between the two empirical distributions.
`PValue` is the p-value on the requested tail. `StatisticLocation` is the observed value at which
that supremum is attained. `StatisticSign` is `+1` when the first sample's empirical distribution
exceeds the second's at that point, `-1` when it falls below.

**Example** — two samples of the same size, shifted apart.

```csharp
using Lodestar.Stats;

double[] left = [1.0, 2.0, 3.0, 4.0, 5.0];
double[] right = [3.0, 4.0, 5.0, 6.0, 7.0];

KsResult result = KolmogorovSmirnov.TwoSample(left, right);

double location = result.StatisticLocation;   // => 3
int sign = result.StatisticSign;              // => 1
```

**Remarks** — `StatisticLocation` and `StatisticSign` are what `TestResult` cannot carry: a
statistic and a p-value alone say *how far apart* two distributions are, not *where*. A sign of
`+1` here means `left`'s empirical CDF is ahead of `right`'s at `location` — every value in
`left` has already been reached by `3.0`, where `right`'s has not — which is exactly what
`left`'s values sitting below `right`'s means.

Being a `record`, two results with the same four fields are equal.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`KolmogorovSmirnov.TwoSample`](kolmogorovsmirnov-twosample.md),
[`TestResult`](testresult.md), the [Python equivalence table](../../../equivalence.md).
