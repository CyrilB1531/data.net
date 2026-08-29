# EmbeddingIndex.FromBlock

Builds an index from a contiguous block of vectors, in one copy.

<!-- docs-declaration -->

```csharp
public static EmbeddingIndex FromBlock(ReadOnlySpan<float> block, int dimension, BlockNormalization normalization, IReadOnlyList<string> ids = null)
```

**Parameters** — `block` is the vectors laid out row after row in C order, so vector *i* occupies
`[i * dimension, (i + 1) * dimension)`; its length must be a multiple of `dimension`, and an empty
block makes an empty index. `dimension` is the length every vector has, at least `1`.
`normalization` says what is to be done about the block and what the index's own normalization
flag becomes — [`BlockNormalization`](blocknormalization.md) has the three answers. `ids` is one id
per vector, or `null` for an anonymous index; it is copied, never retained, and a `null` entry
inside it is a vector without an id.

**Returns** — a new `EmbeddingIndex` holding `block.Length / dimension` vectors, ready to
[`Search`](embeddingindex-search.md) with no further work.

**Exceptions** — `ArgumentOutOfRangeException` when `dimension` is below 1, or `normalization` is
not one of the enum's values — a value reached by a cast would otherwise be read as
`AlreadyNormalized` and score wrongly. `ArgumentException` when `block`'s length is not a multiple
of `dimension`, or `ids` holds a number of entries other than the vector count; both messages name
the two numbers that disagree.

**Example** — two unit vectors and their ids, taken in one call.

```csharp
using Lodestar.Embeddings.Search;

float[] block = { 1f, 0f, 0f, 1f };
var index = EmbeddingIndex.FromBlock(block, dimension: 2, BlockNormalization.AlreadyNormalized, ["east", "north"]);

int count = index.Count;  // => 2
string first = index.GetId(0)!;  // => east
```

**Remarks** — this is what a caller holding a whole corpus reaches for: a `.npy` block, a model's
output, a column read out of a store. The block is **copied**, so the caller's array is neither
retained nor modified and may be reused or freed the moment the call returns.

Replaying the same corpus through [`EmbeddingIndex.Add`](embeddingindex-add.md) is the route that
existed before, and it costs three times the read that produced the block: `Add` copies one vector,
normalizes it, and grows a backing store that doubles on the way up
([#474](https://github.com/CyrilB1531/lodestar/issues/474)). This allocates the store once, at the
size the block already tells it, and copies once. Where the copy itself is the thing being paid
for and the caller can give the array up for good,
[`EmbeddingIndex.FromOwnedBlock`](embeddingindex-fromownedblock.md) skips it —
[decision 0056](../../../decisions/0056-a-block-may-be-adopted-and-the-invariant-is-the-callers-to-keep.md)
has the trade, and the short version is that this one asks nothing of the caller.

**Normalization is decided here and cannot be changed afterwards**, because the index's flag
governs the query as well as the store. `Normalize` normalizes the copy;
`AlreadyNormalized` stores it bit for bit and is a promise the caller keeps — an unnormalized block
taken that way scores wrong and raises nothing; `Off` leaves both sides alone, which is a raw dot
product rather than a cosine.

**A block holding `NaN` or an infinity is accepted here, exactly as [`Add`](embeddingindex-add.md)
accepts one** — the two ingest paths cannot disagree about what an index may hold. It is
[`Save`](embeddingindex-save.md) that refuses it.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BlockNormalization`](blocknormalization.md),
[`EmbeddingIndex.FromOwnedBlock`](embeddingindex-fromownedblock.md),
[`EmbeddingIndex.Add`](embeddingindex-add.md), [`EmbeddingIndex`](embeddingindex.md).
