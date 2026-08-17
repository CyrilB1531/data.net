# Pooler.MeanPoolBatch

The masked mean of every sequence in a padded batch.

<!-- docs-declaration -->

```csharp
public static float[][] MeanPoolBatch(ReadOnlySpan<float> tokenEmbeddings, int batchSize, int seqLen, int dim, ReadOnlySpan<long> attentionMask)
```

**Parameters** — `tokenEmbeddings` is row-major `[batchSize × seqLen × dim]` and `attentionMask` is row-major
`[batchSize × seqLen]`. `batchSize` is the number of sequences, `seqLen` the **padded** length of
every one, `dim` the embedding dimension.

**Returns** — `float[][]`, one `dim`-length vector per sequence, in batch order. Not normalized.

**Exceptions** — `ArgumentException` when the spans do not match the shape the other arguments declare.

**Example** — two sequences of different real lengths, padded to the same width.

```csharp
using Lodestar.Embeddings.Pooling;

float[] tokens = { 1f, 1f, 3f, 3f,    5f, 5f, 99f, 99f };   // 2 sequences, 2 positions, dim 2
long[] mask = { 1L, 1L,   1L, 0L };                          // the second is half padding

float[][] pooled = Pooler.MeanPoolBatch(tokens, batchSize: 2, seqLen: 2, dim: 2, mask);
float bothTokens = pooled[0][0];  // => 2
float oneToken = pooled[1][0];  // => 5
```

**Remarks** — **Each sequence is pooled against its own slice of the mask**, so the padding a shorter sequence
carries cannot reach its vector — and, just as importantly, cannot reach its neighbour's. The
first sequence averages `1` and `3` to `2`; the second has one real token and comes back as `5`
rather than `52`.

This is the shape an encoder actually returns for a batch, so it is the call that avoids slicing
the tensor by hand. Doing that slicing yourself is where off-by-one errors in `seqLen` produce
vectors that are wrong without being obviously wrong.

The batched counterpart of [`MeanPool`](pooler-meanpool.md), and usually a step on the way to
[`MeanPoolAndNormalizeBatch`](pooler-meanpoolandnormalizebatch.md).

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Pooler.MeanPoolAndNormalizeBatch`](pooler-meanpoolandnormalizebatch.md),
[`Pooler.MeanPool`](pooler-meanpool.md), [`Pooler`](pooler.md).
