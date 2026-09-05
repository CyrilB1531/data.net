# ExactMethod

Whether a p-value comes from the exact null distribution or its normal approximation.

<!-- docs-declaration -->

```csharp
public enum ExactMethod { Auto, Exact, Asymptotic }
```

**Members** — `Auto` is exact when the sample is small and free of ties, asymptotic otherwise;
scipy's `'auto'`. `Exact` enumerates the null distribution whatever the sample; measured, scipy
computes an exact p-value on tied data too rather than refusing, so this does the same. `Asymptotic`
uses the normal (or Kolmogorov) approximation whatever the sample size.

**Example** — the same tied sample, exact and asymptotic disagreeing on the number.

```csharp
using Lodestar.Stats;

double[] control = [7.0, 3.0, 6.0, 2.0, 8.0, 5.0];
double[] treated = [9.0, 12.0, 8.0, 11.0, 15.0, 10.0];

TestResult auto = MannWhitney.Test(control, treated);
TestResult exact = MannWhitney.Test(control, treated, method: ExactMethod.Exact);

double autoP = Math.Round(auto.PValue, 6);     // => 0.006392
double exactP = Math.Round(exact.PValue, 6);   // => 0.004329
```

**Remarks — `Auto` never throws; an explicit `Exact` can.** `control` and `treated` share a
value, so `Auto` falls to the asymptotic route on its own — the two samples are otherwise small
enough that `Auto` would have taken the exact route if they had been tie-free. Asking for
`Exact` explicitly still answers, on a different number, because the exact table is built and
read regardless of ties.

Choosing `Exact` is not free at every size. [`MannWhitney.Test`](mannwhitney-test.md) refuses it
past `x.Length * y.Length > 20_000`, and [`Wilcoxon.Paired`](wilcoxon-paired.md) and
[`Wilcoxon.OneSample`](wilcoxon-onesample.md) refuse it past 500 ranked values — both throwing
`ArgumentOutOfRangeException` rather than building a table that would cost tens of seconds or
overflow a `double`. `Auto` is bounded by the same limits internally, but never throws for it:
past the bound it silently falls back to the asymptotic answer instead, because nothing the
caller wrote asked for an exact result in the first place.
[`KolmogorovSmirnov.TwoSample`](kolmogorovsmirnov-twosample.md) is the one exception — its exact
route costs `O(n · m)`, not the quadratic-or-worse cost the rank-based tests' tables do, so
`Exact` there is honoured at any size.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`MannWhitney.Test`](mannwhitney-test.md), [`Wilcoxon.Paired`](wilcoxon-paired.md),
[`KolmogorovSmirnov.TwoSample`](kolmogorovsmirnov-twosample.md), the
[Python equivalence table](../../../equivalence.md).
