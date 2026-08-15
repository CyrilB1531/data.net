# Levenshtein.NormalizedDistance

Scales the distance into `[0, 1]` by dividing it by the length of the longer input.

<!-- docs-declaration -->

```csharp
public static double NormalizedDistance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two strings to compare; `element` says what counts as one
character, and it decides both the distance and the lengths it is divided by.

**Returns** — `double` in `[0, 1]`: `0` when the two are equal, `1` when nothing can be reused.
Two
empty inputs give `0` rather than a division by zero.

**Example** — three edits over the seven characters of the longer input.

```csharp
using DataNet.Text.Distances;

double d = Levenshtein.NormalizedDistance("kitten", "sitting");   // => 0.4285…
```

**Remarks** — this is the number to threshold on and the number to compare across pairs; the raw
`Distance` is neither. It matches `Levenshtein.normalized_distance` in rapidfuzz exactly.

The trap is the same divisor mismatch that catches people between measures: `max(len(a), len(b))`
here, `len(a) + len(b)` in `Indel.NormalizedDistance`. A cut-off of `0.3` is a much stricter
demand
here than there.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Levenshtein.Distance`, `Levenshtein.NormalizedSimilarity`,
`Indel.NormalizedDistance`, the [Python equivalence table](../../../equivalence.md).
