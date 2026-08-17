# Cosine.Similarity

Computes the cosine (Ochiai) similarity of two inputs over their q-gram multisets.

<!-- docs-declaration -->

```csharp
public static double Similarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, int qval = 1, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two inputs to compare; a `string` converts implicitly, and
swapping them does not change the answer. `qval` is how many characters make one gram, `1` by
default; it must be at least `1`. `element` says what counts as one character:
`TextElement.Utf16Unit` by default, or `TextElement.CodePoint` to match Python outside the Basic
Multilingual Plane.

**Returns** — `double` in `[0, 1]`. `1` when the two bags hold the same grams in the same
proportions, `0` when they share none.

**Exceptions** — `ArgumentOutOfRangeException` when `qval` is below `1`.

**Example** — single characters by default, and bigrams when `qval` says so.

```csharp
using Lodestar.Text.Similarity;

double related = Cosine.Similarity("apple", "pineapple");  // => 0.7453…
double bigrams = Cosine.Similarity("night", "nacht", qval: 2);  // => 0.25
```

**Remarks** — `qval: 2` is worth knowing about here, and the example shows why. As single
characters, `"night"` and `"nacht"` share three of five; as bigrams they share only `ht`, and the
score collapses from a passing `0.6` to `0.25`. Longer grams carry the **order** of the characters
inside them, which is most of what a bag of single characters throws away — and the usual fix when
set similarity reports two anagrams as identical.

The cost is that grams get rarer as `qval` grows, so scores fall across the board and a threshold
belongs to the `qval` it was chosen for.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Overlap.Similarity`](overlap-similarity.md),
[`Jaccard.Similarity`](jaccard-similarity.md),
[the Python equivalence table](../../../equivalence.md).
