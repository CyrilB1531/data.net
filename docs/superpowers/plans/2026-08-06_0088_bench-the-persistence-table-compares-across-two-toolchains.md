# #88 Persistence table toolchain parity — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Re-measure section 4 of `bench/README.md` with both columns on the same harness, and say which of its claims survive — distinguishing the counted allocation figure from the sampled time figures.

**Architecture:** No code change. `--inProcess` on the net10 command, repeated processes on both sides, and the two forward references that point here removed once it lands.

**Tech Stack:** BenchmarkDotNet (`InProcessEmitToolchain`), the persistence benchmarks from #58.

**Spec:** `2026-08-06_0088_bench-the-persistence-table-compares-across-two-toolchains.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `fix/88-persistence-toolchain-parity`. Never commit to `main`.
- **No library change.**
- **Re-take all six rows**, including the five currently dismissed as noise.
- **Repeated processes**, not one run per target.
- Name the machine.

### Reusable verification commands

```bash
cd <repo>

net_in() { dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*Persistence*' --inProcess; }
ns_in()  { dotnet run -c Release --project bench/DataNet.NetStandard.Benchmarks -- --filter '*Persistence*'; }
```

---

### Task 1: Record what the table currently claims, and how much rests on each row

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the list of claims to re-check, so "the numbers moved a bit" cannot
substitute for an answer.

- [x] **Step 1: Read section 4 and write down its conclusion**

Expected: the section rests on `SpieceModel` at **2.4×** and **1.95 MB** of extra
allocation; the other five rows are read as noise.

- [x] **Step 2: Split the conclusion into a counted part and a sampled part**

- **Allocation is counted**, not sampled — not at risk from the toolchain.
- **Time is sampled** — at risk.

- [x] **Step 3: Note why the "noise" rows are the fragile ones**

The differences being dismissed as noise are **the same size as the harness
difference**. They are more likely to move than the headline row.

---

### Task 2: Re-measure, both sides, same harness

**Depends on:** Task 1.

- [x] **Step 1: Run both**

```bash
net_in
ns_in
```

- [x] **Step 2: Repeat in fresh processes, at least three times per side**

BenchmarkDotNet's `±` describes dispersion within one process. #61 saw a 2.64×
move between two runs of the same binary with tight intervals on both.

- [x] **Step 3: Record the across-process spread per row**

That spread is what says whether a row is noise, not the `±` inside one run.

- [x] **Step 4: Confirm the allocation figures are unchanged**

They are counted. If they moved, something other than the toolchain changed and
that needs explaining before anything else is published.

---

### Task 3: Publish, and answer the question the issue asked

**Files:**

- Modify: `bench/README.md`

**Depends on:** Task 2.

- [x] **Step 1: Replace all six rows**

- [x] **Step 2: Say what happened to the conclusion**

Explicitly: does `SpieceModel`'s 2.4× survive, and do any of the five "noise" rows
turn out to be real? Both answers are useful; neither is available without this
branch.

- [x] **Step 3: Distinguish counted from sampled in the text**

Allocation is counted and survives; time is sampled. Presenting both in the same
voice invites equal trust in unequal claims.

- [x] **Step 4: `--inProcess` in the documented command itself**

Not in prose beside it.

---

### Task 4: Remove the two notes that point here

**Files:**

- Modify: `bench/README.md`

**Depends on:** Task 3.

- [x] **Step 1: Section 5's `--inProcess` paragraph**

It says section 4 still carries this defect.

- [x] **Step 2: The closing note of section 2**

Same statement.

**A stale cross-reference is how a fixed problem gets re-reported.** Both must
stop saying it in the same commit that makes them false.

- [x] **Step 3: Gate**

```bash
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "bench/README.md" "docs/**/*.md"
git diff --stat -- src/   # must be empty
```

- [x] **Step 4: Commit**

```bash
git add bench/README.md
git commit -m "Measure the persistence table with one toolchain on both columns"
```
