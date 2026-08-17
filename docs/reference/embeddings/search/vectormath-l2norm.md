# VectorMath.L2Norm

The Euclidean length of a vector.

<!-- docs-declaration -->

```csharp
public static float L2Norm(ReadOnlySpan<float> v)
```

**Parameters** — `v` is the vector to measure. A `float[]` converts implicitly.

**Returns** — `float`, the square root of the sum of squares. Never negative; `0` only for the
all-zero vector.

**Exceptions** — none for a well-formed vector.

**Example** — the 3-4-5 triangle, and a unit vector proving itself one.

```csharp
using Lodestar.Embeddings.Search;

float length = VectorMath.L2Norm(new float[] { 3f, 4f });  // => 5
float unit = VectorMath.L2Norm(new float[] { 0f, 1f });  // => 1
```

**Remarks** — this is `Dot(v, v)` under a square root, and it is implemented as exactly that, so
it inherits [`Dot`](vectormath-dot.md)'s SIMD path and its accumulation order.

Dividing a vector by its norm is what makes it a unit vector, which is what makes
[`VectorMath.Dot`](vectormath-dot.md) a cosine similarity.
[`EmbeddingIndex`](embeddingindex.md) does that for you on insertion unless told not to — there is
rarely a reason to normalize by hand before adding.

**A zero vector has norm `0`**, and dividing by it is undefined. The index handles this by leaving
such a vector unnormalized rather than producing `NaN`; a caller normalizing by hand has to make
the same decision.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`VectorMath.Dot`](vectormath-dot.md), [`VectorMath`](vectormath.md),
[the search index](../search.md).
