# 0433 — The warm heap in the persistence harness: implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development
> (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Spec:** [`2026-08-29_0433_warm-heap-in-the-persistence-harness.md`](../specs/2026-08-29_0433_warm-heap-in-the-persistence-harness.md) ·
**Issue:** [#433](https://github.com/CyrilB1531/lodestar/issues/433), part of
[#429](https://github.com/CyrilB1531/lodestar/issues/429) ·
**Branch:** `perf/433-warm-heap-measurement`

**Goal:** replace the inferred "roughly 20%" in `bench/README.md` §7 with a measured figure, learn
whether numpy has the same asymmetry, and decide — on those two numbers — whether the harness
splits its two directions into separate processes.

**Architecture:** a measurement lot. The only code that may change is under `bench/`, and it may
change **only if the numbers say so**. `EmbeddingIndex` is not touched.

**Tech Stack:** C# on `net10.0`, the existing `Harness`/`BenchCorpus` machinery, Python 3.12 with
the pinned `tools/requirements.lock.txt`.

## Global Constraints

- English everywhere; no `feat:`/`fix:` prefix on commit subjects; closing keywords in the PR body.
- Comment budgets: **two lines inline**, eight of prose in XML documentation. No `long-comment:`
  marker on this branch — a measurement lot has no block that needs one, and #187 is why the marker
  is not a convenience.
- Every `Console` call under `bench/` carries `// console-print: <reason>` on the call or the line
  **directly** above it. A two-line comment puts the marker too far up and the guard fails.
- Interleave, never campaign. Run the states round-robin one round each: ADR 0051 records a first
  cut that ran phases to completion and reported a strict subset at 136.7% of its superset.
- Publish medians **and** spread. On a shared machine the floor is the honest half of a row.
- Name the machine and record `uptime`'s load average beside every table.
- The four guards see only tracked files: `git add -N` a new file before running them.
- Run every lint-job guard **on this branch**, not on a branch where the file does not exist.

## File Structure

| File | Responsibility |
| --- | --- |
| `bench/Lodestar.Text.Benchmarks/CrossLang/HeapWarmthBench.cs` | create — the cold-vs-warm profile, its own subcommand, no Python pair. `roc-parallel` and `save-phases` are the precedent for a C#-only diagnostic. |
| `bench/Lodestar.Text.Benchmarks/Program.cs` | edit — one more subcommand, and the header comment's count with it. |
| `bench/python/bench_heap_warmth.py` | create — question 2, standalone rather than a row in `bench_persistence.py`: it must run cold, and a row inside that harness cannot. |
| `bench/README.md` | edit — the command, and §7's inferred 20% replaced by the measurement. |
| `docs/guides/performance.md` | edit — the numbers, with the machine and the window. |
| `bench/bench-map.json` | edit **only if** a harness entry is added. A C#-only diagnostic gets none, per `roc-parallel`. |

## Task 1 — the cold measurement, C# side

- [ ] `HeapWarmthBench.Run()` builds the same 10 000 × 384 index `PersistenceBenchmarks.BuildIndex`
      builds, writes one artifact to a byte array, and measures `EmbeddingIndex.Load` over it.
- [ ] Two states, **in separate processes**, because that is the whole variable: `cold` loads
      without having saved; `warm` saves once, then loads. The subcommand takes the state as an
      argument and the runner alternates the two processes, so neither state gets a machine of
      its own.
- [ ] Report allocated bytes with `GC.GetTotalAllocatedBytes` alongside the time. **If the two
      states allocate differently the experiment is wrong** — the whole claim is that the load does
      identical work — and the task stops until that is explained.
- [ ] Report `GC.CollectionCount(0..2)` for the same reason.
- [ ] Verify: run it three times and confirm the cold/warm ordering is stable across all three. If
      it is not, say so and stop; a figure that does not reproduce is not a figure.

## Task 2 — the same measurement, Python side

- [ ] `bench_heap_warmth.py` mirrors task 1: `np.load` from a `BytesIO` over the same 15.36 MB of
      floats, in a process that has called `np.save` and in one that has not.
- [ ] Use the same corpus arithmetic `bench_persistence.py` uses, so the byte counts match.
- [ ] Record `tracemalloc` or `resource.getrusage(RUSAGE_SELF).ru_maxrss` beside the time — the
      Python analogue of "did the two states allocate the same".
- [ ] Verify: three runs, stable ordering, same rule as task 1.

## Task 3 — read the two results together

- [ ] Write the four numbers into `docs/guides/performance.md` with the machine and the load
      averages, under the existing warm-heap section rather than a new one.
- [ ] Replace `bench/README.md` §7's *"flattered by something on the order of 20%"* with what was
      measured. If the measurement disagrees with 20%, **the sentence changes and the change is
      called out**; a guide that quietly drifts to the new number teaches nothing.
- [ ] State the cross-language consequence explicitly. If C# has the asymmetry and numpy does not,
      the published `embedding_index_load` ratio flatters us, and #324's framing is understated —
      say that in the guide, in those words.

## Task 4 — decide the harness, and be willing to decide against

- [ ] Only now, with tasks 1–3 done, answer question 3. The bar: a split is worth a process launch
      per row **and** the loss of the back-to-back pairing only if the subsidy is large enough to
      change a reader's conclusion about a published row.
- [ ] If it splits: an ADR, `bench-map.json` updated, and `bench/README.md`'s measurement-conditions
      section rewritten — the pairing argument there becomes false and must not be left standing.
- [ ] If it does not split: no ADR. A paragraph in `bench/README.md` §7 saying the subsidy was
      measured, how big it is, and why stating it beats splitting. #432 is the model — a refusal
      with its measurement attached is a result, and ADR 0052 keeps it findable.
- [ ] Either way, close #433 with the number, not with a description of the number.

## Task 5 — the gates, then the pull request

- [ ] `dotnet build Lodestar.slnx -c Release` — 0 warnings.
- [ ] `dotnet test Lodestar.slnx -c Release` — read the count, not the colour.
- [ ] `dotnet format Lodestar.slnx --verify-no-changes`.
- [ ] markdownlint over the documented glob.
- [ ] `check_comment_length`, `check_no_console_writeline`, `check_bench_map`, `check_machine_paths
      --no-environment`, `check_sample_coverage`, `check_version_floor`, `check_sample_culture`,
      `check_adr_immutable --base main`, `python -m pytest tools/tests -q`.
- [ ] A code review before the pull request exists. The gates read declarations and replay corpora;
      none of them reads whether a measurement means what its paragraph says it means.

## What this plan does not do

- **It does not optimise the load.** #434 pre-sizes the tf-idf vocabulary dictionary, #435 reuses
  buffers across loads, #436 memory-maps the block. A subsidy this lot merely measures is a subsidy
  those three will each change, and a plan that fixed it here would take their evidence away.
- **It does not change any artifact or any public API.**
- **It does not assume the split.** The most likely outcome, on the evidence in the spec, is that
  the subsidy is real, modest, and better stated than engineered around. A plan that assumed
  otherwise would be doing what #432 was written to stop.
