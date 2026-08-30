# BlockNormalization

How a block handed to a bulk ingest relates to the index's normalization.

<!-- docs-declaration -->

```csharp
public enum BlockNormalization { Normalize, AlreadyNormalized, Off }
```

**Members** — `Normalize` L2-normalizes every vector of the block and turns the index's
normalization on, which is what makes a dot product a cosine. `AlreadyNormalized` also turns it on
but stores the block bit for bit, on the caller's promise that its vectors are already unit length.
`Off` turns normalization off on insertion *and* on query, which is a raw dot product.

**Example** — the same block ingested two ways, and the scores that follow from it.

```csharp
using Lodestar.Embeddings.Search;

float[] raw = { 3f, 4f };
var normalizing = EmbeddingIndex.FromBlock(raw, dimension: 2, BlockNormalization.Normalize);
var verbatim = EmbeddingIndex.FromBlock(raw, dimension: 2, BlockNormalization.Off);

float scored = normalizing.Search(new float[] { 3f, 4f }, 1)[0].Score;  // => 1
float unscored = verbatim.Search(new float[] { 3f, 4f }, 1)[0].Score;  // => 25
```

Both were given `(3, 4)`. The first normalized it to `(0.6, 0.8)` and normalizes the query the
same way, so a vector queried against itself scores `1` however long it was. The second compares
the two verbatim, and `3² + 4²` is `25` — a number that grows with the length of whatever is being
compared, which is exactly what cosine exists to remove.

**Remarks** — one argument rather than a `normalize` flag beside an `alreadyNormalized` one. The
index's flag governs the query as well as the store, so a pair of booleans would make a fourth
combination representable that means nothing; three named answers make each one sayable and
nothing else.

`Normalize` is the zero value, so a `default(BlockNormalization)` reaching an ingest by accident
yields the correct-but-slower behaviour rather than a silently wrong score. That ordering is
deliberate and is the reason the enum is not alphabetical.

**`AlreadyNormalized` is the one that can hurt.** It is a promise, not a check: a block that is not
unit length taken this way is stored as it is, scored against a normalized query, and every
similarity comes back scaled by the vector's own length — a ranking that is wrong and looks
plausible. Reach for it when the vectors came out of a model that normalizes, or out of an index
that was saved normalized; otherwise `Normalize` costs one pass over the block and removes the
question.

A value outside the three — a cast from an `int`, most likely — is refused by both
[`EmbeddingIndex.FromBlock`](embeddingindex-fromblock.md) and
[`EmbeddingIndex.FromOwnedBlock`](embeddingindex-fromownedblock.md) rather than being read as
`AlreadyNormalized`, which is what the fall-through would otherwise have made it.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`EmbeddingIndex.FromBlock`](embeddingindex-fromblock.md),
[`EmbeddingIndex.FromOwnedBlock`](embeddingindex-fromownedblock.md),
[`EmbeddingIndex`](embeddingindex.md), [the search index](../search.md).
