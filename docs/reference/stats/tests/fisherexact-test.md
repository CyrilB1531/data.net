# FisherExact.Test

Tests a 2x2 table for association.

<!-- docs-declaration -->

```csharp
public static TestResult Test(int[][] table, Alternative alternative = Alternative.TwoSided)
```

**Parameters** — `table` is the counts, as two rows of two, every count non-negative.
`alternative` says which tail the p-value covers.

**Returns** — `TestResult`: the conditional odds ratio — `PositiveInfinity` when the second
diagonal is zero, `NaN` when both diagonals are — and the p-value.

**Exceptions** — `ArgumentException` when `table` is not 2x2. `ArgumentOutOfRangeException` when
a count is negative, or the table's total exceeds 1,000,000.

**Example** — Fisher's own tea-tasting table: four cups poured each way, and the taster placed
three of each correctly.

```csharp
using Lodestar.Stats;

int[][] table = [[3, 1], [1, 3]];

TestResult result = FisherExact.Test(table);

double oddsRatio = result.Statistic;               // => 9
double p = Math.Round(result.PValue, 6);           // => 0.485714
```

**Remarks** — the one-sided p-value on the same table is smaller,
`Math.Round(FisherExact.Test(table, Alternative.Greater).PValue, 6)` giving `0.242857`: the
two-sided p-value sums every table at least as extreme in *either* direction, so it is never
smaller than the one-sided sum on its own tail.

**The exact enumeration has a cost proportional to the table's total.** Every table sharing the
observed margins is walked, so a table summing past 1,000,000 is refused with
`ArgumentOutOfRangeException` rather than run for however long that takes.
[`ChiSquare.Contingency`](chisquare-contingency.md) is the asymptotic alternative at that scale —
right at large samples, where this test's exactness stops mattering and its cost starts.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`ChiSquare.Contingency`](chisquare-contingency.md), [`Alternative`](alternative.md),
the [Python equivalence table](../../../equivalence.md).
