# Tversky

Shared q-grams against the two sides' surpluses, weighted separately:
`|A∩B| / (|A∩B| + α·|A\B| + β·|B\A|)`.

<!-- docs-declaration -->

```csharp
public static class Tversky
```

**Example** — the default weights, which are [`Jaccard`](jaccard.md).

```csharp
using Lodestar.Text.Similarity;

double symmetric = Tversky.Similarity("apple", "pineapple");  // => 0.5555…
```

**Remarks** — the general form the rest of this namespace are cases of. `α = β = 1` is
[`Jaccard`](jaccard.md), `α = β = 0.5` is [`SorensenDice`](sorensendice.md), and any `α = β` gives
a symmetric measure that ranks the same way both do.

What the others cannot express is `α ≠ β`. The two surpluses are charged separately, so the
measure acquires a **direction**: `α = 1, β = 0` charges only for what the first input holds alone
and therefore asks "is `a` contained in `b`", while `α = 0, β = 1` asks the reverse. That is the
reason to reach for this type rather than one of the four simpler ones.

The price of that generality is that the weights are the caller's, and two of the guarantees the
other four offer stop holding — the result need not lie in `[0, 1]`, and an empty input need not
score `0`. [`Tversky.Similarity`](tversky-similarity.md) says when.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Jaccard`](jaccard.md), [`Overlap`](overlap.md),
[the set-similarity index](../similarity.md).

## Members

| Member | What it does |
| --- | --- |
| [`Tversky.Similarity`](tversky-similarity.md) | Shared grams against each side's surplus, weighted by `alpha` and `beta`. |
