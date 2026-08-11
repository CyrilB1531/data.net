# #87 Benchmark toolchain parity — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make sections 2 and 3 of `bench/README.md` measure the target framework rather than the target framework plus the harness — and re-take every number under a common toolchain, over repeated processes.

**Architecture:** No code change. `--inProcess` on the net10 command maps to the same `InProcessEmitToolchain` the `netstandard2.0` project pins. The work is measurement and documentation, and the documentation includes the flag in the command so the confound cannot return through a copy-paste.

**Tech Stack:** BenchmarkDotNet (`InProcessEmitToolchain`), the two benchmark projects.

**Spec:** `2026-08-06_0087_bench-vectormath-and-batchembedding-compare-across-two-toolchains.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `fix/87-benchmark-toolchain-parity`. Never commit to `main`.
- **No library change.** This branch corrects a measurement.
- **Do not keep any old number.** A ratio produced under the confound cannot be
  partially trusted.
- **Repeated processes.** A single run per target is not a result.
- Name the machine, per `CONTRIBUTING.md`.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

net_in()  { dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter "$1" --inProcess; }
ns_in()   { dotnet run -c Release --project bench/DataNet.NetStandard.Benchmarks -- --filter "$1"; }
```

---

### Task 1: Confirm the two columns really were measured differently

**Files:** none modified.

**Depends on:** nothing.

- [x] **Step 1: Read the toolchain the netstandard project pins**

```bash
grep -n "Toolchain\|InProcess" bench/DataNet.NetStandard.Benchmarks/Program.cs
```

Expected: `Job.Default.WithToolchain(InProcessEmitToolchain.Instance)` — and it
*has* to be there. The default toolchain generates its own project, re-resolves
the `ProjectReference` and silently restores the net10.0 build (#10's finding).

- [x] **Step 2: Read the command that produced the net10 column**

```bash
grep -n -B3 -A3 "DataNet.Text.Benchmarks" bench/README.md | head -40
```

Expected: no `--inProcess`. **Default out-of-process toolchain.**

- [x] **Step 3: Note the precedent that makes this worth doing**

The metrics tier had the identical defect and produced a `MatrixWeighted` gap of
1.06×–1.18× that looked systematic across all six shapes — and **disappeared**
once the harness was made common. A systematic-looking small ratio is exactly what
this confound produces.

---

### Task 2: Re-measure section 2 — `VectorMath`

**Files:** none modified yet.

**Depends on:** Task 1.

- [x] **Step 1: Both sides, same harness**

```bash
net_in '*VectorMath*'
ns_in  '*VectorMath*'
```

- [x] **Step 2: Repeat each, in fresh processes**

At least three runs per side. BenchmarkDotNet's `±` describes dispersion **within
one process** and says nothing about reproducibility across them — in the metrics
tier the net10 side moved by up to **2.64×** between two runs of the same binary,
with tight intervals on both.

- [x] **Step 3: Report the across-process spread, not only the mean**

Especially for the small-input rows, which are the ones that move.

- [x] **Step 4: Compare against the published figure and say what changed**

`Dot`'s 4.6×–5.6× is very likely real — `netstandard2.0` genuinely has no
`Vector<T>` SIMD path. Whether the **magnitude** survives is the open question.

---

### Task 3: Re-measure section 3 — `BatchEmbedding`

**Depends on:** Task 2.

- [x] **Step 1: Both sides, same harness, repeated**

```bash
net_in '*BatchEmbedding*'
ns_in  '*BatchEmbedding*'
```

- [x] **Step 2: Watch for ratios in the 1.0–1.2 band**

That is the size of the harness difference itself. Any conclusion resting on a
gap that small was resting on the confound.

---

### Task 4: Publish, so the confound cannot come back

**Files:**

- Modify: `bench/README.md`
- Modify: `docs/guides/performance.md` (the batched-embedding comparison)

**Depends on:** Task 3.

- [x] **Step 1: Replace every number in both sections**

- [x] **Step 2: Put `--inProcess` in the documented command itself**

Not in a note beside it. The next person re-measures by copying the command, and
a flag that lives in prose is a flag that gets dropped.

- [x] **Step 3: Say what the `±` does and does not cover**

One sentence, next to the table. It is the sentence that would have prevented
this.

- [x] **Step 4: Note that section 4 still carries the defect**

Persistence, tracked as its own issue. Say so plainly, and remove the note when
that lands.

- [x] **Step 5: Gate**

```bash
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "bench/README.md" "docs/**/*.md"
git diff --stat -- src/   # must be empty
```

- [x] **Step 6: Commit**

```bash
git add bench/README.md docs/guides/performance.md
git commit -m "Measure both columns with the same toolchain"
```
