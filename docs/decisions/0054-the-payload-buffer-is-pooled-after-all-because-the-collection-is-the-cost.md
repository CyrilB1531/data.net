# 0054 — The payload buffer is pooled after all, because the collection is the cost

**Status:** accepted · **Date:** 2026-08-29 · **Amends:** [`0053`](0053-the-payload-buffer-is-not-pooled-because-residency-outlives-the-load.md)

## Context

[0053](0053-the-payload-buffer-is-not-pooled-because-residency-outlives-the-load.md) refused
renting the payload buffer. It measured two columns — 37 069 648 → 16 480 488 bytes allocated per
load, and 33 554 432 bytes held by the pool for the life of the process — and decided on them.

**It never measured the third.** No timing of the pooled path exists anywhere in that lot. The
8.1% it quotes is [#433](https://github.com/CyrilB1531/lodestar/issues/433)'s warm-heap figure,
which is the value of *pages already committed* between a cold process and a warmed one. Pooling
does something else: it removes the allocation, and with it the collection the allocation
provokes. 8.1% was never its ceiling.

## The measurement 0053 owed

`bench/Lodestar.Text.Benchmarks -- pool-cost`, on a hosted runner, interleaved in one process,
both states touching every page. **Against an uninitialized allocation, not a zeroed one**: the
read path uses `Buffers.AllocateUninitialized`, so charging the pool's rival for a memset the
code does not perform would have inflated the saving by the whole zeroing.

| | median | min | max |
| --- | ---: | ---: | ---: |
| allocate 20 589 007 bytes | 1.783 ms | 0.071 | 2.756 |
| rent and return | **0.042 ms** | 0.038 | 0.068 |

**42×, and 1.74 ms per load.** Against `heap-warmth cold`'s 18.101 ms on the same class of
machine, that is **about a tenth of a load**.

**The allocation is not what costs.** Its minimum is 0.071 ms — as cheap as the rent. The median
is twenty-five times that because a large-object collection fires on most iterations. On the
shared container the same rows read 0.14 ms at the minimum and 8–135 ms at the median, which is
the same finding with the noise turned up. This is exactly the mechanism 0053 printed and did not
price: collections 4/4/4 against 1/1/1.

## Decision

**The payload is pooled.** [`EmbeddingIndex.Load`](../reference/embeddings/search/embeddingindex-load.md)`(Stream)` rents from `ArrayPool<byte>.Shared` and
returns the buffer once `FromPayload` has parsed. The code is [#435](https://github.com/CyrilB1531/lodestar/issues/435)'s
unchanged, restored from `a644bc0`.

**The residency is the price, not the refusal.** The pool still rounds a 20 589 008 rent to
33 554 432 and holds it for the process. 0053 read that as decisive because it weighed it against
nothing; weighed against 1.74 ms and three collections per load it is a trade this library takes.
Throughput is what it publishes — `embedding_index_load` sits about 4× behind `numpy.load` — and
resident memory is not.

**What 0053 got right and this keeps.** The rent lives at the `Load(Stream)` call site, above
`FromPayload`, because the public [`Load`](../reference/embeddings/search/embeddingindex-load.md)`(ReadOnlyMemory<byte>)` overload reaches `FromPayload` too
and parses the *caller's* memory: pooling one line lower would return a caller's buffer to the
shared pool. And the slice is taken from the byte count read, never from the buffer's length,
which `PooledPayloadTests` pins — those tests were kept by 0053 for this attempt and now guard a
live invariant rather than a dormant one.

**What would reopen this.** A caller that loads one index in a short-lived process and is
measured on peak resident memory rather than on load time. Nothing published here is.

## Consequences

- `TryReadDeclaredLength` bounds its read loop by the **declared length**, not `buffer.Length`.
  Those are the same number only while the array is allocated exactly; a rented array is at least
  as long as asked, and the old bound would have read past the artifact.
- **A stream with no declared length is not pooled.** The growable path sizes itself as it reads
  and owns the buffer it grows, so `RentedPayload` carries it owning nothing and `Dispose` is a
  no-op. One shape at the call site rather than a branch to get wrong.
- **The buffer is not cleared on return.** Zeroing 20 MB costs the memset this decision exists to
  avoid, and `ArrayPool<byte>.Shared` is process-local, so the bytes were the caller's own
  artifact already.
- `pool-cost` is committed, so the number this turns on is re-runnable rather than remembered.
  It is a diagnostic, not a nightly row: `bench-map.json` names it in `diagnostics`, which is
  where a subcommand the nightly deliberately never runs is declared.
- **An unknown subcommand now exits 2.** It used to fall through to `BenchmarkSwitcher`, which
  prints its menu and exits 0 — so a typo in a dispatch produced nine rounds of nothing and a
  green run. That happened while taking this measurement.
