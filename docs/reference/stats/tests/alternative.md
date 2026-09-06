# Alternative

Which tail of the null distribution a test's p-value covers.

<!-- docs-declaration -->

```csharp
public enum Alternative { TwoSided, Less, Greater }
```

**Members** — `TwoSided` asks whether the samples differ, in either direction; scipy's
`'two-sided'`. `Less` asks whether the first sample's distribution is shifted below the second's;
scipy's `'less'`. `Greater` asks whether it is shifted above; scipy's `'greater'`.

**Example** — the same test, on the same data, asked three different questions.

```csharp
using Lodestar.Stats;

double[] sample = [12.1, 9.4, 15.0, 11.2, 8.8, 13.9, 10.5];

TTestResult twoSided = TTest.OneSample(sample, 10.0, Alternative.TwoSided);
TTestResult greater = TTest.OneSample(sample, 10.0, Alternative.Greater);
TTestResult less = TTest.OneSample(sample, 10.0, Alternative.Less);

double twoSidedP = Math.Round(twoSided.PValue, 4);   // => 0.1205
double greaterP = Math.Round(greater.PValue, 4);     // => 0.0603
double lessP = Math.Round(less.PValue, 4);           // => 0.9397
```

**Remarks — a one-sided p-value is not half of the two-sided one, except by coincidence.** The
statistic here is positive, so `Greater` reads the tail the sample actually leans toward, and its
`0.0603` is very nearly `twoSidedP / 2`; `Less` reads the opposite tail and its `0.9397` is not
`1 - greaterP` by accident but because the *t* distribution is symmetric around zero. Neither
relationship survives on an asymmetric or discrete null distribution — the exact routes of
[`MannWhitney.Test`](mannwhitney-test.md) and [`Wilcoxon.Paired`](wilcoxon-paired.md) round each
tail separately rather than deriving one from the other, which is why `Alternative` is a genuine
argument to every test in this package rather than a display option layered on top of one
two-sided computation.

scipy spells this `alternative` and defaults it to `'two-sided'` everywhere this package does too.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TTest.OneSample`](ttest-onesample.md), [`MannWhitney.Test`](mannwhitney-test.md),
[`KolmogorovSmirnov.TwoSample`](kolmogorovsmirnov-twosample.md), the
[Python equivalence table](../../../equivalence.md).
