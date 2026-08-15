# DamerauLevenshtein.NormalizedDistance

Scales the distance into `[0, 1]` by dividing it by the length of the longer input.

<!-- docs-declaration -->

```csharp
public static double NormalizedDistance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two strings to compare. `element` says what counts as one
character, and it also decides the lengths the result is divided by, so it moves the denominator
as
well as the distance.

**Returns** — `double` in `[0, 1]`: `0` when the two are equal, `1` when nothing at all can be
reused. Two empty inputs give `0` rather than a division by zero.

**Example** — one swap over six characters.

```csharp
using DataNet.Text.Distances;

double d = DamerauLevenshtein.NormalizedDistance("MARTHA", "MARHTA");   // => 0.1666…
```

**Remarks** — this is the form to threshold on ("reject anything above 0.2") and the only form
worth comparing across pairs of different lengths. The divisor is `max(len(a), len(b))`.

The trap is that "normalized" does not mean "interchangeable". `Indel.NormalizedDistance` divides
by the **sum** of the two lengths instead, so the same pair scores differently on the two scales
and
a threshold tuned against one is meaningless against the other. Check the divisor before you move
a
threshold between measures.

**Applies to** — net10.0, netstandard2.0.

**See also** — `DamerauLevenshtein.Distance`, `DamerauLevenshtein.NormalizedSimilarity`,
`Levenshtein.NormalizedDistance`, the [Python equivalence table](../../../equivalence.md).
