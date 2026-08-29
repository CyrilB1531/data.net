# 0351 — select_benchmarks re-runs a whole harness for one new line

**Issue:** [#0351](https://github.com/CyrilB1531/lodestar/issues/0351) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-20

## Problem

**Reproduced against the exact commit that motivated it**: adding one `Harness.Measure(...)` row to `PersistenceCrossLang.cs` selected **all 15 benchmark classes and all 4 harnesses**, not just persistence.

## Why

`"always"` in `bench/bench-map.json` held the whole `CrossLang/` directory as one unit. But that directory holds **one file per harness**, plus a genuinely shared base (`Harness.cs`), plus one file no harness names at all (`RocParallelBench.cs`, outside the map entirely).

## What was decided

**`Harness.cs` stays in `always`** — every harness depends on it. **Each harness's own cross-language file moves to that harness's `sources`.** The map gets more specific exactly where the directory was lying about coupling, and nowhere else.

## What shipped

The split, and the `_why` note recording that the map is deliberately coarse everywhere else: a benchmark run for nothing costs minutes, one not run hides a regression.
