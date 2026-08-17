# Pooler

Turns per-token model outputs into a single sentence embedding.

<!-- docs-declaration -->

```csharp
public static class Pooler
```

**Example** — two token positions, the second one padding, so only the first reaches the result.

```csharp
using Lodestar.Embeddings.Pooling;

float[] tokens = { 3f, 4f, 99f, 99f };   // two positions, dim 2
long[] mask = { 1L, 0L };                 // the second is padding

float[] pooled = Pooler.MeanPoolAndNormalize(tokens, seqLen: 2, dim: 2, mask);
float x = pooled[0];  // => 0.6
float y = pooled[1];  // => 0.8
```

**Remarks** — `99f` never reaches the answer, which is what the mask is for: the pooled vector is
`(3, 4)` scaled to unit length, and the padding position is not averaged in.

The recipe is masked mean then L2 normalization — what sentence-transformers' `encode` does to one
forward pass. The two `AndNormalize` members do both steps and are what most callers want; the
others exist so a caller can do one half.

Every member is static and allocates only its result, so all five are safe to call from any number
of threads.

**Applies to** — net10.0, netstandard2.0.

**See also** — [the pooling index](../pooling.md), [the embeddings guide](../../../guides/embeddings.md).

## Members

| Member | What it does |
| --- | --- |
| [`Pooler.L2Normalize`](pooler-l2normalize.md) | Scales a vector in place to unit length. |
| [`Pooler.MeanPool`](pooler-meanpool.md) | The masked mean of one sequence. |
| [`Pooler.MeanPoolAndNormalize`](pooler-meanpoolandnormalize.md) | The full recipe, one sequence. |
| [`Pooler.MeanPoolAndNormalizeBatch`](pooler-meanpoolandnormalizebatch.md) | The full recipe, a padded batch. |
| [`Pooler.MeanPoolBatch`](pooler-meanpoolbatch.md) | The masked mean of every sequence in a batch. |
