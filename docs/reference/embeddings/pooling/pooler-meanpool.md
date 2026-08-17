# Pooler.MeanPool

The masked mean of one sequence's token embeddings.

<!-- docs-declaration -->

```csharp
public static float[] MeanPool(ReadOnlySpan<float> tokenEmbeddings, int seqLen, int dim, ReadOnlySpan<long> attentionMask)
```

**Parameters** — `tokenEmbeddings` is the encoder's output, row-major. `seqLen` is the number of token positions,
`dim` the embedding dimension, and `attentionMask` marks a real token with a non-zero value and
padding with zero. `attentionMask` has length `seqLen`.

**Returns** — `float[]` of length `dim`, the mean of the masked token vectors. Not normalized.

**Exceptions** — `ArgumentException` when the spans do not match the shape the other arguments declare.

**Example** — three positions, one of them padding.

```csharp
using Lodestar.Embeddings.Pooling;

float[] tokens = { 2f, 0f, 4f, 0f, 99f, 99f };   // three positions, dim 2
long[] mask = { 1L, 1L, 0L };

float[] pooled = Pooler.MeanPool(tokens, seqLen: 3, dim: 2, mask);
float first = pooled[0];  // => 3
float second = pooled[1];  // => 0
```

**Remarks** — The divisor is the number of **real** tokens, not `seqLen`: two here, not three, which is why the
mean of `2` and `4` is `3` and the padding row never enters it. Dividing by `seqLen` instead —
the mistake this method exists to prevent — would have given `2`.

The formula matches sentence-transformers' `mean_pooling` exactly, clamp included:
`sum(embeddings × mask) / max(sum(mask), 1e-9)`. That clamp is what an **all-padding** sequence
meets; it yields a zero vector rather than a division by zero, and
[`L2Normalize`](pooler-l2normalize.md) leaves a zero vector alone.

Use [`MeanPoolAndNormalize`](pooler-meanpoolandnormalize.md) unless you specifically want the
unnormalized mean.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Pooler.MeanPoolAndNormalize`](pooler-meanpoolandnormalize.md),
[`Pooler.MeanPoolBatch`](pooler-meanpoolbatch.md), [`Pooler`](pooler.md).
