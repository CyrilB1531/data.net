# Osa.NormalizedDistance

Scales the distance into `[0, 1]` by dividing it by the length of the longer input.

<!-- docs-declaration -->

```csharp
public static double NormalizedDistance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two strings to compare; `element` says what counts as one
character, and it moves both the distance and the lengths it is divided by.

**Returns** — `double` in `[0, 1]`: `0` when the two are equal, `1` when nothing can be reused.
Two
empty inputs give `0`.

**Example** — one swap over four characters.

```csharp
using Lodestar.Text.Distances;

double d = Osa.NormalizedDistance("abcd", "acbd");   // => 0.25
```

**Remarks** — the divisor is `max(len(a), len(b))`, the same as `Levenshtein` and
`DamerauLevenshtein` use, so thresholds move freely between those three. `Indel` is the one that
does not share the scale.

The trap is a consequence of the swap being cheap: this scores a pair with several transpositions
much closer to `0` than `Levenshtein.NormalizedDistance` does on the same pair, so a threshold
carried over from Levenshtein will let more through than you expect. That is the intended
behaviour
and worth knowing about anyway.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Osa.Distance`, `Osa.NormalizedSimilarity`,
`DamerauLevenshtein.NormalizedDistance`, the [Python equivalence table](../../../equivalence.md).
