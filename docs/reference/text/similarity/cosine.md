# Cosine

Shared q-grams over the geometric mean of the two bag sizes: `|A∩B| / √(|A|·|B|)`. Also the Ochiai
coefficient.

<!-- docs-declaration -->

```csharp
public static class Cosine
```

**Example** — the same pair the other four score, on this scale.

```csharp
using Lodestar.Text.Similarity;

double related = Cosine.Similarity("apple", "pineapple");  // => 0.7453…
```

**Remarks** — this is the cosine of the angle between the two gram-count vectors, which is where
the name comes from; on multisets of q-grams it reduces to the expression above. It is a
compromise between its neighbours, and that is the reason to reach for it: `√(|A|·|B|)` sits
between [`SorensenDice`](sorensendice.md)'s arithmetic mean and [`Overlap`](overlap.md)'s
`min`, so a size gap costs something without costing everything.

It has nothing to do with the cosine similarity of two **embedding** vectors, which compares
learned representations rather than counted grams; that one lives in `Lodestar.Embeddings`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Overlap`](overlap.md), [`SorensenDice`](sorensendice.md),
[the set-similarity index](../similarity.md).

## Members

| Member | What it does |
| --- | --- |
| [`Cosine.Similarity`](cosine-similarity.md) | Shared grams over the geometric mean of both bag sizes. |
