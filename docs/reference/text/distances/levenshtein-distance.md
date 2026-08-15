# Levenshtein.Distance

Counts the fewest insertions, deletions and substitutions that turn one string into the other.

<!-- docs-declaration -->

```csharp
public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
public static int Distance<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b) where T : IEquatable<T>
```

**Parameters** — `a` and `b` are the two strings to compare; a `string` converts implicitly, so
nothing is allocated for them. `element` says what counts as one character:
`TextElement.Utf16Unit` by default, the native and fastest choice, or `TextElement.CodePoint` to
match Python outside the Basic Multilingual Plane.

**Returns** — `int`, the number of edits. Zero when the two are equal, and never negative.

**Example** — the textbook pair: two substitutions and one insertion.

```csharp
using DataNet.Text.Distances;

int d = Levenshtein.Distance("kitten", "sitting");   // => 3
```

**Remarks** — this is the ordinary answer to "how different are these two texts", and the right
tool for typing mistakes and mis-keyed names. To compare sets of words rather than characters,
`Jaccard` — in the `DataNet.Text.Similarity` namespace, not this one — is the better fit; to
weight
a common prefix, `JaroWinkler`.

The trap is that the result is not bounded. Three edits are enormous between two six-letter words
and negligible between two paragraphs, so a raw distance cannot be compared across pairs of
different lengths — `NormalizedSimilarity` is what you want for a score in `[0, 1]`.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Levenshtein.NormalizedSimilarity`, `Indel.Distance`,
`DamerauLevenshtein.Distance`,
the [Python equivalence table](../../../equivalence.md).
