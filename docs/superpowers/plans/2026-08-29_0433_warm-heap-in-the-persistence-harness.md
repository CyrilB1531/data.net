# Warm heap in the persistence harness — implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** replace the inferred "roughly 20%" in `bench/README.md` §7 with a measured figure for the warm-heap subsidy, learn whether numpy has the same asymmetry, and decide on those numbers whether the harness splits its two directions into separate processes.

**Architecture:** a measurement lot. A new C#-only subcommand runs the load in two states — a process that has saved, and one that has not — because the variable is the process, so it cannot be two rows in one run. A standalone Python script does the same for numpy. Only after both numbers exist is the harness question answered, and the answer may be "no change".

**Tech Stack:** C# on `net10.0`, the existing `Harness`/`BenchCorpus`/`PersistenceBenchmarks` machinery under `bench/`; Python 3.12 with the pinned `tools/requirements.lock.txt`.

**Spec:** [`../specs/2026-08-29_0433_warm-heap-in-the-persistence-harness.md`](../specs/2026-08-29_0433_warm-heap-in-the-persistence-harness.md)

**Issue:** [#433](https://github.com/CyrilB1531/lodestar/issues/433), part of [#429](https://github.com/CyrilB1531/lodestar/issues/429) · **Branch:** `perf/433-warm-heap-measurement`

## Global Constraints

- English everywhere — code, comments, commit messages, PR body. No `feat:`/`fix:` prefix on a commit subject; closing keywords go in the pull-request body only.
- `dotnet build Lodestar.slnx -c Release` treats warnings as errors on both target frameworks, with SonarAnalyzer running in the build.
- Comment budgets: **two lines inline**, eight of prose in XML documentation. No `long-comment:` marker on this branch.
- Every `Console` call under `bench/` carries `// console-print: <reason>` on the call itself or the line **directly** above it. A two-line comment puts the marker too far up and `tools/check_no_console_writeline.py` fails.
- Phases run **round-robin, one round each**, never one phase to completion. ADR 0051 records a first cut that ran them to completion and reported a strict subset at 136.7% of its superset.
- Publish medians **and** spread. Record `uptime`'s load average and name the machine beside every table.
- `git add -N` a new file before running the guards: they see only tracked files.
- Run every lint-job guard on this branch, not on a branch where the file does not exist.

---

### Task 1: `HeapWarmthBench`, the two states

**Files:**

- Create: `bench/Lodestar.Text.Benchmarks/CrossLang/HeapWarmthBench.cs`
- Modify: `bench/Lodestar.Text.Benchmarks/Program.cs:4-5` (the header comment's count) and its subcommand chain
- Test: none. `bench/` carries no test project; Task 5's guards and a manual run are its gate.

**Interfaces:**

- Consumes: `PersistenceBenchmarks.BuildIndex()` → `EmbeddingIndex`, the same 10 000 × 384 index every persistence row uses.
- Produces: `HeapWarmthBench.Run(string[] args)`, dispatched on `args[0] == "heap-warmth"`, with `args[1]` one of `cold` or `warm`.

- [ ] **Step 1: Write the bench**

```csharp
using System.Diagnostics;
using Lodestar.Embeddings.Search;

namespace Lodestar.Text.Benchmarks.CrossLang;

/// <summary>
/// <c>EmbeddingIndex.Load</c> in a process that has saved, against one that has not.
/// </summary>
/// <remarks>
/// The variable is the process, so the two states cannot be two rows in one run: a save warms
/// the heap for everything after it. #433 is the finding, and ADR 0051's consequence section
/// is where it was first seen.
/// </remarks>
internal static class HeapWarmthBench
{
    /// <summary>Timed runs. Odd, so the median is a run rather than a mean of two.</summary>
    private const int Repeats = 9;

    /// <summary>Untimed runs first, to settle the JIT.</summary>
    private const int WarmupRuns = 2;

    public static void Run(string[] args)
    {
        bool warm = args.Length > 1 && args[1] == "warm";
        EmbeddingIndex index = PersistenceBenchmarks.BuildIndex();

        byte[] artifact;
        using (var stream = new MemoryStream())
        {
            index.Save(stream);
            artifact = stream.ToArray();
        }

        // The whole experiment. "warm" saves again before loading, so the load runs on a heap
        // another 20 MB buffer has already grown and committed; "cold" never does.
        if (!warm)
        {
            GC.Collect(2, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        }

        var samples = new List<double>(Repeats);
        long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        for (int run = 0; run < WarmupRuns + Repeats; run++)
        {
            if (warm)
            {
                using var scratch = new MemoryStream(artifact.Length);
                index.Save(scratch);
            }

            long start = Stopwatch.GetTimestamp();
            using var source = new MemoryStream(artifact);
            EmbeddingIndex loaded = EmbeddingIndex.Load(source);
            double ms = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            GC.KeepAlive(loaded);
            if (run >= WarmupRuns)
            {
                samples.Add(ms);
            }
        }

        long allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        samples.Sort();

        // console-print: this subcommand's entire output is these four lines.
        Console.WriteLine($"state           {(warm ? "warm" : "cold")}");
        Console.WriteLine($"load ms         median {samples[Repeats / 2]:F3}  min {samples[0]:F3}  max {samples[^1]:F3}");
        Console.WriteLine($"allocated       {allocated:N0} bytes over {Repeats + WarmupRuns} runs");
        Console.WriteLine($"collections     {GC.CollectionCount(0)}/{GC.CollectionCount(1)}/{GC.CollectionCount(2)}");
    }
}
```

- [ ] **Step 2: Wire the subcommand**

In `Program.cs`, beside the existing `save-phases` branch:

```csharp
if (args.Length > 0 && args[0] == "heap-warmth")
{
    HeapWarmthBench.Run(args);
    return;
}
```

and change the header comment's `Seven entry points` / `six "compare*"/"roc-parallel"/"save-phases" subcommands` to eight and seven.

- [ ] **Step 3: Build and run both states**

```bash
dotnet build bench/Lodestar.Text.Benchmarks -c Release
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks --no-build -- heap-warmth cold
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks --no-build -- heap-warmth warm
```

Expected: two four-line blocks. The `warm` state's `load ms` median should be the lower of the two.

- [ ] **Step 4: Check the experiment is sound before believing it**

The two states must allocate the same **per load**. `warm` allocates more in total because it also saves, so subtract: `warm` should exceed `cold` by about 20 MB × 11 runs.

**If the per-load allocation differs, stop.** The claim under test is that the load does identical work and pays for more of it; a difference means the two states are not running the same load and no timing comparison is valid. Write up what differs and end the task.

- [ ] **Step 5: Run each state three times, alternating**

```bash
for i in 1 2 3; do
  uptime
  dotnet run -c Release --project bench/Lodestar.Text.Benchmarks --no-build -- heap-warmth cold
  dotnet run -c Release --project bench/Lodestar.Text.Benchmarks --no-build -- heap-warmth warm
done
```

Expected: `warm` below `cold` in all three. **If the ordering is not stable across all three, say so and stop** — a figure that does not reproduce is not a figure, and this lot's whole output is one figure.

- [ ] **Step 6: Commit**

```bash
git add bench/Lodestar.Text.Benchmarks/CrossLang/HeapWarmthBench.cs bench/Lodestar.Text.Benchmarks/Program.cs
git commit -m "Measure the load cold against warm, in the two processes that differ"
```

---

### Task 2: the same measurement on the numpy side

**Files:**

- Create: `bench/python/bench_heap_warmth.py`
- Test: none; Step 3 is its gate.

**Interfaces:**

- Consumes: the corpus arithmetic in `bench/python/bench_persistence.py:97` (`build_vectors`), so the byte counts match the C# side.
- Produces: a script taking `cold` or `warm` as `sys.argv[1]` and printing the same four lines Task 1 prints.

- [ ] **Step 1: Write the script**

```python
#!/usr/bin/env python3
"""np.load in a process that has called np.save, against one that has not.

The C# counterpart is HeapWarmthBench. The question is #433's second: if numpy has
no such asymmetry, the published embedding_index_load ratio flatters us and #324's
"furthest behind" framing is understated rather than overstated.
"""

from __future__ import annotations

import io
import resource
import statistics
import sys
import time
from pathlib import Path

sys.path.append(str(Path(__file__).resolve().parent))

import numpy as np  # noqa: E402

from bench_persistence import build_vectors  # noqa: E402

REPEATS = 9
WARMUP = 2


def main() -> None:
    warm = len(sys.argv) > 1 and sys.argv[1] == "warm"
    vectors = build_vectors()
    buffer = io.BytesIO()
    np.save(buffer, vectors)
    payload = buffer.getvalue()

    samples = []
    for run in range(WARMUP + REPEATS):
        if warm:
            np.save(io.BytesIO(), vectors)
        start = time.perf_counter()
        loaded = np.load(io.BytesIO(payload))
        elapsed = (time.perf_counter() - start) * 1000.0
        del loaded
        if run >= WARMUP:
            samples.append(elapsed)

    samples.sort()
    print(f"state           {'warm' if warm else 'cold'}")
    print(f"load ms         median {statistics.median(samples):.3f}  min {samples[0]:.3f}  max {samples[-1]:.3f}")
    print(f"payload         {len(payload):,} bytes")
    print(f"peak rss        {resource.getrusage(resource.RUSAGE_SELF).ru_maxrss:,} KiB")


if __name__ == "__main__":
    main()
```

- [ ] **Step 2: Run both states**

```bash
python bench/python/bench_heap_warmth.py cold
python bench/python/bench_heap_warmth.py warm
```

Expected: two four-line blocks with the same `payload` count on both — 15 360 128 plus numpy's header.

- [ ] **Step 3: Run each three times, alternating, and read the ordering**

```bash
for i in 1 2 3; do uptime; python bench/python/bench_heap_warmth.py cold; python bench/python/bench_heap_warmth.py warm; done
```

Unlike Task 1, **either ordering is a result here.** If `warm` is not faster, numpy has no subsidy and the cross-language ratio is biased in our favour — record that, it is the finding the spec calls the one that would matter most. Only an *unstable* ordering is a non-result.

- [ ] **Step 4: Commit**

```bash
git add bench/python/bench_heap_warmth.py
git commit -m "Ask numpy the same question, since the ratio depends on both sides"
```

---

### Task 3: write the four numbers where the next reader looks

**Files:**

- Modify: `docs/guides/performance.md`, the existing warm-heap section under *The control that was not one*
- Modify: `bench/README.md` §7, *Every load row here is measured on a warmed heap*
- Modify: `bench/README.md`, a command line for the new subcommand beside §8's

**Interfaces:**

- Consumes: the medians from Tasks 1 and 2.
- Produces: nothing code depends on.

- [ ] **Step 1: Replace the inference in `bench/README.md` §7**

It currently reads *"flattered by something on the order of 20%"*, which was inferred from a band of before/after figures. Replace it with the measured ratio, and **say that it replaces an inference and whether the two agree**. A guide that drifts silently to a new number teaches nothing.

- [ ] **Step 2: Add the table to `docs/guides/performance.md`**

Under the existing section rather than a new one, with the machine and the load averages, in the shape §8's tables use:

```markdown
| | cold | warm | ratio |
| --- | ---: | ---: | ---: |
| `EmbeddingIndex.Load` | _ ms | _ ms | _× |
| `np.load` | _ ms | _ ms | _× |
```

- [ ] **Step 3: State the cross-language consequence in words**

If C# has the asymmetry and numpy does not, write that the published `embedding_index_load` ratio flatters us and that #324's "furthest behind" framing is **understated**, in those words. If both have it, write that the ratio is unaffected and why.

- [ ] **Step 4: Document the subcommand**

One short block in `bench/README.md`, matching §8's:

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- heap-warmth cold
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- heap-warmth warm
python bench/python/bench_heap_warmth.py cold
```

- [ ] **Step 5: Commit**

```bash
git add docs/guides/performance.md bench/README.md
git commit -m "Publish the subsidy as measured, in place of the inference"
```

---

### Task 4: decide the harness, and be willing to decide against

**Files:**

- Create: `docs/decisions/00NN-<slug>.md` **only if** the harness changes
- Modify: `bench/README.md` §7 if it does not
- Modify: `bench/bench-map.json` only if a harness entry is added

**Interfaces:**

- Consumes: Tasks 1–3 complete. **Do not start this task before they are.**
- Produces: the answer #433 closes on.

- [ ] **Step 1: Apply the bar**

A split costs a process launch per row **and** the back-to-back pairing `bench/README.md`'s measurement-conditions section argues is what makes the Python and C# rows comparable at all. It is worth that only if the subsidy is large enough to change a reader's conclusion about a published row.

- [ ] **Step 2a: If it splits** — write the ADR, update `bench-map.json`, and rewrite §7's measurement-conditions section, whose pairing argument becomes false and must not be left standing.

- [ ] **Step 2b: If it does not split** — no ADR for the harness. A paragraph in §7 saying the subsidy was measured, how large it is, and why stating it beats splitting. ADR 0052 is the model: a refusal with its measurement attached is a result, and it stops the fourth proposal.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "Decide the harness on the number rather than on the worry"
```

---

### Task 5: the gates, then the pull request

**Files:** none changed by this task except in repair.

- [ ] **Step 1: Build and test**

```bash
dotnet build Lodestar.slnx -c Release
dotnet test Lodestar.slnx -c Release
```

Expected: 0 warnings; **read the test count, not the colour** — a filter that matches nothing exits zero.

- [ ] **Step 2: Format and markdown**

```bash
dotnet format Lodestar.slnx --verify-no-changes
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
```

- [ ] **Step 3: Every lint-job guard, on this branch**

```bash
python tools/check_comment_length.py
python tools/check_no_console_writeline.py
python tools/check_bench_map.py
python tools/check_sample_coverage.py
python tools/check_machine_paths.py --no-environment
python tools/check_version_floor.py
python tools/check_sample_culture.py
python tools/check_adr_immutable.py --base main
python -m pytest tools/tests -q
```

- [ ] **Step 4: A code review before the pull request exists**

The gates read declarations and replay corpora. None of them reads whether a measurement means what its paragraph claims. Review the arithmetic in Task 3's tables specifically.

- [ ] **Step 5: Open the pull request**

Body carries the four numbers, the machine, the load averages, and `Closes #433`.

## Execution log — Task 1, run 2026-08-29

**Task 1 is built and Step 5 stopped the lot**, which is what Step 5 is for.

**Two design faults the plan's own checks caught.**

The first was mine before a line ran: the draft used `GC.Collect` to make the cold state cold,
and SonarAnalyzer's S1215 refused it. The analyser was right for a better reason than it knew —
a process that has already *built and saved* an index to obtain the artifact bytes is not cold,
and no collection makes it so. Hence a `prepare` subcommand: it writes the artifact once, and the
cold process then only reads bytes, having built and saved nothing.

The second was real and would have published a wrong finding. The first warm state saved **before
each load**, and measured cold *faster* than warm, stably, across three rounds — the opposite of
the hypothesis. That is not the subsidy reversing; it is a save inside the measured loop producing
20 MB of garbage per round that competes with the load. `compare-persistence` does not do that: it
measures every save, then every load. Moved the warming saves ahead of the loop and the stable
"result" dissolved.

**Step 4's soundness check passes.** Load allocation is **37 069 648 bytes cold against
37 069 848 warm** — 200 bytes apart on 37 MB. The two states run the same workload on two heaps,
which is the premise the whole comparison rests on.

**Step 5's stability check fails, so there is no figure.**

| round | cold median | warm median | cold min | warm min |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 93.389 ms | 35.686 ms | 13.466 | 12.341 |
| 2 | 40.472 ms | 40.104 ms | 14.114 | 12.117 |
| 3 | 23.854 ms | 30.545 ms | 14.823 | 16.299 |

Warm faster, level, then slower. The median of one state swings **4× between rounds** — 93 to 40
to 24 cold — with p75 values of 116–146 ms against minima of 13. Something on this shared
container takes time in large blocks, and a row that allocates 37 MB per iteration is where it
lands hardest.

**Conditions.** Four cores of an Intel Xeon @ 2.80GHz, .NET 10, a shared cloud container, load
average 0.79–1.27. The same machine ADR 0051 measured on, and the same lesson: shares taken inside
one window transfer, absolutes do not.

**What this leaves.** Tasks 2 to 5 are unchanged and unstarted. The instrument is committed, so
the measurement is one command away from a machine that can hold still — the nightly runner is
where #430's figures were finally obtained after the container withdrew its own. Re-running
`heap-warmth` there, or on the i7-4770S, is what unblocks this lot and therefore #435.

## What this plan does not do

- **It does not optimise the load.** #434 is closed as already done; #435 reuses buffers; #436 memory-maps and is blocked on a format. A subsidy this lot merely measures is one those lots will each change, and fixing it here would take their evidence away.
- **It does not change any artifact, any public API, or any published row's value.** It changes what a published row is *said to mean*.
- **It does not assume the split.** On the spec's evidence the likely outcome is that the subsidy is real, modest, and better stated than engineered around.
