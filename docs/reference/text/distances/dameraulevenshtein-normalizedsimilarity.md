# DamerauLevenshtein.NormalizedSimilarity

`1 - NormalizedDistance`: `1` when the two are identical, `0` when nothing is shared.

<!-- docs-declaration -->

```csharp
public static double NormalizedSimilarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two strings to compare; `element` says what counts as one
character, exactly as it does for `NormalizedDistance`, which this is computed from.

**Returns** — `double` in `[0, 1]`, larger meaning more alike.

**Example** — the same pair as above, read the other way round.

```csharp
using Lodestar.Text.Distances;

double s = DamerauLevenshtein.NormalizedSimilarity("MARTHA", "MARHTA");   // => 0.8333…
```

**Remarks** — use this rather than `NormalizedDistance` wherever a bigger number ought to mean a
better match: ranking candidates, sorting descending, or handing a score to something that expects
one. Nothing else about the two differs.

The trap is the empty case. `NormalizedDistance("", "")` is `0`, so this returns `1` — two empty
strings are reported as a perfect match. If an empty string means "this field was never filled
in",
that is exactly backwards, and the filtering has to happen before the call.

**Applies to** — net10.0, netstandard2.0.

**See also** — `DamerauLevenshtein.NormalizedDistance`, `Osa.NormalizedSimilarity`,
`Levenshtein.NormalizedSimilarity`, the [Python equivalence table](../../../equivalence.md).
