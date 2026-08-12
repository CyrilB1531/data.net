# 0001 — Target frameworks: `net10.0` and `netstandard2.0`

**Status:** accepted · **Date:** 2026-08-01 · **Revised:** 2026-08-04

## Context

The brief suggested `net8.0` (LTS). The development / CI machine has the **.NET 10
runtime**, so the original decision was `net10.0` only, with `netstandard2.0`
explicitly declined on the grounds that no .NET Framework / Unity consumer had
been identified.

That reasoning has been revisited. The packages are published to nuget.org, where
the consumer is unknown by definition — "no consumer identified" reflects not
having shipped yet, rather than evidence of absence. `netstandard2.0` widens reach
to .NET Framework 4.6.1+, Mono, Xamarin and Unity for a bounded, one-off cost.

## Decision

- The three libraries multi-target **`net10.0;netstandard2.0`**.
- Executable projects (tests, benchmarks) stay on **`net10.0`**: `netstandard2.0`
  is a contract, not a runtime, and nothing executes on it.
- The net10 fast paths are **unchanged**. `netstandard2.0` reaches equivalent
  behavior through conditional compilation, never a reduced public API — the two
  builds expose the same surface.
- Gaps are closed in three ways, in order of preference: PolySharp compile-time
  polyfills; the `System.Memory` and `System.Numerics.Vectors` packages, referenced
  only on that target; and hand-written fallbacks where neither applies.
- The `RollForward` that let a `net8.0` host run on the 10 runtime stays removed.

## Consequences

- **Several `net10.0`-only `Vector<T>` paths exist in this codebase for the
  same reason** — the span-based `Vector<T>` constructor is net-only, so each
  falls back to a scalar loop on `netstandard2.0` under `#if NET5_0_OR_GREATER`
  — `VectorMath.Dot` among them. `R2` and `ExplainedVariance` add one more, for
  their single-output, unweighted accumulation (issue #127 task 7):
  `VectorCompensatedSum` keeps one Neumaier partial sum per SIMD lane, reduced
  to one `CompensatedSum` at the end.
  - Unlike the repository's other `Vector<T>` paths, **this one's two targets
    are not guaranteed to be bit-identical.** Lane-wise summation can reorder
    the additions relative to the scalar loop (lane 0 sums elements 0, W, 2W,
    … while lane 1 sums 1, W+1, …), so the `net10.0` and `netstandard2.0`
    results for `R2`/`ExplainedVariance` on this path can differ in the last
    few bits. Both stay Neumaier-compensated and both pass the oracle corpora,
    which compare at `1e-9`. This is narrower than the bit-identity
    `Pooler.MeanPoolBatch` asserts elsewhere in the codebase — a reader should
    not assume that guarantee extends here.
- A handful of net-only conveniences are replaced by portable equivalents:
  `ArgumentNullException.ThrowIfNull` (behind a shared `Guard.NotNull`, so a single
  `#if` covers every call site instead of one per site), `string.Join(char, …)`,
  `Array.Fill`, `MathF`, `CollectionsMarshal`, `.Order()`, `KeyValuePair`
  deconstruction, array range operators, and the `string` char overloads.
- **Both builds are behavior-verified.** Three mirror test projects replay the
  entire suite against the netstandard2.0 assemblies, on the net10 runtime, so the
  assemblies shipped to .NET Framework, Mono and Unity consumers are executed and
  not merely compiled. The test sources are linked rather than copied, so the two
  runs can never drift apart.

  Each mirror asserts the `TargetFrameworkAttribute` of the assembly under test
  before anything else. Without that, a reference quietly resolving back to
  net10.0 would leave every test passing while proving nothing — verified by
  removing the isolation and watching the guard fail.
- CI installs the **10.0.x** SDK, which builds both targets.
- If an LTS-8 consumer appears, add `net8.0` to `TargetFrameworks` rather than
  downgrading anything.
