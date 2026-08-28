# 0052 — Pre-sizing the artifact file buys nothing on a delayed-allocation filesystem

**Status:** accepted · **Date:** 2026-08-28

## Context

[`0051`](0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md) took step 1 of
[#429](https://github.com/CyrilB1531/lodestar/issues/429) and left one of its four items
unbuilt. [#432](https://github.com/CyrilB1531/lodestar/issues/432) is that item:
`JsonArtifact.OpenWrite` opens with `FileMode.Create` and the save then writes about 20 MB
through an 80 KB buffer — 252 `write` calls, each of which extends the file. Setting the
length up front should let the filesystem allocate once.

It was not built with the rest of step 1 for a stated reason, and the reason is the whole
shape of this decision: **no published row could have shown it.** Every persistence
benchmark reporting a save — `embedding_index_save`, `tfidf_save`, `embedding_index_save_gzip`
— writes to a `MemoryStream`. `embedding_index_load_file` was the only row touching a
filesystem, and it reads.

So the row came first. `embedding_index_save_file` is now in `PersistenceCrossLang` beside
its load counterpart, with `np.save` to a path as its Python pair, and #336's reasoning
applies to it unchanged: **the file path is the one a caller actually takes**, and until now
every index figure this project published came from memory.

## The measurement

Two experiments, both interleaved round-robin one round each — the protocol
[`0051`](0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md) adopted after running
phases to completion put one phase at 136.7% of a strict superset.

**Conditions.** Four cores of an Intel Xeon @ 2.80GHz, .NET 10, a shared cloud container,
writing to **ext4 on a block device** — `/dev/vda`, not a tmpfs, which would have made the
whole exercise meaningless.

### The hypothesis on its own

20 589 008 bytes — the artifact's real size — through an 80 KB-buffered `FileStream`, with
and without a `SetLength` first. No JSON, no base64, no index. If pre-sizing absorbs file
extensions, it shows here or nowhere. 75 rounds, three runs of 25:

| | run 1 | run 2 | run 3 |
| --- | ---: | ---: | ---: |
| plain, median | 5.149 ms | 4.998 ms | 5.135 ms |
| `SetLength` first, median | 5.177 ms | 5.081 ms | 5.016 ms |

**They are the same number.** The medians differ by under 2% and they differ in *both
directions* across three runs; the p25–p75 bands lie on top of one another.

### The real save path

[`EmbeddingIndex`](../reference/embeddings/search/embeddingindex.md)'s
[`Save(path)`](../reference/embeddings/search/embeddingindex-save.md) against a reproduction of
itself without the pre-size,
interleaved in one process so both states share a machine *state* rather than a machine,
with `File.WriteAllBytes` of the finished artifact as a floor neither state can influence.
63 rounds, three runs of 21:

| | run 1 | run 2 | run 3 |
| --- | ---: | ---: | ---: |
| no `SetLength`, median | 7.670 ms | 9.932 ms | 7.756 ms |
| `SetLength`, median | 7.901 ms | 9.511 ms | 8.451 ms |
| `WriteAllBytes` floor | 4.853 ms | 4.995 ms | 4.859 ms |

Pre-sizing is **slower in two runs of three** and faster in the third. There is no effect to
find.

The floor row says why, and it is the useful part. Writing the finished bytes in one call
costs 4.86 ms; the whole save costs 7.67. The 2.8 ms between them is the base64 encode —
0051 measured 3.211 ms for it on this same machine — which leaves **nothing at all for file
extension to be costing.**

## Decision

### The artifact file is not pre-sized

`JsonArtifact.OpenWrite` keeps opening with `FileMode.Create` and nothing sets a length.

**The mechanism, because a null result that cannot be explained is not evidence.** ext4 uses
delayed allocation: the 252 `write` calls land in the page cache and the file's blocks are
allocated once at writeback, sized to what is actually there. There is no per-write
extension to absorb, because the per-write extension does not happen. `ftruncate` up front
only creates a sparse extent that writeback still has to allocate — the same work, plus a
syscall.

**What it would have cost** is small but not nothing, and it is what makes the refusal easy
rather than close. Pre-sizing obliges a truncation afterwards, since the estimate covers the
vector block and the head and the ids are written past it — so a crash between the two
leaves a file *longer* than its document, padded with zeros. #432's own third item asked
whether that failure is diagnosable. It is a question worth answering only for a change worth
making, and this one is not: an arrangement owning both ends is the only safe one to expose,
which is a new method on `JsonArtifact` and a new invariant to keep, for zero.

[`0051`](0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md)'s refusal of parallel
base64 is the precedent and this is the same argument twice: a design that is sound, cheap
and obviously right, refused because the measurement it was blocked on came back empty. The
difference is only in which layer turned out to be already doing the work — there the memory
subsystem, here the filesystem.

### The row stays, and it is what reopens this

`embedding_index_save_file` is not scaffolding for a refused change. It is the save row this
project should have had since #336 added the load half for exactly the same reason, and its
Python pair keeps the comparison honest rather than one-sided.

It is also the instrument. **The one thing that would reopen this is a filesystem that
charges per extension** — NTFS is the named candidate, since it advances a valid-data-length
and zero-fills rather than deferring allocation. Run `compare-persistence` there and compare
`embedding_index_save_file` against `embedding_index_save`: if the file row costs materially
more than the memory row does, the answer on that platform is different from this one, and
this decision covers ext4 rather than every filesystem. Nothing here was measured on Windows.

## Consequences

- **#432 is closed as refused, not as done.** The distinction matters for a roadmap item:
  someone reading #429 should see that step 1 item 4 was measured and declined, not skipped.
- `PersistenceCrossLang` and `bench/python/bench_persistence.py` gain
  `embedding_index_save_file`, written to a path of its own so neither direction is measuring
  a file the other just touched. Both sides write without flushing to the device, which is
  what makes them comparable: each measures the write path down to the page cache.
- **The save rows this project publishes are no longer all in-memory**, which was a gap in
  the comparison rather than in this change. Whatever `embedding_index_save_file` reports
  against `numpy.save` to a path is a fact about the file path that nothing published before.
- The floor comparison above is worth keeping in mind beyond this decision: on this machine
  an index save costs a `WriteAllBytes` of the same bytes plus its encode, and nothing else.
  There is no third cost hiding in the save path to go looking for.
