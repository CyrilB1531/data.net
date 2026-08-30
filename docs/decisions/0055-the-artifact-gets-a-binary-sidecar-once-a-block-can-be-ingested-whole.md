# 0055 — The artifact gets a binary sidecar, once a block can be ingested whole

**Status:** accepted · **Date:** 2026-08-29 · **Amends:** [`0011`](0011-persistence-format.md)

## Context

[0011](0011-persistence-format.md) chose versioned JSON and refused a binary format. Its own
`#324 update` block narrowed that to a size argument:

> A binary format would still remove the 1.34× size expansion base64 costs on disk, which is a
> real thing to want. It would not buy back the load time this decision was worried about, so it
> should be argued on the size rather than on the speed.

[0051](0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md) said the same for writing:
`base64_encode` costs 3.211 ms against `block_copy_floor`'s 3.251 for the same 15.36 MB, so the
encode costs nothing over moving the bytes.

**Both statements are about base64, and both are correct.** Neither is a statement about the
whole load path, and that is where this decision differs from them: a JSON artifact is not
base64, it is base64 *inside a document that has to be scanned and validated*. Nobody had
measured the difference until [#436](https://github.com/CyrilB1531/lodestar/issues/436).

## The measurement

`bench/Lodestar.Text.Benchmarks -- sidecar`, on a hosted runner, four rows interleaved one round
each. The corpus is the persistence rows' own: 10 000 × 384 with ids.

**Size** — exact, and machine-independent:

| | bytes |
| --- | ---: |
| artifact | 20 589 007 |
| — of which base64 block | 20 480 000 |
| — of which head (schema, flags, 10 000 ids) | 109 007 |
| `.npy` block + head | 15 469 135 |
| | **1.331× smaller**, 5.12 MB |

**Time** — medians of nine, on a runner whose spread is 10.2–13.6 ms on the load row:

| | median |
| --- | ---: |
| [`EmbeddingIndex.Load`](../reference/embeddings/search/embeddingindex-load.md) (payload pooled, [0054](0054-the-payload-buffer-is-pooled-after-all-because-the-collection-is-the-cost.md)) | 11.834 ms |
| [`NpyFile.Read`](../reference/embeddings/persistence/npyfile-read.md) | 5.236 ms |
| **sidecar floor** — the read plus one copy into a backing store | **5.847 ms** |
| rebuild the index through `Add`, per vector | 17.973 ms |

**`load / floor` is 2.02×.** A sidecar load has half the artifact load's cost available to it.

**And `load / rebuild` is 0.66×.** The route that exists today — read the block, then hand it to
[`EmbeddingIndex`](../reference/embeddings/search/embeddingindex.md) one vector at a time — is **slower than the artifact it would replace**.

## Decision

**The artifact gets a binary sidecar. The condition is a bulk ingest path, and the condition is
the decision.**

[`EmbeddingIndex`](../reference/embeddings/search/embeddingindex.md) has no way to take a block whole. [`Add`](../reference/embeddings/search/embeddingindex-add.md) copies one vector, normalizes it, and
grows a backing store; at 10 000 calls that is 17.973 ms, three times the read it follows. A
sidecar shipped against that surface would be a format change that made loading slower — the
worst possible outcome, and the one a size-only argument would have walked into without noticing.

So the order is fixed and not negotiable:

1. **A bulk ingest into `EmbeddingIndex`** — a constructor or factory taking a contiguous block, a
   dimension and a count, doing one copy. That is [#474](https://github.com/CyrilB1531/lodestar/issues/474),
   with its own measurement against the floor above; the floor is what it must approach, not what
   it may assume.
2. **Then the sidecar**, whose layout is `.npy` unless that lot finds a reason otherwise:
   [#450](https://github.com/CyrilB1531/lodestar/issues/450) already ships a reader and a writer
   whose fixtures numpy itself wrote, and the format is contiguous, unencoded, `float32`,
   little-endian, at an offset its own header declares, padded so the payload starts on a
   64-byte boundary.
3. **The head stays JSON**, and stays the artifact's own. A `.npy` carries no ids, no normalize
   flag, no schema and no version — 109 007 bytes of this corpus is exactly that, and it is why
   #450 was interop rather than a second format.

**What this does not decide.** Whether the sidecar is one file or two, how a reader finds the
block from the head, what happens when the two are separated, and whether an artifact written by
an earlier version keeps loading — 0011's compatibility promise is untouched here and is the
first thing the sidecar lot has to answer.

## Consequences

- **0011's "argue on size rather than speed" is narrowed, not overturned.** It is right about
  base64 and 0051 is right about the encode. What neither covers is the JSON scan around the
  block, which is where the 2.02× lives. A future reader quoting either line against a sidecar
  proposal should be pointed here.
- **Memory-mapping ([#436](https://github.com/CyrilB1531/lodestar/issues/436)'s original subject)
  is now downstream of two lots rather than one**, and is still worth having: it removes the copy
  the floor above still pays. Its own questions — the lifetime of a mapped view against the
  `float[]` backing store, a file changing under a live index, and whether `netstandard2.0` gets
  the same behaviour or a documented divergence — are untouched by this.
- **`sidecar` is committed** as a diagnostic, declared in `bench-map.json`'s `diagnostics`, so the
  four rows are re-runnable. The nightly never runs it: it answers a question a lot asks once.
- **The container reached the opposite conclusion and would have decided this wrongly.** It put
  `load / floor` at 0.73×, the sidecar slower, with a spread of 12–43 ms on the floor row against
  the runner's 4.0–8.5. Recorded because the container is what a contributor has, and this is the
  second time in this roadmap that it inverted a result — [#433](https://github.com/CyrilB1531/lodestar/issues/433)
  was the first.
