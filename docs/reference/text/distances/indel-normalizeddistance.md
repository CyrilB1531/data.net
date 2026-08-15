# Indel.NormalizedDistance

Scales the distance into `[0, 1]` by dividing it by the sum of the two lengths.

<!-- docs-declaration -->

```csharp
public static double NormalizedDistance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two strings to compare; `element` says what counts as one
character, and it moves both the distance and the lengths it is divided by.

**Returns** — `double` in `[0, 1]`: `0` when the two are equal, `1` when they share nothing at
all.
Two empty inputs give `0`.

**Example** — five edits over thirteen characters of input.

```csharp
using DataNet.Text.Distances;

double d = Indel.NormalizedDistance("kitten", "sitting");   // => 0.3846…
```

**Remarks** — the divisor here is `len(a) + len(b)`, not the `max(len(a), len(b))` that
`Levenshtein`, `Osa` and `DamerauLevenshtein` all use. That is not an inconsistency to work
around;
it is what keeps the result in `[0, 1]` for a measure whose raw distance can reach the sum of both
lengths rather than the longer of them.

The trap follows from that: a threshold carried over from `Levenshtein.NormalizedDistance` will be
too lenient here, because the same pair of inputs scores lower on this scale. Tune the number
against the measure you are actually calling.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Indel.Distance`, `Indel.NormalizedSimilarity`, `Levenshtein.NormalizedDistance`,
the [Python equivalence table](../../../equivalence.md).
