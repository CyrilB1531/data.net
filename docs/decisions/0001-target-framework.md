# 0001 — Target framework: `net10.0`

**Status:** accepted · **Date:** 2026-08-01 · **Revised:** 2026-08-01

## Context

The brief suggested `net8.0` (LTS). The development / CI machine has the **.NET 10
runtime**. After weighing it, the chosen target is explicitly **`net10.0`**.

## Decision

- The `DataNet.Text` library **and** the executable projects (tests, benchmarks)
  target **`net10.0`**.
- The `RollForward` that let a `net8.0` host run on the 10 runtime is **removed**:
  it's no longer needed since we target the runtime that's present.
- `netstandard2.0` **not** added: no .NET Framework / Unity consumer identified.
  Reconsider if such a need appears (cost: polyfills for `Span`, `ArrayPool`, etc.).

## Consequences

- Building and packaging target **`net10.0+`** consumers. This is a deliberate
  choice favoring the latest APIs and runtime performance, at the price of a
  narrower reach than `net8.0` LTS. If an LTS-8 consumer appears, add `net8.0` to
  `TargetFrameworks` (multi-targeting) rather than downgrading.
- The behavior under test is that of the 10 runtime, identical to the targeted
  production runtime — no test/prod gap introduced by roll-forward.
- CI installs the **10.0.x** SDK.
