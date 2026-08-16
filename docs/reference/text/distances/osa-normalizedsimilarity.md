# Osa.NormalizedSimilarity

`1 - NormalizedDistance`: `1` when the two are identical, `0` when nothing survives.

<!-- docs-declaration -->

```csharp
public static double NormalizedSimilarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two strings to compare; `element` says what counts as one
character, exactly as it does for `NormalizedDistance`, which this is computed from.

**Returns** — `double` in `[0, 1]`, larger meaning more alike. Two empty inputs give `1`.

**Example** — the same swap, read as a score.

```csharp
using Lodestar.Text.Distances;

double s = Osa.NormalizedSimilarity("abcd", "acbd");   // => 0.75
```

**Remarks** — the member to rank on when transpositions should be forgiven and a bigger number
should mean a better match. It matches `OSA.normalized_similarity` in rapidfuzz.

Two traps, both inherited. Two empty inputs return `1`. And short inputs make the scale coarse:
with `max(len(a), len(b))` as the divisor, a four-character pair can only ever score `0`, `0.25`,
`0.5`, `0.75` or `1`, so a threshold like `0.8` is really a threshold of `1` for anything that
short.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Osa.NormalizedDistance`, `DamerauLevenshtein.NormalizedSimilarity`,
`Levenshtein.NormalizedSimilarity`, the [Python equivalence table](../../../equivalence.md).
