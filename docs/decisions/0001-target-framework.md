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

- **One deliberate performance difference.** `VectorMath.Dot` takes a
  `Vector<T>` SIMD path under `#if NET5_0_OR_GREATER` and falls back to a scalar
  loop on `netstandard2.0`, because the span-based `Vector<T>` constructor is
  net-only. Everything else compiles to equivalent IL.
- A handful of net-only conveniences are replaced by portable equivalents:
  `ArgumentNullException.ThrowIfNull` (behind a shared `Guard.NotNull`, so a single
  `#if` covers every call site instead of one per site), `string.Join(char, …)`,
  `Array.Fill`, `MathF`, `CollectionsMarshal`, `.Order()`, `KeyValuePair`
  deconstruction, array range operators, and the `string` char overloads.
- **The netstandard2.0 build is compile-verified, not behavior-verified.** The
  test suite targets `net10.0`, so it exercises the net10 assemblies; the oracle
  corpora currently say nothing about the netstandard2.0 build's runtime behavior.
  Since the two differ by conditional compilation, that is a real gap, tracked
  separately. Do not read "158 tests pass" as covering both targets.
- CI installs the **10.0.x** SDK, which builds both targets.
- If an LTS-8 consumer appears, add `net8.0` to `TargetFrameworks` rather than
  downgrading anything.
