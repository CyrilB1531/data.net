# ChiSquare.GoodnessOfFit

Tests observed counts against an expected distribution.

<!-- docs-declaration -->

```csharp
public static TestResult GoodnessOfFit(ReadOnlySpan<double> observed, ReadOnlySpan<double> expected = default)
```

**Parameters** — `observed` are the observed counts, at least two categories; the span is read,
never modified. `expected` are the expected counts, which must sum to the observed total; omit
them for a uniform expectation across every category, which is what `scipy.stats.chisquare` does
with `f_exp=None`.

**Returns** — `TestResult`: the statistic, and the upper-tail p-value.

**Exceptions** — `ArgumentException` when there are fewer than two categories, `observed` and
`expected` differ in length, an expectation is not positive, or the expectations do not sum to
the observations.

**Example** — six faces of a die, rolled 88 times.

```csharp
using Lodestar.Stats;

double[] rolls = [16.0, 18.0, 16.0, 14.0, 12.0, 12.0];

TestResult result = ChiSquare.GoodnessOfFit(rolls);

double statistic = result.Statistic;               // => 2
double p = Math.Round(result.PValue, 6);            // => 0.849145
```

**Remarks** — a p-value this large says the rolls are entirely consistent with a fair die; the
uniform expectation here is `88 / 6` in every category, since `expected` was omitted. Passing an
explicit `expected` answers a different question — not "is this uniform?" but "does this match
*this* distribution?" — and it must sum to within `1e-8` of the observed total, relative to that
total, or the p-value would be comparing tables of different sizes.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`ChiSquare.Contingency`](chisquare-contingency.md), [`FisherExact.Test`](fisherexact-test.md)
for a 2×2 table at any sample size, the [Python equivalence table](../../../equivalence.md).
