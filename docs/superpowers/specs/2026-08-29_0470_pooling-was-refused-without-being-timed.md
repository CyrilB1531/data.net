# 0470 — Pooling was refused without being timed

**Issue:** [#470](https://github.com/CyrilB1531/lodestar/issues/470) ·
**Status:** accepted — implemented by [ADR 0054](../../decisions/0054-the-payload-buffer-is-pooled-after-all-because-the-collection-is-the-cost.md) ·
**Date:** 2026-08-29

## The defect this records

[ADR 0053](../../decisions/0053-the-payload-buffer-is-not-pooled-because-residency-outlives-the-load.md)
closed [#435](https://github.com/CyrilB1531/lodestar/issues/435) by refusing to rent the payload
buffer. It carried two columns and decided on them:

- **saved** — 37 069 648 → 16 480 488 bytes allocated per load, collections 4/4/4 → 1/1/1;
- **cost** — `ArrayPool<byte>.Shared` rounds a 20 589 008 rent to 33 554 432, held for the process.

**The third column was never taken.** No timing of the pooled path exists in that lot. The 8.1%
it quotes belongs to [#433](https://github.com/CyrilB1531/lodestar/issues/433), which measured a
cold process against one whose heap was already grown — the value of *pages already committed*.
Pooling removes the allocation and the collection it provokes, which is a different effect, so
8.1% was never its ceiling.

That is a method failure, not a judgement call: this repository refuses changes on measurement
([0052](../../decisions/0052-pre-sizing-the-artifact-file-buys-nothing-on-a-delayed-allocation-filesystem.md)
is the model) and 0053 refused one on an absence.

## What the corpus is

`bench/Lodestar.Text.Benchmarks -- pool-cost`: allocating a 20 589 007-byte buffer against renting
and returning one, interleaved one round each in a single process, both rows touching every page.

**Against an uninitialized allocation.** `Buffers.AllocateUninitialized` is what the read path
uses, so comparing a zeroed `new byte[]` would charge the pool's rival for a memset the code does
not perform and inflate the saving by the whole zeroing. Getting that wrong first is what the
first draft of this measurement did.

## What it found

On a hosted runner: allocate **1.783 ms** median (min 0.071, max 2.756) against rent's **0.042**
(min 0.038, max 0.068). **42×, 1.74 ms a load**, about a tenth of `heap-warmth cold`'s 18.101 ms.

**The allocation itself is as cheap as the rent** — its minimum says so. The median is twenty-five
times that because a large-object collection fires on most iterations. Which is the mechanism
0053 printed as 4/4/4 against 1/1/1 and did not price.

The shared container reads the same shape with the noise turned up: 0.14 ms min against 8–135 ms
median. It could not have decided this, and the first three runs taken there were not published.

## What it does not claim

- **Not that residency is free.** 33.5 MB stays held for the process, and the pool's buckets are
  per core, so a threaded loader holds a multiple. 0054 takes that as a price rather than
  discovering it is absent.
- **Not that the whole load gets 1.74 ms faster in every process.** The figure is the primitive's;
  a load that never triggers a collection saves the allocation's minimum instead.
- **Not that 0053's other reasoning was wrong.** Its exposure invariant and its placement of the
  rent above `FromPayload` are both correct and are kept verbatim.

## The bug found while measuring it

An unknown subcommand fell through to `BenchmarkSwitcher`, which prints a menu and exits 0 — so a
mistyped dispatch produced nine rounds of nothing and a **green** run. That happened on run 4 of
`Benchmark (on demand)` and cost a cycle. It exits 2 now.
