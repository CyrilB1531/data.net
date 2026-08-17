# SorensenDice

Shared q-grams counted twice, over the two bag sizes added: `2·|A∩B| / (|A| + |B|)`.

<!-- docs-declaration -->

```csharp
public static class SorensenDice
```

**Example** — the same pair [`Jaccard`](jaccard.md) scores, read on this scale.

```csharp
using Lodestar.Text.Similarity;

double related = SorensenDice.Similarity("apple", "pineapple");  // => 0.7142…
```

**Remarks** — the same comparison as [`Jaccard`](jaccard.md), on a more forgiving scale.
`Dice = 2·Jaccard / (1 + Jaccard)`, which rises with `Jaccard` across the whole of `[0, 1]`, so
the two produce the **same ranking** and differ only in the number. Sorting candidates by one
gives the order the other would; a threshold tuned against one has to be re-tuned for the other.

Reach for it when a human reads the score and `Jaccard`'s numbers feel unduly harsh. It is
[`Tversky`](tversky.md) with `α = β = 0.5`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Jaccard`](jaccard.md), [`Tversky`](tversky.md),
[the set-similarity index](../similarity.md).

## Members

| Member | What it does |
| --- | --- |
| [`SorensenDice.Similarity`](sorensendice-similarity.md) | Shared grams counted twice, over both bag sizes. |
