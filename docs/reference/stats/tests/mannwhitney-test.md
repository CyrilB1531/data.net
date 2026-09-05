# MannWhitney.Test

Compares two independent samples by their ranks.

<!-- docs-declaration -->

```csharp
public static TestResult Test(ReadOnlySpan<double> x, ReadOnlySpan<double> y, Alternative alternative = Alternative.TwoSided, Continuity continuity = Continuity.Applied, ExactMethod method = ExactMethod.Auto)
```

**Parameters** — `x` and `y` are the two samples, each at least one value; both spans are read,
never modified. `alternative` says which tail the p-value covers. `continuity` says whether the
normal approximation gets the half-unit correction; it is ignored on the exact branch, where
there is nothing to approximate. `method` chooses the exact null distribution, its normal
approximation, or a choice between them by sample size and ties.

**Returns** — `TestResult`: *U* for `x`, and the p-value.

**Exceptions** — `ArgumentException` when either sample is empty. `ArgumentOutOfRangeException`
when `method` is `ExactMethod.Exact` and `x.Length * y.Length` exceeds 20,000.

**Example** — a control group and a treated group, one value tied across them.

```csharp
using Lodestar.Stats;

double[] control = [7.0, 3.0, 6.0, 2.0, 8.0, 5.0];
double[] treated = [9.0, 12.0, 8.0, 11.0, 15.0, 10.0];

TestResult result = MannWhitney.Test(control, treated);

double u = result.Statistic;                    // => 0.5
double p = Math.Round(result.PValue, 6);         // => 0.006392
```

**Remarks** — `control` and `treated` share the value `8`, so `Ranks.HasTies` is true and
`ExactMethod.Auto` falls straight to the normal approximation — untied, both samples here are
small enough (six values each, at or under the eight-value bound `Auto` checks) that it would
have taken the exact route instead. Asking for `ExactMethod.Exact` explicitly still answers, just
not the same number: on this data it gives `0.004329` rather than `0.006392`, because scipy
computes an exact p-value on tied data too instead of refusing, and this package matches that
rather than raising on a case scipy accepts.

**A NaN propagates.** There is no `nan_policy` here: a NaN anywhere in either sample makes the
statistic and the p-value `NaN`, checked before `Ranks.Average` ever runs — unguarded, `Array.Sort`
sorts a NaN to the front and it would take a finite rank like any other value.

**The exact route has a size bound `Auto` cannot cross.** `x.Length * y.Length` above 20,000 costs
tens of seconds to enumerate — the table is `(m + 1) × (n·m + 1)` and grows with the square of
that product. Passing `ExactMethod.Exact` past the bound throws; `ExactMethod.Auto` never does,
falling back to the asymptotic answer instead, because nothing the caller wrote asked for an
exact result. The bound on the *smaller* sample alone is eight, not both: `x.Length = 8,
y.Length = 10_000` still qualifies for `Auto`'s exact route by that rule and would build a
multi-gigabyte table if the product bound did not also apply.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TTest.Independent`](ttest-independent.md) for the parametric counterpart,
[`Wilcoxon.Paired`](wilcoxon-paired.md) for paired measurements,
[`ExactMethod`](exactmethod.md), the [Python equivalence table](../../../equivalence.md).
