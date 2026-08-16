# Lcs.SubsequenceLength

Returns the length of the longest sequence of characters that appears in both inputs in the same
order, with gaps allowed.

<!-- docs-declaration -->

```csharp
public static int SubsequenceLength(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
public static int SubsequenceLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b) where T : IEquatable<T>
```

**Parameters** — `a` and `b` are the two strings to compare. `element` says what counts as one
character: `TextElement.Utf16Unit` by default, or `TextElement.CodePoint` to count an emoji once.
The second overload compares any two spans of an `IEquatable<T>`, which is how you get the longest
common run of **words** rather than of characters.

**Returns** — `int`, at most the length of the shorter input, and `0` when either is empty.

**Example** — `ittn` survives in both, out of six and seven characters.

```csharp
using Lodestar.Text.Distances;

int n = Lcs.SubsequenceLength("kitten", "sitting");   // => 4
```

**Remarks** — this is the classic LCS, and it is the thing to call when you want the shared
material itself rather than a score derived from it: how much of a document survived an edit, how
much of a template a candidate string fills in. `Indel.Distance` is precisely
`len(a) + len(b) - 2 × SubsequenceLength(a, b)`, so if you need both, call this and do the
arithmetic rather than paying for two passes.

The trap is that it is a raw length with no upper bound of its own, and the number alone says
nothing: a shared run of 4 is most of a six-letter word and nothing at all in a paragraph. It is
also not what `SubstringLength` returns — this one allows gaps, and the two answers differ on
almost
every real pair.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Lcs.SubstringLength`, `Indel.Distance`, `RatcliffObershelp.Similarity`,
the [Python equivalence table](../../../equivalence.md).
