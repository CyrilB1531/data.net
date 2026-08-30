# EmbeddingIndex.FromOwnedBlock

Builds an index that **takes** the block, without copying it.

<!-- docs-declaration -->

```csharp
public static EmbeddingIndex FromOwnedBlock(float[] block, int dimension, BlockNormalization normalization, IReadOnlyList<string> ids = null)
```

**Parameters** — `block` is the vectors laid out row after row in C order, handed over rather than
lent: it becomes the index's backing store. Its length must be a multiple of `dimension`.
`dimension` is the length every vector has, at least `1`. `normalization` says what is to be done
about the block and what the index's own normalization flag becomes —
[`BlockNormalization`](blocknormalization.md) has the three answers, and `Normalize` rewrites
`block` in place. `ids` is one id per vector, or `null` for an anonymous index; it is copied, never
retained, which the block is not.

**Returns** — a new `EmbeddingIndex` holding `block.Length / dimension` vectors, backed by the very
array it was given.

**Exceptions** — `ArgumentNullException` when `block` is null. `ArgumentOutOfRangeException` when
`dimension` is below 1, or `normalization` is not one of the enum's values. `ArgumentException`
when `block`'s length is not a multiple of `dimension`, or `ids` holds a number of entries other
than the vector count.

**Example** — the array is the index's now, and normalization is visible in it.

```csharp
using Lodestar.Embeddings.Search;

float[] block = { 3f, 4f };
var index = EmbeddingIndex.FromOwnedBlock(block, dimension: 2, BlockNormalization.Normalize);

// The index took the array, so normalization happened in the caller's own values.
float first = block[0];  // => 0.6
int count = index.Count;  // => 1
```

**Remarks** — **ownership transfers.** The index reads this array for as long as it lives, so
three obligations move to the caller and stay there: do not write to it, do not return it to an
`ArrayPool`, and expect `Normalize` to change its values in place. None of the three can be
checked by the library, and a breach is silent — a returned array re-rented elsewhere becomes this
index's embeddings, and it goes on scoring queries against another renter's bytes without raising
anything.

[`EmbeddingIndex.FromBlock`](embeddingindex-fromblock.md) is the one to reach for unless that copy
has been measured and matters. It costs one pass over the block and asks nothing of the caller;
this saves the pass and asks for a permanent invariant.
[Decision 0056](../../../decisions/0056-a-block-may-be-adopted-and-the-invariant-is-the-callers-to-keep.md)
records why both ship rather than only the copying one: a caller holding a block from a model's
output, a memory-mapped file or a column store would otherwise have no way to avoid a copy the
library can see is unnecessary.

The array whose ownership is easiest to give up is one nobody else has ever held — the freshly
allocated block that [`NpyFile.Read`](../persistence/npyfile-read.md) returns is the shape this
exists for. That block is not yet reachable from here: `NpyFile.Read` hands it back as a
`ReadOnlyMemory<float>`, and there is no supported way from that back to the array. Closing that
gap is
[decision 0055](../../../decisions/0055-the-artifact-gets-a-binary-sidecar-once-a-block-can-be-ingested-whole.md)'s
work.

**A block holding `NaN` or an infinity is accepted here, exactly as [`Add`](embeddingindex-add.md)
accepts one** — the two ingest paths cannot disagree about what an index may hold. It is
[`Save`](embeddingindex-save.md) that refuses it.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`EmbeddingIndex.FromBlock`](embeddingindex-fromblock.md),
[`BlockNormalization`](blocknormalization.md), [`EmbeddingIndex.Add`](embeddingindex-add.md),
[`EmbeddingIndex`](embeddingindex.md).
