# Lcs.SubstringLength

Returns the length of the longest **contiguous** run of characters that appears in both inputs.

<!-- docs-declaration -->

```csharp
public static int SubstringLength(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
public static int SubstringLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b) where T : IEquatable<T>
```

**Parameters** — `a` and `b` are the two strings to compare; `element` says what counts as one
character. The second overload compares any two spans of an `IEquatable<T>`.

**Returns** — `int`, at most the length of the shorter input, and `0` when either is empty.

**Example** — the same pair as above, where only `itt` is unbroken.

```csharp
using DataNet.Text.Distances;

int n = Lcs.SubstringLength("kitten", "sitting");   // => 3
```

**Remarks** — contiguity is the whole difference from `SubsequenceLength`, and it is what makes
this the right call for detecting quoted or copied text, shared identifiers, or a common stem. It
matches the `size` that Python's `difflib.SequenceMatcher.find_longest_match` reports. Only the
size: this returns no position, and it swaps its two operands internally when `b` is the longer,
so
which of several equally long runs was measured is not observable from here. If you need the run's
location, difflib's tie-break is the one `RatcliffObershelp` applies.

The trap is how brittle contiguity is. A single character inserted in the middle of an otherwise
identical string halves this number, while `SubsequenceLength` barely moves. If you are measuring
"how much do these two share" rather than "what is the longest unbroken quote", you almost
certainly
want the subsequence.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Lcs.SubsequenceLength`, `RatcliffObershelp.Similarity`,
the [Python equivalence table](../../../equivalence.md).
