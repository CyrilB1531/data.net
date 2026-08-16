# DamerauLevenshtein.Distance

Counts the fewest insertions, deletions, substitutions and swaps of neighbouring characters that
turn one string into the other.

<!-- docs-declaration -->

```csharp
public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
public static int Distance<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b) where T : IEquatable<T>
```

**Parameters** — `a` and `b` are the two strings to compare; a `string` converts implicitly, so
nothing is allocated for them. `element` says what counts as one character:
`TextElement.Utf16Unit`
by default, the native and fastest choice, or `TextElement.CodePoint` to match Python outside the
Basic Multilingual Plane. The second overload takes any two spans of an `IEquatable<T>` — words,
tokens, decoded code points — and compares elements rather than characters.

**Returns** — `int`, the number of edits. Zero when the two are equal, and never negative.

**Example** — a swap and an insertion, where `Osa` charges three edits for the same pair.

```csharp
using Lodestar.Text.Distances;

int d = DamerauLevenshtein.Distance("CA", "ABC");   // => 2
```

**Remarks** — reach for this instead of `Levenshtein` when the mistakes you are chasing are typing
mistakes: "teh" for "the" is one slip of the fingers, and `Levenshtein` charges two edits for it.
Reach for it instead of `Osa` when a stretch of text may need editing more than once — that single
restriction is the only difference between the two, and it is what makes `"CA"` to `"ABC"` cost 2
here and 3 there.

Where it matters most is the one place people expect the opposite. With unit costs this **is** a
proper metric — Lowrance-Wagner satisfies the triangle inequality because two transpositions never
cost less than an insertion plus a deletion — so it can be indexed by anything that needs one, a
BK-tree for nearest-neighbour lookup included. `Osa` cannot: restricting each stretch to a single
edit is exactly what breaks the inequality there, and `Osa.Distance("bca", "ab")` is 3 while the
route through `"ba"` costs `1 + 1`. If you are building an index rather than scoring one pair at a
time, that is the reason to take the unrestricted variant even though it costs more to compute.

The trap is the ordinary one for a raw distance: the result is unbounded, so three edits mean
something different between two names and between two paragraphs. Threshold on
`NormalizedSimilarity`, never on this.

**Applies to** — net10.0, netstandard2.0.

**See also** — `DamerauLevenshtein.NormalizedSimilarity`, `Osa.Distance`, `Levenshtein.Distance`,
the [Python equivalence table](../../../equivalence.md).
