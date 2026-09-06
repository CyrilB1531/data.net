# MultipleComparisons.BenjaminiYekutieli

The Benjamini-Yekutieli procedure, valid under any dependence.

<!-- docs-declaration -->

```csharp
public static double[] BenjaminiYekutieli(ReadOnlySpan<double> pValues)
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
double[] adjusted = MultipleComparisons.BenjaminiYekutieli(family);

double smallest = Math.Round(adjusted[0], 6);   // => 0.011417
double largest = Math.Round(adjusted[4], 6);    // => 0.0959
```

**Remarks** — [`BenjaminiHochberg`](multiplecomparisons-benjaminihochberg.md) assumes the tests
are independent or positively dependent; this drops that assumption and is valid under any
dependence structure at all, at the price of a harmonic-sum factor, `1 + 1/2 + ... + 1/n`, that
multiplies every adjustment. Its values are never smaller than Benjamini-Hochberg's on the same
family — here, `0.0959` against `0.042` on the largest raw p-value — which is the cost of not
needing to know how the tests relate to each other.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`MultipleComparisons.Bonferroni`](multiplecomparisons-bonferroni.md),
[`MultipleComparisons.BenjaminiHochberg`](multiplecomparisons-benjaminihochberg.md), the
[Python equivalence table](../../../equivalence.md).
