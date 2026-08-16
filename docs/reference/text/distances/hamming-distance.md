# Hamming.Distance

Counts the positions at which the two differ, then adds the difference in their lengths.

<!-- docs-declaration -->

```csharp
public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
public static int Distance<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b) where T : IEquatable<T>
```

**Parameters** — `a` and `b` are the two strings to compare, and they need not be the same length.
`element` says what counts as one position: `TextElement.Utf16Unit` by default, or
`TextElement.CodePoint` to count an emoji once instead of twice. The second overload compares any
two spans of an `IEquatable<T>`.

**Returns** — `int`, never negative, and `0` only when the two are equal.

**Example** — nothing can differ position by position here, so only the two missing characters
count.

```csharp
using Lodestar.Text.Distances;

int d = Hamming.Distance("a", "abc");   // => 2
```

**Remarks** — this is the right measure for things that are aligned by construction: fixed-width
identifiers, ISBNs, hashes, DNA reads, two readings of the same fixed-length field. It is also by
far the cheapest thing on this page, a single pass with no matrix behind it.

It is the wrong measure the moment anything can shift. Inserting one character at the front of a
string makes every later position disagree, so `Hamming.Distance("abcdef", "xabcdef")` is 7 where
`Levenshtein` says 1. If insertions are possible at all, you want `Levenshtein` or `Indel`.

The textbook definition is undefined for inputs of different lengths; this one is not — it charges
the length difference and carries on, so a wrong-length input returns a number instead of
throwing,
and a length bug will read as a large distance rather than as an error. Against combining marks
and
mixed scripts the result also deliberately differs from `jellyfish.hamming_distance`, which
diverges from the standard definition there; the measurements are in
[decision 0005](../../../decisions/0005-hamming-jellyfish-divergence.md).

**Applies to** — net10.0, netstandard2.0.

**See also** — `Hamming.NormalizedSimilarity`, `Levenshtein.Distance`, `Indel.Distance`,
the [Python equivalence table](../../../equivalence.md).
