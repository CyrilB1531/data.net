# SorensenDice.Similarity

Computes the Sørensen-Dice similarity of two inputs: shared q-grams counted twice, over the two
bag sizes added.

<!-- docs-declaration -->

```csharp
public static double Similarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, int qval = 1, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two inputs to compare; a `string` converts implicitly, and
swapping them does not change the answer. `qval` is how many characters make one gram, `1` by
default; it must be at least `1`. `element` says what counts as one character:
`TextElement.Utf16Unit` by default, or `TextElement.CodePoint` to match Python outside the Basic
Multilingual Plane.

**Returns** — `double` in `[0, 1]`. `1` when the two bags hold exactly the same grams with the
same multiplicities, `0` when they share none.

**Exceptions** — `ArgumentOutOfRangeException` when `qval` is below `1`.

**Example** — the pair from the index, and the two extremes.

```csharp
using Lodestar.Text.Similarity;

double related = SorensenDice.Similarity("apple", "pineapple");  // => 0.7142…
double same = SorensenDice.Similarity("abc", "abc");  // => 1
double nothing = SorensenDice.Similarity("abc", "xyz");  // => 0
```

**Remarks** — the numerator counts every shared gram twice, which is the whole of the difference
from [`Jaccard.Similarity`](jaccard-similarity.md): the same pair reads `0.7142…` here and
`0.5555…` there. Because `Dice = 2·Jaccard / (1 + Jaccard)` is increasing, the two never disagree
about which of two candidates is the better match — only about by how much.

Two empty inputs give `1`; one empty input against a non-empty one gives `0`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Jaccard.Similarity`](jaccard-similarity.md),
[`Tversky.Similarity`](tversky-similarity.md),
[the Python equivalence table](../../../equivalence.md).
