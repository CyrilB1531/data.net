# Fuzz.PartialTokenSortRatio

Sorted words, compared over the best-matching window.

<!-- docs-declaration -->

```csharp
public static double PartialTokenSortRatio(string a, string b)
```

**Parameters** — `a` and `b` are the strings to compare.

**Returns** — `double` in `[0, 100]`: the words are sorted, then
[`PartialRatio`](fuzz-partialratio.md) is applied.

**Example** — reordered words, scored as a fragment.

```csharp
using Lodestar.Fuzzy;

double score = Fuzz.PartialTokenSortRatio("new york mets", "mets new york");  // => 100
```

**Remarks** — both transformations at once: order stops counting **and** one side may be a
fragment of the other. That is two kinds of forgiveness compounded, so it scores high on pairs a
person would call unrelated — which is why it is worth choosing deliberately rather than reaching
for as a default.

When the input is not known in advance, [`WRatio`](fuzz-wratio.md) chooses among the scorers
instead of applying the most forgiving one unconditionally.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Fuzz.TokenSortRatio`](fuzz-tokensortratio.md),
[`Fuzz.PartialRatio`](fuzz-partialratio.md).
