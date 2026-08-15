# Indel.Distance

Counts the fewest insertions and deletions that turn one string into the other, with substitution
not allowed.

<!-- docs-declaration -->

```csharp
public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
public static int Distance<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b) where T : IEquatable<T>
```

**Parameters** — `a` and `b` are the two strings to compare. `element` says what counts as one
character: `TextElement.Utf16Unit` by default, or `TextElement.CodePoint` for Python's answer
outside the Basic Multilingual Plane. The second overload compares any two spans of an
`IEquatable<T>`.

**Returns** — `int`, the number of insertions and deletions. Zero when the two are equal, and
never
negative.

**Example** — five insertions and deletions where `Levenshtein`, allowed to substitute, needs only
three edits.

```csharp
using DataNet.Text.Distances;

int d = Indel.Distance("kitten", "sitting");   // => 5
```

**Remarks** — every substitution costs two here, one delete and one insert, so this is always at
least as large as `Levenshtein` and usually larger. That is the point: it is the measure that
weights a replaced character as heavily as a lost one, which is what makes it match how people
judge two versions of a longer text rather than two mis-keyed short names.

It is exactly `len(a) + len(b) - 2 × Lcs.SubsequenceLength(a, b)`, so `Lcs` is the thing to reach
for when you want the shared run itself rather than a score over it.

The trap is a naming one, and it is the most common confusion in this area: `fuzz.ratio` in
rapidfuzz is **this** measure normalized, not Levenshtein. Porting `fuzz.ratio(a, b)` to
`Levenshtein.NormalizedSimilarity(a, b) * 100` silently produces different numbers on almost every
pair. `Indel.NormalizedSimilarity(a, b) * 100` is the port.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Indel.NormalizedSimilarity`, `Lcs.SubsequenceLength`, `Levenshtein.Distance`,
the [Python equivalence table](../../../equivalence.md).
