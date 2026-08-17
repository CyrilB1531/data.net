# Pooling — `Lodestar.Embeddings.Pooling`

A transformer returns one vector **per token**. A sentence embedding is one vector. Pooling is the
step between, and getting it wrong is the quiet way to make every downstream similarity slightly
untrue.

`Lodestar.Embeddings.Pooling` holds one static class, [`Pooler`](pooling/pooler.md), implementing
the recipe sentence-transformers uses: **masked mean, then L2 normalization**.

## Which call?

| You have | You want | Call |
| --- | --- | --- |
| one sequence | a sentence embedding | [`MeanPoolAndNormalize`](pooling/pooler-meanpoolandnormalize.md) |
| a padded batch | one embedding per sequence | [`MeanPoolAndNormalizeBatch`](pooling/pooler-meanpoolandnormalizebatch.md) |
| one sequence | the mean only, unnormalized | [`MeanPool`](pooling/pooler-meanpool.md) |
| a padded batch | the means only | [`MeanPoolBatch`](pooling/pooler-meanpoolbatch.md) |
| a vector already pooled | it scaled to unit length | [`L2Normalize`](pooling/pooler-l2normalize.md) |

The two `AndNormalize` forms are the ones to reach for. The others exist because a caller who
pools now and normalizes later, or who pools something this package did not produce, should not
have to reimplement half the recipe.

## The mask is the whole point

A batch is padded to its longest sequence, and the padding positions carry embeddings — the model
computed something for them. Averaging those in would drag every short sequence's vector toward
whatever the padding token happens to mean.

**The mask is what excludes them**, and every call here takes one. The mean divides by the number
of real tokens, not by `seqLen`: `sum(embeddings × mask) / max(sum(mask), 1e-9)`, which is
sentence-transformers' own formula down to the clamp that keeps an all-padding sequence from
dividing by zero.

## Why normalize

Cosine similarity is a dot product **only on unit vectors**, which is what
[`EmbeddingIndex`](search/embeddingindex.md) relies on. Normalizing at pooling time
means every consumer downstream gets that for free.

## The same bits on both target frameworks

This namespace makes the opposite trade from `VectorMath`. In
[`L2Normalize`](pooling/pooler-l2normalize.md) the sum of squares is accumulated in `double` and
**deliberately not vectorized**: a `Vector<float>` accumulator would lose precision *and* make the
result depend on the SIMD width of the machine that ran it. The scaling pass, which is exact
whichever way it is done, is vectorized.

The result is a pooled vector that is bit-identical between `net10.0` and `netstandard2.0`, and
between machines of different vector widths.

## Types

| Type | What it is |
| --- | --- |
| [`Pooler`](pooling/pooler.md) | The five pooling calls. |

## See also

- [Embeddings, end to end](../../guides/embeddings.md) — where pooling sits in the chain.
- [Python → C# equivalence](../../equivalence.md) — the pooling rows.
