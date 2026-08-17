# Jaccard.Similarity

Computes the Jaccard similarity of two inputs: shared q-grams over the union of both bags.

<!-- docs-declaration -->

```csharp
public static double Similarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, int qval = 1, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two inputs to compare; a `string` converts implicitly, so
nothing is allocated for them, and swapping them does not change the answer. `qval` is how many
characters make one gram, `1` by default; it must be at least `1`. `element` says what counts as
one character: `TextElement.Utf16Unit` by default, the native and fastest choice, or
`TextElement.CodePoint` to match Python outside the Basic Multilingual Plane.

**Returns** — `double` in `[0, 1]`. `1` when the two bags hold exactly the same grams with the
same multiplicities, `0` when they share none.

**Exceptions** — `ArgumentOutOfRangeException` when `qval` is below `1`.

**Example** — a fragment against the whole, then the two extremes.

```csharp
using Lodestar.Text.Similarity;

double related = Jaccard.Similarity("apple", "pineapple");  // => 0.5555…
double same = Jaccard.Similarity("abc", "abc");  // => 1
double nothing = Jaccard.Similarity("abc", "xyz");  // => 0
```

**Remarks** — the five grams of `"apple"` are all present in `"pineapple"`, and the answer is
still only `5/9`, because the four grams `"pineapple"` holds alone are charged in full. That is
the measure working as intended, and it is also the reason a search that matches short queries
against long documents wants [`Overlap`](overlap.md) or [`Cosine`](cosine.md) instead.

What `element` changes is visible as soon as a character leaves the Basic Multilingual Plane. An
emoji is one code point and two UTF-16 units, so the two readings count different bags:

```csharp
using Lodestar.Text;
using Lodestar.Text.Similarity;

double units = Jaccard.Similarity("🙂a", "🙂b");  // => 0.5
double points = Jaccard.Similarity("🙂a", "🙂b", element: TextElement.CodePoint);  // => 0.3333…
```

Two empty inputs give `1`; one empty input against a non-empty one gives `0`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SorensenDice.Similarity`](sorensendice-similarity.md),
[`Tversky.Similarity`](tversky-similarity.md),
[the Python equivalence table](../../../equivalence.md).
