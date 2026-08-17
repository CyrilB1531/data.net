# VectorMath

The two SIMD primitives dense vector search is built on.

<!-- docs-declaration -->

```csharp
public static class VectorMath
```

**Example** — a dot product and a norm, both exact on these inputs.

```csharp
using Lodestar.Embeddings.Search;

float dot = VectorMath.Dot(new float[] { 1f, 2f, 3f }, new float[] { 4f, 5f, 6f });  // => 32
float norm = VectorMath.L2Norm(new float[] { 3f, 4f });  // => 5
```

**Remarks** — these are public because they are useful on their own, not only because
[`EmbeddingIndex`](embeddingindex.md) needs them. Anything comparing dense `float` vectors wants
the same two operations.

**This type carries the package's one deliberate behavioural split between target frameworks.**
On `net10.0`, [`Dot`](vectormath-dot.md) accumulates through `System.Numerics.Vector<float>`; on
`netstandard2.0` it is a scalar loop, because the span-based `Vector<T>` constructor is not
available there. Both compute the same dot product, and they add the products up **in a different
order** — floating-point addition is not associative, so the two targets can disagree in the last
bits of a long vector. Neither is wrong; they are different roundings of the same sum.

That matters in exactly one place: a score computed on one target and compared for equality
against a score computed on the other. Compare with a tolerance, or compare rankings rather than
scores.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`EmbeddingIndex`](embeddingindex.md), [the search index](../search.md).

## Members

| Member | What it does |
| --- | --- |
| [`VectorMath.Dot`](vectormath-dot.md) | The dot product of two equal-length vectors. |
| [`VectorMath.L2Norm`](vectormath-l2norm.md) | The Euclidean length of a vector. |
