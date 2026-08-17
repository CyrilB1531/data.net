# 0033 — `CompensatedSum` is Neumaier's variant, and its SIMD lanes are not bit-identical to it

**Status:** accepted · **Date:** 2026-08-14

## Context

Issue #127 found that a plain sequential `+=` mean can drift far enough from
numpy's pairwise summation to fail the oracle corpora's 1e-9 comparison, on an
ill-conditioned target — a large offset over a small spread. Measured on
`CompensatedSumTests.IllConditioned()` (offset `1e9`, spread `1e-2`,
`n = 200 000`): the sequential mean lands `5.0e-3` away from the exact one, 50%
of the range the data occupies, and `R2`/`ExplainedVariance` centre on that
mean before squaring, so the error does not stay put — it is amplified into
the metric itself.

## Decision

`CompensatedSum` compensates every addition with Neumaier's variant, and
`VectorCompensatedSum` (`net10.0` only, one partial sum per SIMD lane) mirrors
it per lane.

### Neumaier's branch, not Kahan's

Kahan's correction is computed against the running sum's own scale and is
swamped whenever an incoming term is larger in magnitude than the running
total — a known weakness of the algorithm in general. Neumaier's branch
removes that failure mode unconditionally, by comparing magnitudes and
correcting against whichever operand is larger, and is the only difference
between the two algorithms.

That failure mode is not, in fact, reachable on the shape `CompensatedSum`
exists for: measured on the same `IllConditioned()` fixture, plain Kahan and
Neumaier both land within about `1e-16` relative of the decimal reference —
because after the first addition the running sum near `1e9` dominates every
later term, and an incoming term never again exceeds it. Neumaier is still the
right choice: it costs nothing extra over Kahan, and its correctness does not
depend on the data staying shaped the way it is today — a caller who adds the
large term last would reach exactly the failure mode Kahan has and Neumaier
does not.

### The compensation cannot be reassociated away

`(sum - total) + value`, as written in `CompensatedSum.Add`, is not fragile in
the way it would be in C: .NET does not reassociate floating-point arithmetic
— there is no fast-math switch — so the compiler and the JIT are both required
to evaluate it as written. A reader arriving from a language where it can
should not "simplify" it.

### `VectorCompensatedSum`'s lanes are not bit-identical to `CompensatedSum`

Each lane runs Neumaier's exact formula independently, so within a lane the
result is exactly as exact as the scalar type it mirrors. What is not the same
is the *order* the terms arrive in: a caller that batches
`Vector<double>.Count` consecutive elements per `Add` call has lane 0 summing
elements `0, W, 2W, …` while lane 1 sums `1, W+1, 2W+1, …` — a different
association of the same terms than a sequential scalar loop uses, and
`Reduce` combines the lanes in a further step on top of that. Two
mathematically valid summation orders of the same finite-precision terms are
not guaranteed to round to the same `double`, so a metric computed this way
and the same metric computed by `CompensatedSum` alone are not guaranteed to
be bit-identical — both are Neumaier-compensated and both pass the oracle
corpora's 1e-9 comparison, but the guarantee of an exact match, not the match
itself, is what is withdrawn. This repository asserts bit-identity elsewhere
([`Pooler.MeanPoolBatch`](../reference/embeddings/pooling/pooler-meanpoolbatch.md)); it is deliberately not asserted here. The
`net10.0`/`netstandard2.0` split this type exists for is recorded in
[`0001`](0001-target-framework.md).

## Consequences

- `Internal/CompensatedSum.cs`'s two doc blocks carry short pointers to this
  record instead of restating the measurements and the Kahan-versus-Neumaier
  argument inline.
- Verified by `CompensatedSumTests.IllConditioned()` and by the frozen oracle
  corpora every regression metric that accumulates through `CompensatedSum` or
  `VectorCompensatedSum` replays, compared at `MetricsCorpus.Tolerance`
  (`1e-9`).
- A third compensated-summation path added later should re-derive whether
  Kahan's failure mode is reachable on its own data shape before assuming
  Neumaier is optional overhead — the answer here is shape-specific, not
  general.
