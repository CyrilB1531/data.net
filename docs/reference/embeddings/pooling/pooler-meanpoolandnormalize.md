# Pooler.MeanPoolAndNormalize

Mean-pools then L2-normalizes — the full sentence-embedding recipe.

<!-- docs-declaration -->

```csharp
public static float[] MeanPoolAndNormalize(ReadOnlySpan<float> tokenEmbeddings, int seqLen, int dim, ReadOnlySpan<long> attentionMask)
```

**Parameters** — `tokenEmbeddings` is the encoder's output, row-major. `seqLen` is the number of token positions,
`dim` the embedding dimension, and `attentionMask` marks a real token with a non-zero value and
padding with zero. `attentionMask` has length `seqLen`.

**Returns** — `float[]` of length `dim`, of unit length unless every token was masked out.

**Exceptions** — `ArgumentException` when the spans do not match the shape the other arguments declare.

**Example** — one sequence, one sentence embedding.

```csharp
using Lodestar.Embeddings.Pooling;

float[] tokens = { 3f, 4f, 99f, 99f };
long[] mask = { 1L, 0L };

float[] embedding = Pooler.MeanPoolAndNormalize(tokens, seqLen: 2, dim: 2, mask);
float x = embedding[0];  // => 0.6
float y = embedding[1];  // => 0.8
```

**Remarks** — **This is the call to reach for.** It is [`MeanPool`](pooler-meanpool.md) followed by
[`L2Normalize`](pooler-l2normalize.md), which is what sentence-transformers does to one forward
pass, and doing the two steps separately gains nothing but the chance to forget the second.

Forgetting it is not a loud failure. An unnormalized vector still scores, still ranks, and ranks
**wrongly**: a longer sentence tends to a longer vector, so a dot-product index quietly prefers
it. Normalizing is what makes the score depend on direction alone.

The result is ready for [`EmbeddingIndex.Add`](../search/embeddingindex-add.md), which is where
most of these vectors are going.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Pooler.MeanPoolAndNormalizeBatch`](pooler-meanpoolandnormalizebatch.md),
[`Pooler.MeanPool`](pooler-meanpool.md), [`Pooler`](pooler.md).
