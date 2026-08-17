# VectorMath.Dot

The dot product of two equal-length vectors.

<!-- docs-declaration -->

```csharp
public static float Dot(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
```

**Parameters** — `a` and `b` are the two vectors. They must be the same length; a `float[]`
converts implicitly, so nothing is allocated to pass one.

**Returns** — `float`, the sum of the element-wise products. Order of the arguments does not
change it.

**Exceptions** — `ArgumentException` when `a` and `b` differ in length. There is no silent
truncation to the shorter of the two.

**Example** — the textbook product, and the same call standing in for cosine similarity on unit
vectors.

```csharp
using Lodestar.Embeddings.Search;

float plain = VectorMath.Dot(new float[] { 1f, 2f, 3f }, new float[] { 4f, 5f, 6f });  // => 32
float orthogonal = VectorMath.Dot(new float[] { 1f, 0f }, new float[] { 0f, 1f });  // => 0
```

**Remarks** — on two **L2-normalized** vectors this is cosine similarity: `1` for the same
direction, `0` for perpendicular, `-1` for opposite. That identity is the whole reason
[`EmbeddingIndex`](embeddingindex.md) normalizes on insertion — it turns a similarity metric into
one multiply-accumulate loop.

On unnormalized vectors it is not a similarity, because length contributes. A long vector scores
high against everything.

The accumulation order differs between the two target frameworks, which
[`VectorMath`](vectormath.md) explains: the SIMD path on `net10.0` sums lane-wise and the
`netstandard2.0` scalar loop sums left to right, so long vectors can disagree in the last bits.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`VectorMath.L2Norm`](vectormath-l2norm.md),
[`EmbeddingIndex.Search`](embeddingindex-search.md), [the search index](../search.md).
