# Fuzz.WRatio

Picks among the other scorers by inspecting the input.

<!-- docs-declaration -->

```csharp
public static double WRatio(string a, string b)
```

**Parameters** — `a` and `b` are the strings to compare.

**Returns** — `double` in `[0, 100]`, a weighted best among the scorers it judges applicable.

**Example** — the same typo `Ratio` scores, reached by a different route.

```csharp
using Lodestar.Fuzzy;

double score = Fuzz.WRatio("apple pie", "appel pie");  // => 88.88888888888889
```

**Remarks** — the answer to "which scorer" when the input is not known in advance. It compares the
lengths, tries the applicable scorers, and weights the partial ones down so that a fragment match
does not automatically beat a whole-string one.

The same value as [`Ratio`](fuzz-ratio.md) here, because on two strings of equal length there is
nothing for the partial scorers to add. On a pair where one side is much longer they diverge, and
that divergence is the point of the type.

It is rapidfuzz's `WRatio` and the weights are rapidfuzz's; the number is reproduced rather than
invented, which matters because the weights are not derived from anything — they are a choice that
library made.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Fuzz.Ratio`](fuzz-ratio.md), [`Fuzz.PartialRatio`](fuzz-partialratio.md),
[the matching index](../matching.md).
