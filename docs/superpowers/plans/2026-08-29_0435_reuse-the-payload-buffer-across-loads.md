# 0435 — Reuse the payload buffer across loads: implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development
> (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Spec:** [`2026-08-29_0435_reuse-the-payload-buffer-across-loads.md`](../specs/2026-08-29_0435_reuse-the-payload-buffer-across-loads.md) ·
**Issue:** [#435](https://github.com/CyrilB1531/lodestar/issues/435), part of
[#429](https://github.com/CyrilB1531/lodestar/issues/429) ·
**Branch:** `perf/435-reuse-the-payload-buffer`

**Goal:** decide, on measurement, whether the transient artifact payload is rented from
`ArrayPool<byte>.Shared` rather than allocated — trading 20.5 MB allocated per load against
33.5 MB held by the pool for the life of the process.

**Architecture:** the payload only. `JsonArtifact.ReadAllBytes` and its async twin change shape so
the buffer's lifetime is owned by the caller that consumes it; the vector block is untouched,
because it becomes the index's backing store and outlives the call.

**Blocked on:** [#433](https://github.com/CyrilB1531/lodestar/issues/433). Its lot measures the
warm-heap subsidy directly, and that number is the ceiling on what this can return. **Do not start
task 3 before it lands** — sizing a lever against an inferred figure is what #432 exists to stop.

## Global Constraints

- English everywhere; no `feat:`/`fix:` prefix; closing keywords in the PR body only.
- Comment budgets: two lines inline, eight of prose in XML documentation.
- Both target frameworks, warnings as errors, SonarAnalyzer in the build. `ArrayPool<T>` exists on
  `netstandard2.0` through `System.Buffers`, which is already referenced there.
- The exposure invariant below is not a code-review preference. Treat a failure of it as a
  security defect.
- Interleave the before/after; publish medians and spread; name the machine.
- Run every lint-job guard on this branch.

## File Structure

| File | Responsibility |
| --- | --- |
| `src/Shared/Persistence/JsonArtifact.cs` | edit — `ReadAllBytes`/`ReadAllBytesAsync` return a rented buffer with an owner, not a bare `ReadOnlyMemory<byte>`. |
| `src/Shared/Persistence/Buffers.cs` | edit — the rent/return helper and the one place the exact-length slice is taken. |
| `src/Lodestar.Embeddings/Search/EmbeddingIndex.Persistence.cs` | edit — `Load(Stream)` returns the buffer in a `finally`. |
| `src/Lodestar.Text/Vectorization/*.Persistence.cs` | edit — the same, if and only if the measurement says the smaller artifacts benefit. Most likely not: they sit below the LOH threshold. |
| `tests/Lodestar.Embeddings.Tests/Persistence/PooledPayloadTests.cs` | create — the leak-across-loads test. |

## Task 1 — prove the payload escapes nowhere

- [ ] Read every consumer of `ReadAllBytes`/`ReadAllBytesAsync` and confirm each one's output holds
      no slice of the input: ids arrive as new `string`s from `Utf8JsonReader`, floats are decoded
      into their own array.
- [ ] `EmbeddingIndex.Load(ReadOnlyMemory<byte>)` is the case to think hardest about — it parses
      **caller** memory in place, so it must keep doing exactly that and must never return a
      caller's buffer to a pool.
- [ ] If any consumer does retain a slice, **stop and write it up**. The lot is then a different
      lot, and pooling that path would hand one caller another's bytes.

## Task 2 — the ownership shape, and the invariant

- [ ] Give the rented buffer an owner with a `Dispose` that returns it, so no call site can forget.
      `ArtifactIo.SaveWithBlock` is the precedent: own the whole sequence in one place rather than
      handing every artifact a thing to be careful with (ADR 0051).
- [ ] Slice to the **exact** byte count read. Never hand out `buffer` where `buffer.AsMemory(0, n)`
      is meant: the pool returns an array at least as long as asked, and here the tail may hold the
      previous index.
- [ ] `PooledPayloadTests`: load index A, load a *different* index B through the same pooled path,
      and assert B's vectors and ids contain nothing of A's. Make A and B differ in length so B is
      the shorter — that is the case where a lazy slice exposes A's tail.
- [ ] Verify: run the new test against a deliberately broken slice (`buffer` instead of
      `buffer.AsMemory(0, n)`) and confirm it **fails**. A test that has never failed proves nothing.
- [ ] Full suite on both target frameworks.

## Task 3 — measure, once #433 has landed

- [ ] Before/after on `embedding_index_load` and `embedding_index_load_file`, interleaved, enough
      runs to separate from the 6.9–7.9 ms class of spread the guide records.
- [ ] Carry an allocation-free control; a load row is not a control for a load change.
- [ ] Record **residency**, not only time: `GC.GetTotalAllocatedBytes` per load, and the pool's
      retained size. The spec's trade is 20.5 MB transient against 33.5 MB resident and the table
      must show both columns.
- [ ] Measure the *second* load in a process as well as the first. The first pays the commit either
      way; if only the second gains, say so, because a caller who loads one index gains nothing and
      pays the residency.

## Task 4 — decide, and be willing to refuse

- [ ] The bar: the time saving must be worth 33.5 MB held for the life of the process **for a
      caller who loads once**, which is the ordinary case for an embedding index.
- [ ] If it clears: ship, with the residency stated in `docs/guides/performance.md` and the
      embeddings guide, and an ADR for the trade.
- [ ] If it does not: revert the code, keep the measurement, and write the ADR anyway. ADR 0052 is
      the model — a refusal with its measurement attached is a result, and it stops the fourth
      proposal.
- [ ] Either way `bench/README.md` gains nothing: this adds no row. Say so rather than leaving a
      reader to wonder.

## Task 5 — the gates, then the pull request

- [ ] Build, test, format, markdownlint, and the full guard set on this branch.
- [ ] A code review before the pull request exists, reading the slice arithmetic specifically.

## What this plan does not do

- **It does not pool the vector block.** That array becomes `_data` and outlives the call; returning
  it is a use-after-free the runtime will not complain about. #436 is where the block's own
  lifetime gets reconsidered, and it is blocked on a format decision.
- **It does not touch `Load(ReadOnlyMemory<byte>)`.** That overload parses the caller's memory and
  must keep doing so.
- **It does not assume the trade is worth taking.** On the evidence in the spec it may well not be.
