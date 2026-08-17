# Jaccard

Shared q-grams over the grams either side holds at all: `|A∩B| / |A∪B|`.

<!-- docs-declaration -->

```csharp
public static class Jaccard
```

**Example** — a word against one that contains it.

```csharp
using Lodestar.Text.Similarity;

double related = Jaccard.Similarity("apple", "pineapple");  // => 0.5555…
```

**Remarks** — the strictest of the five, and the usual default. Every gram either input holds
alone lands in the denominator, so `Jaccard` is the one that notices a size gap most sharply:
`"apple"` scores barely over a half against `"pineapple"` even though every one of its own grams
is present.

It ranks identically to [`SorensenDice`](sorensendice.md) — the two are a monotone function of one
another — so the choice between them is a choice of scale, not of order. It is also
[`Tversky`](tversky.md) with `α = β = 1`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SorensenDice`](sorensendice.md), [`Tversky`](tversky.md),
[the set-similarity index](../similarity.md).

## Members

| Member | What it does |
| --- | --- |
| [`Jaccard.Similarity`](jaccard-similarity.md) | Shared grams over the union of both bags. |
