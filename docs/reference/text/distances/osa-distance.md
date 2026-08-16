# Osa.Distance

Counts the fewest insertions, deletions, substitutions and swaps of neighbouring characters, with
no character allowed to take part in more than one edit.

<!-- docs-declaration -->

```csharp
public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
public static int Distance<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b) where T : IEquatable<T>
```

**Parameters** — `a` and `b` are the two strings to compare. `element` says what counts as one
character: `TextElement.Utf16Unit` by default, or `TextElement.CodePoint` for rapidfuzz's answer
outside the Basic Multilingual Plane. The second overload compares any two spans of an
`IEquatable<T>`.

**Returns** — `int`, the number of edits. Zero when the two are equal, and never negative.

**Example** — the pair that separates OSA from full Damerau-Levenshtein, which answers 2.

```csharp
using Lodestar.Text.Distances;

int d = Osa.Distance("CA", "ABC");   // => 3
```

**Remarks** — for real text this and `DamerauLevenshtein` agree almost always, and this one costs
less to compute — three rolling rows instead of a full matrix and a symbol table. Reach for it as
the default transposition-aware distance, and only move to `DamerauLevenshtein` if the pairs you
are matching really do need a stretch edited twice.

The trap is that "almost always" is not always, and the disagreement is silent. `"CA"` to `"ABC"`
is
2 under `DamerauLevenshtein` and 3 here, because reaching 2 means transposing `CA` to `AC` and
then
inserting into that same stretch. If a test suite was built against Python's
`DamerauLevenshtein.distance`, `Osa.Distance` will pass on nearly every case and fail on a
handful,
which is the worst way to discover the difference.

The restriction costs one property outright: unlike `Levenshtein` and unlike unrestricted
`DamerauLevenshtein`, this is **not a metric**. The triangle inequality fails —
`Osa.Distance("bca", "ab")` is 3, while going through `"ba"` costs `1 + 1` — so a BK-tree or any
other structure that assumes a metric will silently return wrong neighbours. Use
`DamerauLevenshtein` when you need to index rather than to score.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Osa.NormalizedSimilarity`, `DamerauLevenshtein.Distance`, `Levenshtein.Distance`,
the [Python equivalence table](../../../equivalence.md).
