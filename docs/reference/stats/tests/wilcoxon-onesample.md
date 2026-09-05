# Wilcoxon.OneSample

Compares a sample of differences against a median of zero.

<!-- docs-declaration -->

```csharp
public static TestResult OneSample(ReadOnlySpan<double> differences, ZeroMethod zeroMethod = ZeroMethod.Wilcox, Alternative alternative = Alternative.TwoSided, Continuity continuity = Continuity.None, ExactMethod method = ExactMethod.Auto)
```

**Parameters** — `differences` is the sample, at least one value; the span is read, never
modified. `zeroMethod` says what to do with differences that are exactly zero. `alternative` says
which tail the p-value covers. `continuity` says whether the normal approximation gets the
half-unit correction. `method` chooses the exact null distribution, the exhaustive permutation
test, its normal approximation, or a choice between them by the number of non-zero differences.

**Returns** — `TestResult`: the smaller of the two signed-rank sums, and the p-value.

**Exceptions** — `ArgumentException` when `differences` is empty. `ArgumentOutOfRangeException`
when `method` is `ExactMethod.Exact` and the zero-method-processed sample exceeds 500 values.

**Example** — seven differences, two of them exactly zero.

```csharp
using Lodestar.Stats;

double[] differences = [2.0, 0.0, 3.0, 0.0, 2.0, 3.0, 3.0];

TestResult result = Wilcoxon.OneSample(differences);

double w = result.Statistic;   // => 0
double p = result.PValue;      // => 0.0625
```

**Remarks — every difference tied at zero is a defined answer, not an error.** When
`zeroMethod` leaves nothing to rank — `ZeroMethod.Wilcox` drops every value — there is no evidence
either way, and this returns a statistic of `0.0` and a p-value of `1.0` rather than throwing;
scipy answers the same way on the same input.

**The exact route has a size bound this package added.** scipy's own signed-rank table is exact
for any `n`, but its total, `2^n`, overflows a `double` to `+Infinity` past `n = 1023`, and every
p-value it then divides would silently come out as exactly `0.0`. `ExactMethod.Exact` above 500
ranked values is refused with `ArgumentOutOfRangeException` instead, comfortably inside that
margin; `ExactMethod.Auto` never reaches the bound on its own, since it turns unconditionally
asymptotic above 50 values.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Wilcoxon.Paired`](wilcoxon-paired.md), [`TTest.OneSample`](ttest-onesample.md)
for the parametric counterpart, [`ZeroMethod`](zeromethod.md), [`ExactMethod`](exactmethod.md),
the [Python equivalence table](../../../equivalence.md).
