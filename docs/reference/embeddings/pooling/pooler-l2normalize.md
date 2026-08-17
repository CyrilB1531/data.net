# Pooler.L2Normalize

Scales a vector in place to unit L2 norm.

<!-- docs-declaration -->

```csharp
public static void L2Normalize(Span<float> vector)
```

**Parameters** — `vector` is the vector to scale, modified **in place**. A `float[]` converts implicitly.

**Returns** — nothing — the argument is the result.

**Exceptions** — none. A zero vector is left alone rather than producing `NaN`.

**Example** — the 3-4-5 triangle, scaled to unit length.

```csharp
using Lodestar.Embeddings.Pooling;

float[] v = { 3f, 4f };
Pooler.L2Normalize(v);

float x = v[0];  // => 0.6
float y = v[1];  // => 0.8
```

**Remarks** — **This is the one place in the package that refuses to vectorize on purpose.** The sum of squares
is accumulated in `double` and computed by a scalar loop: a `Vector<float>` accumulator would lose
precision, and it would make the answer depend on the SIMD width of the machine that ran it. The
scaling pass afterwards is exact whichever way it is done, so that half **is** vectorized.

The consequence is worth stating plainly: a pooled vector is **bit-identical** across
`net10.0`, `netstandard2.0`, and machines with different vector widths. That is the opposite trade
from `VectorMath.Dot`, where the SIMD accumulation is the point and the last bits may differ.

A **zero vector is a no-op**, not a `NaN`. That matters because an all-padding sequence pools to
zero, and a `NaN` there would spread into every score it ever touched.

Matches `torch.nn.functional.normalize(v, p=2, dim=1)`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Pooler.MeanPoolAndNormalize`](pooler-meanpoolandnormalize.md), [`Pooler`](pooler.md),
[the pooling index](../pooling.md).
