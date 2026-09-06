# Continuity

Whether a discrete statistic's normal approximation gets the half-unit correction.

<!-- docs-declaration -->

```csharp
public enum Continuity { Applied, None }
```

**Members** — `Applied` shifts the statistic half a unit toward the mean before the normal tail
is read. `None` takes the statistic as it stands.

**Example** — the same asymptotic Mann-Whitney p-value, with and without the correction.

```csharp
using Lodestar.Stats;

double[] control = [7.0, 3.0, 6.0, 2.0, 8.0, 5.0];
double[] treated = [9.0, 12.0, 8.0, 11.0, 15.0, 10.0];

TestResult corrected = MannWhitney.Test(
    control, treated, Alternative.TwoSided, Continuity.Applied, ExactMethod.Asymptotic);
TestResult uncorrected = MannWhitney.Test(
    control, treated, Alternative.TwoSided, Continuity.None, ExactMethod.Asymptotic);

double correctedP = Math.Round(corrected.PValue, 6);     // => 0.006392
double uncorrectedP = Math.Round(uncorrected.PValue, 6); // => 0.004998
```

**Remarks — one idea, three spellings in scipy.** `use_continuity` on `mannwhitneyu` defaults to
`true`, `correction` on `wilcoxon` defaults to `false`, and `correction` on `chi2_contingency`
defaults to `true` and is applied to 2×2 tables only. The three defaults disagree with each
other, which is exactly why this is a named argument here rather than a `bool` nobody reads —
[`MannWhitney.Test`](mannwhitney-test.md) defaults to `Applied`, matching scipy's own default
there, and [`Wilcoxon.Paired`](wilcoxon-paired.md) and
[`Wilcoxon.OneSample`](wilcoxon-onesample.md) default to `None`, matching scipy's default there
too.

The correction only ever touches a normal-approximation p-value; the exact and permutation
routes have no continuous approximation to correct, so `continuity` is silently unused on them —
[`MannWhitney.Test`](mannwhitney-test.md)'s own parameter documentation says as much.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`MannWhitney.Test`](mannwhitney-test.md), [`Wilcoxon.Paired`](wilcoxon-paired.md),
[`ChiSquare.Contingency`](chisquare-contingency.md), the
[Python equivalence table](../../../equivalence.md).
