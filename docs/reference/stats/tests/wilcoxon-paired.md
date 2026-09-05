# Wilcoxon.Paired

Compares two paired samples by the ranks of their differences.

<!-- docs-declaration -->

```csharp
public static TestResult Paired(ReadOnlySpan<double> x, ReadOnlySpan<double> y, ZeroMethod zeroMethod = ZeroMethod.Wilcox, Alternative alternative = Alternative.TwoSided, Continuity continuity = Continuity.None, ExactMethod method = ExactMethod.Auto)
```

**Parameters** — `x` is the first measurement of each pair. `y` is the second measurement of
each pair, in the same order as `x`. `zeroMethod` says what to do with pairs whose difference is
zero. `alternative` says which tail the p-value covers. `continuity` says whether the normal
approximation gets the half-unit correction. `method` chooses the exact null distribution, the
exhaustive permutation test, its normal approximation, or a choice between them.

**Returns** — `TestResult`: the smaller of the two signed-rank sums, and the p-value.

**Exceptions** — `ArgumentException` when the two samples differ in length, or are empty.
`ArgumentOutOfRangeException` when `method` is `ExactMethod.Exact` and the ranked sample exceeds
500 values.

**Example** — seven pairs, two of them unchanged: what the three zero methods disagree about.

```csharp
using Lodestar.Stats;

double[] before = [12.0, 9.0, 15.0, 11.0, 8.0, 14.0, 10.0, 13.0];
double[] after = [9.0, 6.0, 16.0, 11.0, 4.0, 15.0, 10.0, 17.0];

TestResult wilcox = Wilcoxon.Paired(before, after);
TestResult pratt = Wilcoxon.Paired(before, after, ZeroMethod.Pratt);
TestResult zsplit = Wilcoxon.Paired(before, after, ZeroMethod.ZSplit);

double wilcoxW = wilcox.Statistic;   // => 8.5
double prattW = pratt.Statistic;     // => 14.5
double zsplitW = zsplit.Statistic;   // => 16
```

**Remarks** — this delegates to [`OneSample`](wilcoxon-onesample.md) on the pairwise differences
`x[i] - y[i]`; `Paired(x, y, ...)` and `OneSample(differences, ...)` agree exactly wherever the
differences agree, the same relationship `TTest.Paired` has to `TTest.OneSample`.

Two of the seven pairs above are unchanged, and the three zero methods read that differently:
`Wilcox` drops both pairs before ranking the rest; `Pratt` ranks them alongside everything else
but excludes their ranks from the sums that follow; `ZSplit` ranks them the same way and *keeps*
them, splitting their rank sum evenly between the positive and negative totals. All three land on
different statistics here — the [`ZeroMethod`](zeromethod.md) page has the full three-way
comparison, including the p-values.

`method` here refuses past a lower bound than [`MannWhitney.Test`](mannwhitney-test.md)'s:
`ExactMethod.Exact` is only honoured up to 500 ranked values, because the exact null
distribution's total, `2^n`, overflows a `double` to infinity past `n = 1023` and every p-value
it divides would silently become exactly zero rather than throwing. `ExactMethod.Auto` never
reaches that bound on its own — it is unconditionally asymptotic above 50 values, exact only
below that and free of both ties and zeros, and falls to an exhaustive permutation test at 13
values or fewer when ties or zeros rule out the plain exact table.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Wilcoxon.OneSample`](wilcoxon-onesample.md), [`TTest.Paired`](ttest-paired.md)
for the parametric counterpart, [`ZeroMethod`](zeromethod.md),
[`MannWhitney.Test`](mannwhitney-test.md), the [Python equivalence table](../../../equivalence.md).
