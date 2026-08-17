# Tversky.Similarity

Computes the Tversky index of two inputs: shared q-grams against each side's surplus, weighted
separately.

<!-- docs-declaration -->

```csharp
public static double Similarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, double alpha = 1, double beta = 1, int qval = 1, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two inputs to compare; a `string` converts implicitly. Unlike
the rest of this namespace, **their order matters** unless `alpha` and `beta` are equal. `alpha`
weights the grams `a` holds and `b` does not, `beta` those `b` holds and `a` does not; both are
`1` by default, which is [`Jaccard.Similarity`](jaccard-similarity.md). `qval` is how many
characters make one gram, `1` by default; it must be at least `1`. `element` says what counts as
one character: `TextElement.Utf16Unit` by default, or `TextElement.CodePoint` to match Python
outside the Basic Multilingual Plane.

**Returns** — `double`, in `[0, 1]` for non-negative `alpha` and `beta`. `1` when the weighted
surpluses vanish, `0` when the two share no gram.

**Exceptions** — `ArgumentOutOfRangeException` when `qval` is below `1`.

**Example** — the same pair read symmetrically, as containment, and on `SorensenDice`'s scale.

```csharp
using Lodestar.Text.Similarity;

double symmetric = Tversky.Similarity("apple", "pineapple");  // => 0.5555…
double contained = Tversky.Similarity("apple", "pineapple", alpha: 1, beta: 0);  // => 1
double dice = Tversky.Similarity("apple", "pineapple", alpha: 0.5, beta: 0.5);  // => 0.7142…
```

**Remarks** — the weights are what make this the only asymmetric measure in the namespace.
`alpha: 1, beta: 0` charges nothing for what `b` holds alone, so it answers "is `a` contained in
`b`" — `1` above, because every gram of `"apple"` is in `"pineapple"`. Reversing the weights to
`alpha: 0, beta: 1` reads `0.5555…` for this pair, the same as the symmetric default, since
`"apple"` has no surplus of its own for `alpha` to charge in the first place.

Two settings reproduce neighbours exactly: `alpha: 1, beta: 1` is
[`Jaccard.Similarity`](jaccard-similarity.md), and `alpha: 0.5, beta: 0.5` is
[`SorensenDice.Similarity`](sorensendice-similarity.md) — `0.7142…` above, the number that page
prints for the same pair.

Two edges belong to the caller rather than to the measure, because the weights are theirs.

**A zero weight makes an empty input score `1`.** With `beta: 0` nothing is charged for what `b`
holds alone, so an empty `a` has no surplus, no intersection and no denominator at all — and the
degenerate case answers `1`, the same value two empty inputs get. An empty query is *vacuously*
contained in everything, which is arithmetically right and rarely what a caller wanted:

```csharp
using Lodestar.Text.Similarity;

double vacuous = Tversky.Similarity("", "abc", alpha: 1, beta: 0);  // => 1
```

**Negative weights leave `[0, 1]`.** They are accepted rather than rejected, and a negative
denominator yields a legitimate quotient, so the bound in **Returns** holds only for non-negative
`alpha` and `beta`.

Two empty inputs give `1` whatever the weights.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Jaccard.Similarity`](jaccard-similarity.md),
[`Overlap.Similarity`](overlap-similarity.md),
[the Python equivalence table](../../../equivalence.md).
