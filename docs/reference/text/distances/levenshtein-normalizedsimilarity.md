# Levenshtein.NormalizedSimilarity

`1 - NormalizedDistance`: `1` when the two are identical, `0` when nothing survives.

<!-- docs-declaration -->

```csharp
public static double NormalizedSimilarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two strings to compare; `element` says what counts as one
character, exactly as it does for `NormalizedDistance`, which this is computed from.

**Returns** — `double` in `[0, 1]`, larger meaning more alike. Two empty inputs give `1`.

**Example** — the same three edits, read as a score.

```csharp
using Lodestar.Text.Distances;

double s = Levenshtein.NormalizedSimilarity("kitten", "sitting");   // => 0.5714…
```

**Remarks** — this is the member to reach for by default when you want one number saying how alike
two strings are and you have no particular reason to prefer another measure. It matches
`Levenshtein.normalized_similarity` in rapidfuzz exactly.

Two traps. Two empty inputs return `1`, a perfect match between two blanks — filter empties first
if
that is wrong for your data. And this is not `fuzz.ratio`: that is `Indel.NormalizedSimilarity`
times 100, and substituting this for it is the single most common porting mistake in this area.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Levenshtein.NormalizedDistance`, `Indel.NormalizedSimilarity`,
`DamerauLevenshtein.NormalizedSimilarity`, the [Python equivalence
table](../../../equivalence.md).
