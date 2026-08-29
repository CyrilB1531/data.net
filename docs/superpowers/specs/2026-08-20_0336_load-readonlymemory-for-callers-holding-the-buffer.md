# 0336 — EmbeddingIndex.Load(ReadOnlyMemory) for callers who already hold the buffer

**Issue:** [#0336](https://github.com/CyrilB1531/lodestar/issues/0336) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-20

## Problem

A caller holding the artifact already — a blob, a cache entry, an embedded resource — had to hand it to the `Stream` overload, which copies it back out before parsing. [#324](https://github.com/CyrilB1531/lodestar/issues/324) profiled that read phase at **about a third of a large index's load**.

## What was measured

**1.40× on processor time** against the stream overload, both rows in the same run.

## What was decided, and its edges

- **It checks `MaxTotalBytes` before parsing rather than while reading**, the length being known up front.
- **No `Async` counterpart, and none is needed** — nothing is waited on.
- **It is the only loader to gain one.** The saving scales with the artifact and no other artifact here is large enough for it to matter.
- The bytes must not change while it runs, which the page says rather than leaving to be discovered.

## What this lot also found

Every embedding-index figure either harness published came from a `MemoryStream`. **Loading off disk went unmeasured**, which is how it stayed the worst row in the comparison without anyone knowing: 12.650 ms against numpy's 2.363. `embedding_index_load_file` was added on both sides — and the write half was left, which [#432](https://github.com/CyrilB1531/lodestar/issues/432) finally added.
