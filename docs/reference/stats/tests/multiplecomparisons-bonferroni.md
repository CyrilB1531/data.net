# MultipleComparisons.Bonferroni

Multiplies each p-value by the family size, clamped at one.

<!-- docs-declaration -->

```csharp
public static double[] Bonferroni(ReadOnlySpan<double> pValues)
```

**Parameters** — `pValues` is the family, at least one value, each in `[0, 1]`; the span is
read, never modified.

**Returns** — `double[]`: the adjusted p-values, in the input's own order.

**Exceptions** — `ArgumentException` when `pValues` is empty. `ArgumentOutOfRangeException` when
a value is `NaN` or outside `[0, 1]`.

**Example** — five p-values from five tests run together.

```csharp
using Lodestar.Stats;

double[] family = [0.001, 0.008, 0.039, 0.041, 0.042];
double[] adjusted = MultipleComparisons.Bonferroni(family);

double smallest = Math.Round(adjusted[0], 3);   // => 0.005
double largest = Math.Round(adjusted[4], 3);    // => 0.21
```

**Remarks** — this is the correction most people mean by "multiple comparisons": each raw p-value
is multiplied by the family size and clamped at `1.0`, which controls the chance of *any* false
positive across the whole family, whatever the tests' dependence. It is also the most
conservative of the three — every value here comes out larger than
[`BenjaminiHochberg`](multiplecomparisons-benjaminihochberg.md)'s adjustment of the same family,
because Bonferroni is not trying to control the same quantity. A family of twenty tests at the
raw 5 % level effectively demands 0.25 % from each one here, which is why it is the right choice
when even one false positive is costly and the wrong one when it would only bury real findings
under an unreachable bar.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`MultipleComparisons.BenjaminiHochberg`](multiplecomparisons-benjaminihochberg.md),
[`MultipleComparisons.BenjaminiYekutieli`](multiplecomparisons-benjaminiyekutieli.md), the
[Python equivalence table](../../../equivalence.md).
