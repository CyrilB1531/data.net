# Mmr.Select

Selects up to `count` candidates.

<!-- docs-declaration -->

```csharp
public static int[] Select(ReadOnlySpan<float> query, IReadOnlyList<float[]> candidates, int count, double lambda = 0.5)
```

**Parameters** — `query` is what relevance is measured against. `candidates` are the candidate
vectors, all of `query`'s length. `count` is how many to select; more than there are selects them
all. `lambda` is `1` for pure relevance, `0` for pure diversity.

**Returns** — `int[]`, the chosen indices **in selection order** — not re-sorted by score
afterwards.

**Exceptions** — `ArgumentNullException` when `candidates` is null. `ArgumentOutOfRangeException`
when `count` is negative, or `lambda` is outside `[0, 1]`. `ArgumentException` when a candidate is
null, of a different length than `query`, or has a zero or non-finite norm — cosine is undefined in
either case, and the same check applies to `query` itself.

**Example** — asking for more candidates than exist returns them all, once each.

```csharp
using Lodestar.Embeddings.Search;

float[] query = [1f, 0f, 0f];
float[][] candidates =
[
    [1.00f, 0.00f, 0.00f],
    [0.80f, 0.60f, 0.00f],
    [0.60f, 0.00f, 0.80f],
    [0.00f, 1.00f, 0.00f],
];

int[] chosen = Mmr.Select(query, candidates, count: 99);
int returned = chosen.Length;  // => 4
```

**Remarks** — the default `lambda`, `0.5`, weighs relevance and diversity equally. A zero-vector,
`NaN` or infinite norm is refused rather than treated as a degenerate cosine of zero, on either
`query` or any candidate — a silent zero would rank that candidate as neither similar nor
dissimilar to anything, which is not what an undefined value means.

[`VectorMath.Dot`](vectormath-dot.md) sums in a different order on `net10.0` (SIMD) than on
`netstandard2.0` (scalar), so a genuine near-tie between two candidates can select a different
index on the two targets — accepted, not a defect, and the same divergence
[`VectorMath`](vectormath.md) already documents for the dot product itself.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Mmr`](mmr.md), [`VectorMath.Dot`](vectormath-dot.md),
[the search index](../search.md), the [Python equivalence table](../../../equivalence.md).
