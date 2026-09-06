# ChiSquare.Contingency

Tests a contingency table for independence of its two factors.

<!-- docs-declaration -->

```csharp
public static Chi2ContingencyResult Contingency(double[][] table, Continuity continuity = Continuity.Applied)
```

**Parameters** — `table` is the observed counts, row-major and rectangular, at least two rows and
two columns. `continuity` says whether to apply Yates's correction; it is defined for 2×2 tables
only, so asking for it on any other shape changes nothing — the same rule
`scipy.stats.chi2_contingency` follows with `correction=True`.

**Returns** — `Chi2ContingencyResult`: the statistic, the p-value, the degrees of freedom
`(rows - 1) * (columns - 1)`, and the table independence would have produced.

**Exceptions** — `ArgumentException` when `table` is empty, ragged, holds a negative, infinite or
NaN count, or has a zero row or column total. Unlike every other family in this package, a NaN or
an infinite cell here is refused rather than propagated: a contingency table's cells are counts,
not measurements, and the expected-frequency table divides by their marginals — a table that
cannot produce a marginal has nothing for the test to run against. See the
[Python equivalence table](../../../equivalence.md)'s `nan_policy` row.

**Example** — a 2×2 preference table, with Yates's correction applied by default.

```csharp
using Lodestar.Stats;

double[][] table =
[
    [30.0, 20.0],
    [15.0, 35.0],
];

Chi2ContingencyResult result = ChiSquare.Contingency(table);

double statistic = Math.Round(result.Statistic, 4);   // => 7.9192
int dof = result.Dof;                                 // => 1
double expected00 = result.ExpectedFrequencies[0][0];  // => 22.5
```

**Remarks — Yates only ever touches a 2×2 table.** `Continuity.Applied` moves each cell half a
unit toward its expectation before squaring it, and only when the table is exactly two rows by
two columns; on any other shape `continuity` is accepted and does nothing, matching scipy rather
than throwing on a parameter that would otherwise be silently ignored. Passing
`Continuity.None` on this same table gives a larger statistic, `9.0909`, and a smaller p-value —
the correction always pulls the statistic down, which is why it is the more conservative default.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`ChiSquare.GoodnessOfFit`](chisquare-goodnessoffit.md),
[`FisherExact.Test`](fisherexact-test.md) for the exact alternative at any sample size,
[`Chi2ContingencyResult`](chi2contingencyresult.md), [`Continuity`](continuity.md), the
[Python equivalence table](../../../equivalence.md).
