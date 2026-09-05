# MultipleComparisons.BenjaminiHochberg

The Benjamini-Hochberg step-up procedure.

<!-- docs-declaration -->

```csharp
public static double[] BenjaminiHochberg(ReadOnlySpan<double> pValues)
```

**Parameters** — `pValues` is the family, at least one value, each in `[0, 1]`; the span is
read, never modified.

**Returns** — `double[]`: the adjusted p-values, in the input's own order.

**Exceptions** — `ArgumentException` when `pValues` is empty. `ArgumentOutOfRangeException` when
a value is `NaN` or outside `[0, 1]`.

**Example** — the same five p-values [`Bonferroni`](multiplecomparisons-bonferroni.md) adjusts.

```csharp
using Lodestar.Stats;

double[] family = [0.001, 0.008, 0.039, 0.041, 0.042];
double[] adjusted = MultipleComparisons.BenjaminiHochberg(family);

double smallest = adjusted[0];   // => 0.005
double largest = adjusted[4];    // => 0.042
```

**Remarks** — this controls the expected *proportion* of false positives among the results called
significant, the false discovery rate, rather than Bonferroni's chance of any false positive at
all — a weaker guarantee, and one that costs less: every value here is at most what
[`Bonferroni`](multiplecomparisons-bonferroni.md) gives the same family, `0.042` against
`0.21` on the largest raw p-value above. The procedure walks the sorted p-values from the largest
down, keeping a running minimum, which is what keeps the adjusted values monotone in the original
order — without it a smaller raw p-value could end up with a larger adjusted one, an ordering
inversion the caller would have no reason to expect.

This assumes the tests are independent or positively dependent.
[`BenjaminiYekutieli`](multiplecomparisons-benjaminiyekutieli.md) drops that assumption, at a
cost.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`MultipleComparisons.Bonferroni`](multiplecomparisons-bonferroni.md),
[`MultipleComparisons.BenjaminiYekutieli`](multiplecomparisons-benjaminiyekutieli.md), the
[Python equivalence table](../../../equivalence.md).
