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

> **#321 update: the same two conditions now govern the shared walk.** This decision
> was written about `R2` and `ExplainedVariance`, which carry their own accumulation.
> `Outputs.WeightedMean` — the walk `mse`, `mae` and `RootMeanSquaredError` take —
> kept a scalar loop, and the nightly found the cost of that: **0.60× against numpy**
> at a million rows on a runner with AVX-512, below the gate
> [`../guides/performance.md`](../guides/performance.md) sets, while `r2` on the same
> run stayed above it. The published table said the same thing more quietly — `r2`
> cost less doing *two* passes than `mse` doing one.
>
> `Outputs.ScoreVectorized` applies this decision's rule unchanged: `outputCount == 1`
> for contiguity, `Vector.IsHardwareAccelerated` checked apart from it. Measured
> 1.65× on `mse` and 1.60× on `mae`, with `r2` re-run as an untouched control.
>
> **What is new is which kernels may take it.** `IResidualKernel` gained a sibling,
> `IVectorResidualKernel`, rather than a second method: four of the six kernels cannot
> have a lane-wise form at all — the Tweedie deviances reach `Math.Pow` and the log
> errors `Math.Log`. A single interface would have forced four implementations that
> could only throw.
