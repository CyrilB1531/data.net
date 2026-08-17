# Overlap

Shared q-grams over the smaller bag: `|A∩B| / min(|A|, |B|)`. Also the Szymkiewicz-Simpson
coefficient.

<!-- docs-declaration -->

```csharp
public static class Overlap
```

**Example** — containment, which is what this measure is for.

```csharp
using Lodestar.Text.Similarity;

double contained = Overlap.Similarity("apple", "pineapple");  // => 1
```

**Remarks** — the size gap between the two inputs is divided away rather than charged for, which
makes `Overlap` the right answer to "does the shorter one appear in the longer" and the wrong
answer to "do these two agree".

That is also its trap, and it is worth stating plainly: **every bag contained in another scores
`1`**, however much extra the other holds. A one-character query scores `1` against any text
containing that character. Reach for [`Cosine`](cosine.md) when the extra material should still
cost something, or [`Tversky`](tversky.md) when the containment should be measured in one
direction only.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Cosine`](cosine.md), [`Tversky`](tversky.md),
[the set-similarity index](../similarity.md).

## Members

| Member | What it does |
| --- | --- |
| [`Overlap.Similarity`](overlap-similarity.md) | Shared grams over the smaller of the two bags. |
