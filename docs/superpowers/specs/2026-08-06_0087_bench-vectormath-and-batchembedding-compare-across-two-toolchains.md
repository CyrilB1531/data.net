# Design — #87: the `VectorMath` and `BatchEmbedding` tables compare two harnesses

**Date:** 2026-08-06 · **Issue:** #87 · **Branch:** `fix/87-benchmark-toolchain-parity` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

Sections 2 (`VectorMath`) and 3 (`BatchEmbedding`) of `bench/README.md` publish
net10 vs `netstandard2.0` ratios whose two columns were **not measured the same
way**.

`DataNet.NetStandard.Benchmarks/Program.cs` pins
`Job.Default.WithToolchain(InProcessEmitToolchain.Instance)` — and it has to:
BenchmarkDotNet's default toolchain generates and builds its own project per run,
re-resolves the `ProjectReference` and silently restores the net10.0 build, so
both suites would measure the same assemblies while looking correct (this is #10's
finding).

The net10 side, however, runs through `DataNet.Text.Benchmarks` with the
**default out-of-process toolchain**. Every ratio in those two sections therefore
mixes the target framework with the harness that measured it.

## Why this is not hypothetical

Found while re-measuring #61. The metrics tier had the same defect and produced a
`MatrixWeighted` gap of **1.06×–1.18×** that looked systematic across all six
shapes. **It disappeared** once the toolchain was made common to both sides and
the runs repeated.

Section 5 is now measured and documented with the confound lifted. Sections 2 and
3 are not.

## Decisions

### D1 — Add `--inProcess` to the net10 command; no code change

The BenchmarkDotNet CLI flag maps to `InProcessEmitToolchain`, the same toolchain
the `netstandard2.0` project pins.

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*VectorMath*' --inProcess
```

### D2 — Re-take every number in both sections

A published ratio produced under the confound cannot be partially trusted. All of
them are re-measured, not only the ones that look suspicious.

### D3 — Repeated processes, not a single run per target

BenchmarkDotNet's `±` margin describes dispersion **within one process** and says
nothing about reproducibility across processes.

In the metrics tier the net10 side moved by up to **2.64× between two runs of the
same binary over the same corpus** at the smallest size, with tight intervals on
both. Small-input rows here deserve the same treatment before any figure is
quoted.

### D4 — Say what is likely real and still not established

`VectorMath.Dot`'s 4.6×–5.6× gap is very likely real — it comes from a
`Vector<T>` SIMD path `netstandard2.0` genuinely does not have. **Its magnitude
is not established until both columns share a harness.**

Stating both halves is the point: a plausible mechanism is not a measurement.

## Out of scope

- Section 4, the persistence table, which has the identical defect and gets its
  own issue (#88) so the two can be reviewed separately.
- Any change to the library.

## What "done" means

Both sections re-measured with a common toolchain, over repeated processes; the
numbers replaced; `bench/README.md` documenting `--inProcess` as part of the
command, so the confound cannot return through a copy-pasted invocation.
