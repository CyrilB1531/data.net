# TTest.Paired

The paired *t*-test: a one-sample test on the differences.

<!-- docs-declaration -->

```csharp
public static TTestResult Paired(ReadOnlySpan<double> a, ReadOnlySpan<double> b, Alternative alternative = Alternative.TwoSided)
```

**Parameters** — `a` is the first measurement of each pair. `b` is the second measurement of each
pair, in the same order as `a`. `alternative` says which tail the p-value covers.

**Returns** — `TTestResult`: the t statistic of the differences `a[i] - b[i]`, its p-value, and
the degrees of freedom, `a.Length - 1`.

**Exceptions** — `ArgumentException` when the two samples differ in length, or hold fewer than
two pairs.

**Example** — the same seven machines, measured before and after a configuration change.

```csharp
using Lodestar.Stats;

double[] before = [102.0, 98.0, 110.0, 105.0, 99.0, 101.0, 108.0];
double[] after = [99.0, 96.0, 104.0, 103.0, 95.0, 99.0, 102.0];

TTestResult result = TTest.Paired(before, after);

double t = Math.Round(result.Statistic, 4);   // => 5.2129
double df = result.Df;                        // => 6
```

**Remarks** — this is `TTest.OneSample` on the pairwise differences against a population mean of
`0.0`: `Paired(a, b, alternative)` and `OneSample(differences, 0.0, alternative)` agree exactly,
which is also how `scipy.stats.ttest_rel` is defined against `scipy.stats.ttest_1samp`. There is
no `Variance` parameter here — pooling only means something when two *separate* samples each
carry their own variance, and a paired test has one sample of differences.

**Order matters, sign included.** `Paired(a, b)` and `Paired(b, a)` report the same magnitude and
the opposite sign, so a one-sided `alternative` answers a different question depending on which
argument is `a`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TTest.Independent`](ttest-independent.md), [`TTest.OneSample`](ttest-onesample.md),
[`Wilcoxon.Paired`](wilcoxon-paired.md) for the rank-based counterpart, the
[Python equivalence table](../../../equivalence.md).
