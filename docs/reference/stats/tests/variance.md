# Variance

Whether an independent-samples t-test pools the two variances.

<!-- docs-declaration -->

```csharp
public enum Variance { Equal, Welch }
```

**Members** — `Equal` pools the two variances — Student's *t*; scipy's `equal_var=True`. `Welch`
does not pool, using the Welch-Satterthwaite degrees of freedom instead; scipy's
`equal_var=False`.

**Example** — the same two samples, tested both ways.

```csharp
using Lodestar.Stats;

double[] before = [102.0, 98.0, 110.0, 105.0, 99.0];
double[] after = [95.0, 92.0, 99.0, 91.0, 97.0];

TTestResult welch = TTest.Independent(before, after, Alternative.TwoSided, Variance.Welch);
TTestResult student = TTest.Independent(before, after, Alternative.TwoSided, Variance.Equal);

double welchDf = Math.Round(welch.Df, 4);     // => 7.0904
double studentDf = student.Df;                // => 8
```

**Remarks — this package's one deliberate divergence from scipy.**
[`TTest.Independent`](ttest-independent.md) defaults to `Welch`; `scipy.stats.ttest_ind` defaults
to `Equal` (`equal_var=True`). Pooling is only correct when the two populations really do share a
variance, which is an assumption a caller rarely has grounds to make going in — Welch is the
safer choice when it is wrong and loses nothing when it happens to be right, so the safer default
costs one word at the call site here rather than a wrong answer by default. `Equal`'s degrees of
freedom are always `n + m - 2`, a whole number; `Welch`'s Satterthwaite denominator is not a
count of anything, which is why `TTestResult.Df` is a `double` rather than an `int` — `7.0904`
above is not a rounding artefact.

On these two samples the *t* statistic itself does not move — both give `3.028` — because the
two sample sizes are equal; pooling and not pooling only disagree on the standard error, and
hence the degrees of freedom, once the samples are unequal in size or spread.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TTest.Independent`](ttest-independent.md), [`TTestResult`](ttestresult.md), the
[Python equivalence table](../../../equivalence.md).
