# Design — #88: the persistence table compares two harnesses

**Date:** 2026-08-06 · **Issue:** #88 · **Branch:** `fix/88-persistence-toolchain-parity` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

Section 4 of `bench/README.md` — the #58 persistence loaders — publishes a net10
vs `netstandard2.0` table whose two columns were produced by **two different
BenchmarkDotNet toolchains**. The same defect #87 fixes in sections 2 and 3.

The `netstandard2.0` project pins `InProcessEmitToolchain` and has to; the net10
column comes from the default out-of-process toolchain. Every ratio in that table
mixes the target framework with the harness that measured it.

## What is actually at stake, row by row

This is the reason the issue is worth writing rather than just re-running a
command.

**Five of the six rows are currently read as noise, and the section's conclusion
rests on the sixth**: `SpieceModel` at 2.4× and 1.95 MB of extra allocation.

- The **allocation** half of that claim is *counted*, not sampled, and is not at
  risk from the toolchain.
- The **2.4× time** figure is at risk.
- **The five "noise" rows are the ones most likely to move**, because the
  differences being dismissed as noise are the same size as the harness
  difference.

So the conclusion could survive intact while five of its supporting rows change
meaning — or the reverse. Neither can be asserted before re-measuring.

## Decisions

### D1 — `--inProcess` on the net10 command; no code change

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*Persistence*' --inProcess
```

The CLI flag maps to the same `InProcessEmitToolchain` the `netstandard2.0`
project pins.

### D2 — Re-take all six rows

Including the five currently dismissed as noise. "Noise" is a claim about
magnitude, and the magnitude is exactly what is in doubt.

### D3 — Repeated processes, not one run per target

As #87 and #61 both concluded. BenchmarkDotNet's `±` describes dispersion within
one process and says nothing about reproducibility across processes.

### D4 — Separate the counted claim from the sampled one in the text

Allocation is counted and survives; time is sampled and does not. A table that
presents both in the same voice invites the reader to trust them equally.

### D5 — Remove the two forward references once this lands

Section 5's `--inProcess` paragraph and the closing note of section 2 both say
section 4 still carries this. **Both must stop saying so** — a stale cross-
reference is how a fixed problem gets re-reported.

## Out of scope

- Sections 2 and 3 (#87).
- Any change to the persistence code.

## What "done" means

All six rows re-measured with a common toolchain over repeated processes; the
counted and sampled claims distinguished; the two notes pointing here removed.
