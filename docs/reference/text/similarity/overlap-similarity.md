# Overlap.Similarity

Computes the overlap coefficient of two inputs: shared q-grams over the smaller of the two bags.

<!-- docs-declaration -->

```csharp
public static double Similarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, int qval = 1, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two inputs to compare; a `string` converts implicitly, and
swapping them does not change the answer. `qval` is how many characters make one gram, `1` by
default; it must be at least `1`. `element` says what counts as one character:
`TextElement.Utf16Unit` by default, or `TextElement.CodePoint` to match Python outside the Basic
Multilingual Plane.

**Returns** — `double` in `[0, 1]`. `1` when the smaller bag is wholly contained in the larger,
`0` when they share no gram.

**Exceptions** — `ArgumentOutOfRangeException` when `qval` is below `1`.

**Example** — containment scores full marks, and a disjoint pair scores nothing.

```csharp
using Lodestar.Text.Similarity;

double contained = Overlap.Similarity("apple", "pineapple");  // => 1
double nothing = Overlap.Similarity("abc", "xyz");  // => 0
```

**Remarks** — `1` here does **not** mean the two inputs are alike. It means one bag holds every
gram of the other, which `"apple"` against `"pineapple"` satisfies while
[`Jaccard.Similarity`](jaccard-similarity.md) reads the same pair as `0.5555…`. Anything ranked by
this measure will place a short candidate above a longer, better one whenever the short one
happens to be contained, so a cutoff on `Overlap` alone is rarely the filter it looks like.

Where the containment has a direction — "is the query inside the document", not "is either inside
the other" — [`Tversky.Similarity`](tversky-similarity.md) with `alpha: 1, beta: 0` is the measure
that says so, because `Overlap` takes the smaller bag whichever argument it arrived in.

Two empty inputs give `1`; one empty input against a non-empty one gives `0`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Cosine.Similarity`](cosine-similarity.md),
[`Tversky.Similarity`](tversky-similarity.md),
[the Python equivalence table](../../../equivalence.md).
