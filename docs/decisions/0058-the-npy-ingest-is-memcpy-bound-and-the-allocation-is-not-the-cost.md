# 0058 — The `.npy` ingest is `memcpy`-bound, and the allocation is not the cost

**Status:** accepted · **Date:** 2026-08-30 · **Amends:** [`0057`](0057-the-npy-read-serves-a-stream-and-a-buffer-differently.md)

## Context

[0057](0057-the-npy-read-serves-a-stream-and-a-buffer-differently.md) shipped the two-entry-point
reader and, in its Consequences, explained the measured win by pointing at an allocation:

> All of it came from adopting, because `FromBlock` was allocating a second 15.36 MB store on the
> large object heap to copy into and `FromOwnedBlock` allocates none — the mechanism
> [0054](0054-the-payload-buffer-is-pooled-after-all-because-the-collection-is-the-cost.md) priced
> on the artifact buffer.

That explanation was never measured. It was inferred by subtracting whole benchmark rows, and
[#480](https://github.com/CyrilB1531/lodestar/issues/480) was opened to test it rather than leave
it standing. The test refutes it.

`ingest-phases` on a hosted runner, three rounds of nine interleaved runs each — the full table is
in [the performance guide](../guides/performance.md#where-the-ingests-time-actually-goes-issue-480):

| | round 1 | round 2 | round 3 |
| --- | ---: | ---: | ---: |
| `read_stream_owned` − `stream_copy_floor` | 0.022 ms | 0.072 ms | 0.018 ms |
| … less `parse_header_only` | 0.016 ms | 0.067 ms | 0.013 ms |
| `allocate_cold` − `allocate_reused` | 0.016 ms | 0.016 ms | 0.018 ms |

**Two subtractions, built differently, and neither puts the allocation near a copy.** The first
prices the reader's `float[]` from inside the read; the second prices the same allocation on its
own, cold against a reused buffer. They are not pricing quite the same thing: the read-side
figure also carries the header parse — measured separately as `parse_header_only`, 0.005–0.006 ms
— and the difference between a fresh destination and a warm one, which is the middle row above.
With the header out, rounds 1 and 3 agree with the allocation side to a thousandth; round 2 does
not, its 0.072 against 0.016 driven by a `stream_copy_floor` reading 0.889 ms where the other two
read 0.965 and 0.967.

So two estimators put the allocation at 0.013–0.018 ms in two rounds of three, and no reading of
either puts it within a tenth of a copy. Against the canonical harness's 1.11 ms ingest that is
roughly 2%; against `ingest_total`'s own median, 0.9%. The denominator matters to the percentage
and not at all to the conclusion.

What costs is the block moving. A bare `CopyTo` of the 15.36 MB reads 0.94–0.98 ms; the staged
read reads 0.96–0.99, one copy;
[`FromBlock`](../reference/embeddings/search/embeddingindex-fromblock.md) reads 0.96–1.09, one
copy. [`FromOwnedBlock`](../reference/embeddings/search/embeddingindex-fromownedblock.md) reads
0.010–0.016 ms and the memory overload 0.005–0.008 — free, because neither moves the block.

## Decision

**The ingest is bandwidth-bound on `memcpy`, and adopting the block is worth exactly one copy —
about 0.96 ms on this runner — not more.** 0057's Consequences are corrected on that point, and
its own figures are left as they were measured.

Three things follow, and they are what this decision is for:

- **A copy of this block is the unit to reason in.** At roughly 16 GB/s a 15.36 MB copy is about
  0.96 ms, and every route through the ingest is one copy or none. A proposal that claims more
  than the copies it removes is claiming something this table does not support.
- **Pooling the reader's array would buy about 0.02 ms**, so the tension 0056 creates — the array
  is handed to the caller for the life of an index, leaving nobody to return it to — does not need
  resolving. It was never worth the contract.
- **0054 is not contradicted; it was applied to the wrong thing.** Its finding stands on the
  artifact buffer, where a 20.59 MB allocation per load provoked a collection worth 1.74 ms. What
  0057 did was assume the same mechanism on a path where nothing had measured it.

## What was refused

**Leaving 0057 to stand and correcting only the guide.** An ADR is where a reader goes for the
reasoning behind a shipped decision, so a wrong mechanism left there outlives any page that
contradicts it. 0057 is accepted and therefore immutable, which is exactly why this exists as a
new decision rather than as an edit.

**Reading `ingest_total` as the answer.** It measures 2.17–2.26 ms where its own parts sum to
0.97–1.00 and where the canonical harness measures the same chain at 1.109–1.134. Taking the
larger number would have replaced one unmeasured attribution with another.

## Consequences

- 0057's shape is unaffected: two entry points, one contract each,
  [`OwnedArray`](../reference/embeddings/persistence/npyblock.md) filled only by the
  stream reader. **What made the win reachable is unchanged; only why it is that size is.**
- [`NpyFile.Read(ReadOnlyMemory<byte>)`](../reference/embeddings/persistence/npyfile-read.md) is
  worth more than 0057 argued, not less: at 0.005–0.008 ms
  against the stream read's 0.96–0.99, a caller who already holds the bytes skips the whole cost
  of the ingest rather than a copy of it.
- **What would change this decision** is the gap `ingest_total` still carries, and it is left
  unexplained rather than attributed. Its minimum, 0.92–1.00 ms, equals the sum of its parts and
  its median is a copy higher, so the row is bimodal. It carries 2 gen0, 2 gen1 and 2 gen2 over
  nine runs where `from_block_copy` — which allocates 15.36 MB, fills it, hands it to an index and
  drops the index, at one copy — carries none, so retention alone does not pick it out; and
  `from_owned_adopt` carries 4 of each at 0.010–0.016 ms, so on this table a collection count does
  not predict a cost. `ingest_total` is also always the first phase of every round, which makes it
  where the collector settles what the round before it left — a property of the harness, the
  likeliest of these candidates, and still a candidate. The canonical harness measures the same
  chain at 1.109–1.134 ms, close to the sum of the parts and not to `ingest_total`. Telling these
  apart needs a run that reorders the phases, which this lot did not make. An unexplained gap named
  honestly is worth more than a second unmeasured mechanism, which is what this decision refuses
  above.
