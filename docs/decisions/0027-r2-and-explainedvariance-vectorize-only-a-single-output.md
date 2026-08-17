# 0027 — R²'s and ExplainedVariance's unweighted accumulation vectorizes only for a single output

**Status:** accepted · **Date:** 2026-08-14

## Context

`R2.AccumulateUnweighted` and `ExplainedVariance.AccumulateUnweighted` both
need two passes over `yTrue`/`yPred` per output column — one to accumulate a
mean, one to accumulate centred squares and residuals — and both offer a
`Vector<double>`-based fast path (`AccumulateUnweightedVectorized`) on
`net10.0`.

That fast path is gated on `outputCount == 1 && Vector.IsHardwareAccelerated`,
not on `Vector.IsHardwareAccelerated` alone. `outputCount == 1` is the only
shape where rows are contiguous in `yTrue`/`yPred`: with more than one
output, column `col` of row `row` sits at `(row * outputCount) + col`, a
strided access that a `Vector<T>` load cannot gather from a `ReadOnlySpan`
without scattering it into a temporary first — not how SIMD is written
elsewhere in this repository ([`VectorMath.Dot`](../reference/embeddings/search/vectormath-dot.md), `Pooling.cs`,
`EmbeddingIndex.Persistence.cs` all vectorize over a single contiguous
span). `outputCount > 1` therefore keeps the scalar loop, which walks the
strided layout directly.

`Vector.IsHardwareAccelerated` is checked independently of the shape
condition, and falls through to the same scalar loop on a runtime where
`Vector<double>` is software-emulated — the same guard `Pooling.cs` and
`EmbeddingIndex.Persistence.cs` check before vectorizing ([`VectorMath.Dot`](../reference/embeddings/search/vectormath-dot.md)
predates this repository checking it explicitly, per
[`0001`](0001-target-framework.md)).

## Decision

`AccumulateUnweighted` vectorizes only when both conditions hold:

```csharp
if (outputCount == 1 && Vector.IsHardwareAccelerated)
{
    AccumulateUnweightedVectorized(...);
    return;
}
```

Every other combination — multiple outputs, or no hardware acceleration —
runs the scalar loop.

## Consequences

- `R2.cs` and `ExplainedVariance.cs` each carry a one-line pointer to this
  record at the guard, instead of restating the reasoning inline in both
  files.
- The vectorized path is a different summation order from the scalar one and
  is not asserted bit-identical to it; that is a separate question, answered
  where `VectorCompensatedSum` is defined
  (`src/DataNet.Metrics/Internal/CompensatedSum.cs`), not here.
- A third caller that needs this shape (single contiguous output, two-pass
  mean-then-residual accumulation) can reuse the same guard rather than
  re-deriving it — nothing today shares the accumulation itself, only the
  condition under which it vectorizes.
