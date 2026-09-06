# ZeroMethod

How the Wilcoxon signed-rank test treats pairs whose difference is zero.

<!-- docs-declaration -->

```csharp
public enum ZeroMethod { Wilcox, Pratt, ZSplit }
```

**Members** — `Wilcox` discards the zero-difference pairs before ranking; scipy's `'wilcox'`, and
the default. `Pratt` ranks the zeros alongside everything else, then drops their ranks from the
sums; scipy's `'pratt'`. `ZSplit` ranks the zeros the same way and splits their rank sum evenly
between the two sums; scipy's `'zsplit'`.

**Example** — the same seven pairs, two of them unchanged, read three different ways.

```csharp
using Lodestar.Stats;

double[] before = [12.0, 9.0, 15.0, 11.0, 8.0, 14.0, 10.0, 13.0];
double[] after = [9.0, 6.0, 16.0, 11.0, 4.0, 15.0, 10.0, 17.0];

TestResult wilcox = Wilcoxon.Paired(before, after, ZeroMethod.Wilcox);
TestResult pratt = Wilcoxon.Paired(before, after, ZeroMethod.Pratt);
TestResult zsplit = Wilcoxon.Paired(before, after, ZeroMethod.ZSplit);

double wilcoxW = wilcox.Statistic;   // => 8.5
double prattW = pratt.Statistic;     // => 14.5
double zsplitW = zsplit.Statistic;   // => 16
```

**Remarks — three rules, three different statistics on the same data.** This is why
`zeroMethod` is part of the test's definition rather than a tuning knob: `Wilcox` ranks only the
five non-zero differences, so its statistic sits on a different scale entirely from `Pratt`'s and
`ZSplit`'s, which both rank all eight values and only differ in what happens to the zero group's
ranks afterwards — dropped for `Pratt`, split for `ZSplit`.

**`Pratt` and `ZSplit` can share a p-value even when their statistics differ.** Under the
permutation route both zero methods here land on — the sample is small and carries zeros, so
[`ExactMethod.Auto`](exactmethod.md) falls to the exhaustive test rather than the plain exact
table — the p-value is computed from the *raw* positive and negative rank sums, before `ZSplit`'s
even split is added; `Pratt` and `ZSplit` compute that raw sum identically; only `Wilcox`'s
p-value, built from a genuinely smaller ranked set, is free to land somewhere else. Measured
here: all three give `0.7188`, `0.8438`, `0.8438` — `Wilcox` reads differently, `Pratt` and
`ZSplit` agree past three digits on this route even though their statistics, `14.5` and `16`, do
not.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Wilcoxon.Paired`](wilcoxon-paired.md), [`Wilcoxon.OneSample`](wilcoxon-onesample.md),
the [Python equivalence table](../../../equivalence.md).
