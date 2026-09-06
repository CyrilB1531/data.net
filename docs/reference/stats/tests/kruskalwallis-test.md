# KruskalWallis.Test

Compares two or more groups by their ranks in the pooled sample.

<!-- docs-declaration -->

```csharp
public static TestResult Test(double[][] groups)
```

**Parameters** — `groups` are the samples to compare, at least two, each holding at least one
value — `scipy.stats.kruskal` takes its samples the same way, one array per group, which
`groups` is `params` for.

**Returns** — `TestResult`: the H statistic, and the upper-tail p-value.

**Exceptions** — `ArgumentException` when there are fewer than two groups, a group is empty, or
every value in the pooled sample is tied.

**Example** — the same three shifts [`OneWayAnova.Test`](onewayanova-test.md) compares.

```csharp
using Lodestar.Stats;

double[] morning = [12.0, 14.0, 11.0, 13.0, 15.0];
double[] afternoon = [16.0, 15.0, 18.0, 17.0, 14.0];
double[] evening = [21.0, 19.0, 22.0, 20.0, 23.0];

TestResult result = KruskalWallis.Test(morning, afternoon, evening);

double h = Math.Round(result.Statistic, 4);   // => 11.6215
double p = Math.Round(result.PValue, 6);      // => 0.002995
```

**Remarks — a fully tied pooled sample throws, where [`OneWayAnova.Test`](onewayanova-test.md)'s
analogous input answers `NaN`.**

```csharp
using Lodestar.Stats;

string message = "nothing was thrown";
try
{
    KruskalWallis.Test([5.0, 5.0], [5.0, 5.0]);
}
catch (ArgumentException error)
{
    message = error.Message;
}

string what = message;   // => Every value in the pooled sample is tied…
```

The tie correction this statistic divides by is `1 - (t³ - t) / (n³ - n)` for a tie group
spanning `t` of the `n` pooled values; when every value is tied, `t = n` and the correction is
exactly `0` — not close to zero, not a value a tolerance would need to catch — so the division
that would follow is refused instead of silently producing an infinite or NaN statistic from
ranks that carry no information at all.

**A NaN propagates.** There is no `nan_policy` here: a NaN anywhere in any group makes the
statistic and the p-value `NaN`, checked before ranking — unguarded, `Array.Sort` sorts a NaN to
the front and it would take a finite rank like any other value, the same failure mode
[`MannWhitney.Test`](mannwhitney-test.md) shares and guards against the same way.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`OneWayAnova.Test`](onewayanova-test.md) for the parametric counterpart,
[`MannWhitney.Test`](mannwhitney-test.md), the [Python equivalence table](../../../equivalence.md).
