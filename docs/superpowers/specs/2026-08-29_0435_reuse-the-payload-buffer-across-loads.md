# 0435 — Reuse the payload buffer across loads, so its pages are already committed

**Issue:** [#435](https://github.com/CyrilB1531/lodestar/issues/435) ·
**Status:** proposed · **Date:** 2026-08-29

## Problem

[#324](https://github.com/CyrilB1531/lodestar/issues/324) established that the load's budget is
allocation and page commit, and that `GC.AllocateUninitializedArray` recovered only the zeroing
part — *"most of that phase is the operating system committing pages on first touch, which no
allocation strategy avoids."*

**Reuse is the exception to that sentence**, because a pooled buffer's pages are already committed.
[#433](https://github.com/CyrilB1531/lodestar/issues/433) is independent evidence of the size: a
load inheriting a warmed heap ran 1.10–1.22× faster than one that did not. That figure is itself
being re-measured by #433's own lot, and this lot should not start before it has.

## The scope question, settled

The issue asks who owns the pool and for how long, and says the two candidates have opposite
lifetimes. They do, and the code already separates them:

```text
EmbeddingIndex.Load(Stream)
  → JsonArtifact.ReadAllBytes(source, limits)      transient: consumed by FromPayload, escapes nowhere
  → FromPayload(payload, limits)
      → Base64Numbers.ReadSingles(...)             persistent: becomes the index's _data
```

**Only the payload is poolable.** It is filled, parsed, and dead when `FromPayload` returns. The
vector block becomes the index's backing store and outlives every call, so returning it to a pool
is a use-after-free with no runtime complaint — exactly the failure the issue names.

One thing the plan must *verify* rather than assume: that nothing retains a slice of the payload.
Ids come out of `Utf8JsonReader` as new `string` instances and the floats are decoded into their
own array, so the reading is that nothing does — but "the reading is" is not "it was checked".

## What was measured, and what it costs

`ArrayPool<byte>.Shared` on .NET 10, this machine:

| rented | actual array | same instance returned |
| ---: | ---: | --- |
| 524 288 | 524 288 | yes |
| 1 048 576 | 1 048 576 | yes |
| 1 048 577 | 2 097 152 | yes |
| **20 589 008** | **33 554 432** | **yes** |

The shared pool does serve a 20 MB rent — worth stating because it is widely believed to cap at
1 MiB, and a lot planned on that belief would have built a private pool for nothing.

**But it rounds to the next power of two.** The artifact is 20 589 008 bytes and the rent is
33 554 432: the trade is **20.5 MB allocated per load and collected, against 33.5 MB held by the
pool for the life of the process.** That is the decision, and it is not obviously the right way
round for a library whose caller may load one index and never load another.

## The invariant, which here has teeth

`src/Shared/Persistence/Buffers.cs` states it:

> **Every element the caller then exposes must be written first.** A partly filled buffer has to be
> sliced to what was written […] past that, this hands back whatever the heap held, where `new T[]`
> handed back zeroes.

On this path a rented buffer may specifically hold **the previous index's artifact**. A load that
exposes one byte more than it wrote does not return zeroes; it returns another index's vectors and
ids to the caller, silently and plausibly. It is a security property, not only a correctness one,
because the value reaches the caller.

The exact-length slice is therefore not a detail of the implementation but the thing being
implemented, and a test that a second load cannot observe the first's bytes is the acceptance
criterion, not a nicety.

## Acceptance

- A before/after on `embedding_index_load` with enough runs to separate from its spread, interleaved,
  with an allocation-free control.
- **The residency cost stated beside the time saving.** A row that reports 1.1× and omits 33.5 MB
  held is not a result this project publishes.
- A test that loads two different indexes in sequence through a pooled path and asserts the second
  observes none of the first's bytes.
- Identical behaviour on `net10.0` and `netstandard2.0`.
- If the trade is refused, an ADR saying so — #432 and ADR 0052 are the model.
