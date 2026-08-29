# 0053 — The payload buffer is not pooled, because residency outlives the load

**Status:** accepted · **Date:** 2026-08-29 · **Refines:** [`0051`](0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md)

## Context

[0051](0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md) halved a save's allocation by
never growing a 20.48 MB writer buffer, and its consequences noted that the load had been
*subsidised* by that buffer: a process that saved first found the pages already committed.
[#433](https://github.com/CyrilB1531/lodestar/issues/433) measured the subsidy at **8.1%** of a
load, nine rounds of nine on one runner, with the mechanism being one fewer garbage collection
rather than anything obscure.

The obvious next move is to warm the heap deliberately: rent the payload buffer from
`ArrayPool<byte>.Shared` instead of allocating one per load, so the pages are committed once and
reused. [#435](https://github.com/CyrilB1531/lodestar/issues/435) built it and measured it.

**It works.** `heap-warmth cold`, same container, before and after:

| | allocated per load | collections (gen0/1/2) |
| --- | ---: | ---: |
| allocating | 37 069 648 bytes | 4 / 4 / 4 |
| rented | 16 480 488 bytes | 1 / 1 / 1 |

**2.25× less allocated and a quarter of the collections.** The 20 589 160 bytes removed are the
payload itself — the artifact is 20 589 007 on disk — so this is not an estimate of a buffer, it
is the buffer.

## Decision

**The payload is not pooled.** The code is reverted; the tests and the measurement stay.

**The bar is a caller who loads once, and that caller loses.** `ArrayPool<byte>.Shared` serves a
20 MB rent — contrary to the common belief that it caps at 1 MiB — but rounds up to a power of
two: asking for 20 589 008 returns **33 554 432**, 1.63× the ask, because just past 16 MiB is the
worst place on that curve to land. That array is then held **for the life of the process**, not
the life of the load.

So the trade is 20.5 MB *not allocated per load* against 33.5 MB *resident forever*, and
break-even is 1.63 loads. An embedding index is loaded once at start-up and queried for as long
as the process runs — the shape [`0011`](0011-persistence-format.md) designed the artifact for and
the one every guide demonstrates. **That caller swaps a collectable 20.5 MB for a permanent
33.5 MB and receives nothing.** It is not a near miss; it is the wrong sign.

Two things sharpen it rather than soften it. The shared pool keeps its buckets per core, so a
process loading indexes on several threads holds a multiple of 33.5 MB, not one. And the 8.1%
ceiling was never reached or even measured here: the time this could return was capped by #433
before the first line was written, and the allocation win is what it actually delivered.

**What would change this decision** is a caller that loads repeatedly in one process — a reload
loop, a multi-tenant service swapping indexes, or the benchmark harness itself. None of those is
this library's published shape today, and a pool serving them would want to be their choice
rather than a library default. `ArtifactLoadOptions` is where such a choice would live, and
[0044](0044-compression-belongs-to-the-caller.md) already refused one option there for reasons
that apply again: the caller who wants pooling can rent and pass the bytes to
[`EmbeddingIndex.Load`](../reference/embeddings/search/embeddingindex-load.md)`(ReadOnlyMemory<byte>)`, which parses in place and never pools.

## Consequences

- The tests stay. `PooledPayloadTests` round-trips a short artifact after a long one and asserts
  the second observes nothing of the first. Without pooling they cannot fail, which is stated
  here so nobody mistakes them for coverage of a live invariant — they exist so that the next
  attempt at this fails loudly rather than silently serving one caller another's bytes. They did
  fail, once, deliberately: a slice taken from the buffer's length instead of the byte count read
  produced `'d' is invalid after a single JSON value` at byte 38 540, which is one artifact being
  parsed after another ended.
- **`ReadAllBytes` keeps its exact-length contract**, and its documentation keeps saying why. The
  reverted work found one place the contract was load-bearing in a way nobody had noticed:
  `TryReadDeclaredLength` bounded its read loop by `buffer.Length`, which is the declared length
  only while the array is allocated exactly. That is correct today and would have been a
  read-past-the-artifact the moment a rented array appeared.
- **No bench row was added**, and none was forgotten. `heap-warmth` already measures allocation
  per load, which is the column this decision turns on; a row that publishes a refused change's
  timing would be a row nobody can act on.
- The residency figures are in
  [`../guides/performance.md`](../guides/performance.md#renting-the-payload-instead-of-allocating-it-issue-435),
  beside the saving, so the next proposal starts from the trade rather than from the win.
