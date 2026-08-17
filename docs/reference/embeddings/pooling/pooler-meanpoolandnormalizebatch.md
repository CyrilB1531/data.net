# Pooler.MeanPoolAndNormalizeBatch

Mean-pools then L2-normalizes every sequence of a padded batch.

<!-- docs-declaration -->

```csharp
public static float[][] MeanPoolAndNormalizeBatch(ReadOnlySpan<float> tokenEmbeddings, int batchSize, int seqLen, int dim, ReadOnlySpan<long> attentionMask)
```

**Parameters** — `tokenEmbeddings` is row-major `[batchSize × seqLen × dim]` and `attentionMask` is row-major
`[batchSize × seqLen]`. `batchSize` is the number of sequences, `seqLen` the **padded** length of
every one, `dim` the embedding dimension.

**Returns** — `float[][]`, one unit-length vector per sequence, in batch order.

**Exceptions** — `ArgumentException` when the spans do not match the shape the other arguments declare.

**Example** — a whole forward pass turned into sentence embeddings.

```csharp
using Lodestar.Embeddings.Pooling;

float[] tokens = { 3f, 4f, 99f, 99f,    0f, 5f, 0f, 5f };   // 2 sequences, 2 positions, dim 2
long[] mask = { 1L, 0L,   1L, 1L };

float[][] embeddings = Pooler.MeanPoolAndNormalizeBatch(tokens, batchSize: 2, seqLen: 2, dim: 2, mask);
float first = embeddings[0][0];  // => 0.6
float second = embeddings[1][1];  // => 1
```

**Remarks** — What sentence-transformers' `encode` does to one forward pass, and the call an ONNX batch should
land in. Each sequence is pooled against its own slice of the mask and normalized on its own, so
neither the padding nor the length of a neighbour reaches a vector.

The second sequence here is the degenerate-looking case that is not: both its positions are real
and identical, so the mean is `(0, 5)` and normalizing gives `(0, 1)` — length discarded,
direction kept, which is the entire purpose.

The batched counterpart of [`MeanPoolAndNormalize`](pooler-meanpoolandnormalize.md).

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Pooler.MeanPoolAndNormalize`](pooler-meanpoolandnormalize.md),
[`Pooler.MeanPoolBatch`](pooler-meanpoolbatch.md), [`Pooler`](pooler.md).
