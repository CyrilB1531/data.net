# Design — #10: benchmark the net10 build against the netstandard2.0 build

**Date:** 2026-08-04 · **Issue:** #10 · **Branch:** `feat/10-netstandard-benchmark` ·
**Checkout:** `<repo>`

## Problem

Issue #1 shipped a second target framework and stated, in ADR 0001, that the two builds
differ — `VectorMath.Dot` keeps `Vector<T>` SIMD on net10 and falls back to a
scalar loop on `netstandard2.0`. Nothing measures that difference, so the cost of
the second target is unknown, and a consumer choosing between frameworks has
nothing to choose on.

## The trap this issue exists for

An earlier attempt already produced numbers: a **4 % difference** between the two
builds. That number is wrong, and the way it is wrong is the whole point of this
design.

`SetTargetFramework` on the `ProjectReference` **is not sufficient on its own.**
BenchmarkDotNet's default toolchain generates and builds its own project per run,
re-resolves the reference and restores the net10 build. Both suites then measure
the same assemblies while reporting entirely plausible numbers.

4 % is exactly the kind of result nobody questions. It is small, it has the right
sign, and it looks like the sort of thing a JIT difference would produce.

## Decisions

### D1 — One suite, two builds: link the sources, swap only the reference

`bench/DataNet.NetStandard.Benchmarks` links the **same** benchmark sources as the
existing suite and changes only which assemblies it references. Same shape as the
mirror test projects: no second copy to keep in sync, so the two cannot drift.

### D2 — In-process toolchain

No generated project, so the benchmarks run against what the process actually
loaded. This is the fix for the trap above, and it is the reason the change is not
a one-line `SetTargetFramework`.

### D3 — A pre-flight assertion on the loaded assemblies, exiting non-zero

Reading `TargetFrameworkAttribute` off the loaded assemblies and printing it:

```text
// DataNet.Text: .NETStandard,Version=v2.0
// DataNet.Embeddings: .NETStandard,Version=v2.0
```

**An isolation failure is invisible in the numbers unless you already know what to
expect.** So it is checked rather than eyeballed, and a mismatch fails the run
instead of producing a plausible table.

### D4 — Benchmark `Dot` and `L2Norm` specifically

They are where the two builds *deliberately* differ. A benchmark suite that
averages over code paths which are byte-identical on both targets measures
nothing and hides the one place there is something to see.

### D5 — Sanity-check the result against physics, not against expectation

With isolation working, `VectorMath.Dot` on an Intel i7-4770S, .NET 10.0.110:

| Dimension | net10 | netstandard2.0 | cost |
| --- | --- | --- | --- |
| 384 | 73.2 ns | 338.5 ns | **4.6×** |
| 768 | 130.9 ns | 679.8 ns | **5.2×** |
| 1024 | 163.6 ns | 912.1 ns | **5.6×** |

That is what losing SIMD should look like. And 912 ns for 1024 floats is
~0.89 ns per element, consistent with a latency-bound scalar accumulator.

**The old 173 ns was never physically plausible for scalar code** — that is what
gave the bug away, and it is the check worth repeating: before quoting a benchmark
result, ask whether the number is achievable by the code you think ran.

### D6 — `bench/README.md` records how to tell if isolation breaks again

The numbers, the machine, and the assertion output to expect. A future reader
seeing 4 % must be able to recognise it as the failure mode rather than as a
result.

## Out of scope

- Optimising the `netstandard2.0` path. This measures; it does not improve.
- A CI performance gate (#11).

## What "done" means

Both suites run; the pre-flight assertion prints `.NETStandard,Version=v2.0` for
the netstandard suite and fails the run otherwise; `bench/README.md` carries the
figures and the machine; build clean on both frameworks.
