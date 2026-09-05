# OneWayAnova.Test

Compares the means of two or more groups.

<!-- docs-declaration -->

```csharp
public static TestResult Test(double[][] groups)
```

**Parameters** — `groups` are the samples to compare, at least two, each holding at least one
value, and at least one holding more than one — `scipy.stats.f_oneway` takes its samples the same
way, one array per group, which `groups` is `params` for.

**Returns** — `TestResult`: the F statistic, and the upper-tail p-value.

**Exceptions** — `ArgumentException` when there are fewer than two groups, a group is empty, or
every group holds exactly one value.

**Example** — three shifts, fifteen measurements in all.

```csharp
using Lodestar.Stats;

double[] morning = [12.0, 14.0, 11.0, 13.0, 15.0];
double[] afternoon = [16.0, 15.0, 18.0, 17.0, 14.0];
double[] evening = [21.0, 19.0, 22.0, 20.0, 23.0];

TestResult result = OneWayAnova.Test(morning, afternoon, evening);

double f = Math.Round(result.Statistic, 4);   // => 32.6667
double p = Math.Round(result.PValue, 8);      // => 1.396E-05
```

**Remarks — a fully degenerate input answers `NaN`, not an exception.** Two groups that are each
internally constant, and constant at the *same* value, drive both the between- and the
within-group sums of squares to exactly zero:

```csharp
using Lodestar.Stats;

TestResult degenerate = OneWayAnova.Test([5.0, 5.0], [5.0, 5.0]);

bool isNaN = double.IsNaN(degenerate.Statistic);   // => True
```

Zero divided by zero has no value, and scipy's own `f_oneway` returns the same `NaN` on the same
input — propagating it is the honest answer, not a guard this package chose to skip. Compare
[`KruskalWallis.Test`](kruskalwallis-test.md), which *throws* on the analogous all-tied input: an
ANOVA on constants is a well-formed question with an undefined answer, where the rank-based
statistic's inputs there are provably meaningless rather than merely indeterminate.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`KruskalWallis.Test`](kruskalwallis-test.md) for the rank-based counterpart,
[`TTest.Independent`](ttest-independent.md), [`MultipleComparisons`](multiplecomparisons.md) for
correcting the many pairwise tests an ANOVA's rejection invites, the
[Python equivalence table](../../../equivalence.md).
