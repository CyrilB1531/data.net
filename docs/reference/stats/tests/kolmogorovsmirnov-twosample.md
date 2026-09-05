# KolmogorovSmirnov.TwoSample

Compares two samples by the largest gap between their empirical distributions.

<!-- docs-declaration -->

```csharp
public static KsResult TwoSample(ReadOnlySpan<double> a, ReadOnlySpan<double> b, Alternative alternative = Alternative.TwoSided, ExactMethod method = ExactMethod.Auto)
```

**Parameters** — `a` and `b` are the two samples, each at least one value; both spans are read,
never modified. `alternative` says which direction of gap counts: `Alternative.TwoSided` takes
the largest gap in either direction, the one-sided values take the largest gap in one.
`method` chooses the exact null distribution, its asymptotic approximation, or a choice between
them by the sample sizes.

**Returns** — `KsResult`: the distance, the p-value, where that supremum is attained, and its
sign.

**Exceptions** — `ArgumentException` when either sample is empty. `ArgumentOutOfRangeException`
when `method` is `ExactMethod.Exact` and `a.Length * b.Length` exceeds 1,000,000.

**Example** — two samples of the same size, shifted apart.

```csharp
using Lodestar.Stats;

double[] left = [1.0, 2.0, 3.0, 4.0, 5.0];
double[] right = [3.0, 4.0, 5.0, 6.0, 7.0];

KsResult result = KolmogorovSmirnov.TwoSample(left, right);

double d = Math.Round(result.Statistic, 4);        // => 0.4
double p = Math.Round(result.PValue, 6);           // => 0.873016
double location = result.StatisticLocation;        // => 3
int sign = result.StatisticSign;                   // => 1
```

**Remarks** — a `sign` of `+1` means `left`'s empirical distribution exceeds `right`'s at
`location`; every value in `left` reaches `3` before `right` does, which is the point the largest
gap is measured at here.

`method` chooses between an exact route and an asymptotic one, the same shape as
[`MannWhitney.Test`](mannwhitney-test.md)'s and [`Wilcoxon.Paired`](wilcoxon-paired.md)'s.
`Auto` switches to the asymptotic route once `a.Length * b.Length` passes 10,000, purely because
the exact answer stops being worth its cost there, not because it would be wrong.

**The exact route has a size bound `Auto` cannot cross.** Past `a.Length * b.Length = 1,000,000`,
`ExactPValue`'s lattice-path recurrence allocates a fresh `double[b.Length + 1]` row on each of
`a.Length + 1` outer iterations — measured at 8,000 × 8,000 (a product of 64,000,000), that walk
allocates 673 MB and climbs quadratically with the product from there. Passing
`ExactMethod.Exact` past the bound throws; `ExactMethod.Auto` never does, falling back to the
asymptotic answer instead, because nothing the caller wrote asked for an exact result — and
`Auto`'s own 10,000 threshold keeps it two orders of magnitude clear of the bound regardless.

**A NaN propagates.** There is no `nan_policy` here: a NaN anywhere in either sample makes the
statistic, the p-value and `StatisticLocation` all `NaN`. `StatisticSign` becomes `0`, the
closest an `int` comes to carrying scipy's own `nan` there. Before this guard existed, a NaN
input span both samples and hung `Walk` forever instead: `sorted[index] == value` is `false` for
a NaN `value`, so neither sample's cursor ever advances.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`KsResult`](ksresult.md), [`Alternative`](alternative.md),
[`ExactMethod`](exactmethod.md), the [Python equivalence table](../../../equivalence.md).
