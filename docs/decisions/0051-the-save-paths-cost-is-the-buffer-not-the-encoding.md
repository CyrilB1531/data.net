# 0051 — The save path's cost is the buffer, not the encoding

**Status:** accepted · **Date:** 2026-08-27 · **Amends:** [`0044`](0044-compression-belongs-to-the-caller.md)

## Context

[`../guides/performance.md`](../guides/performance.md)'s save-path section has said since
[#323](https://github.com/CyrilB1531/lodestar/issues/323) that **encoding is the dominant cost of
a save**. Nothing measured it. The arithmetic behind it was plausible — 15.36 MB of vectors,
vectorised base64 at roughly 3 GB/s on one core, about 5 ms, against a nightly reporting 5.949 ms
for the whole operation — and on the strength of it, parallelising the encode was proposed twice.

[#324](https://github.com/CyrilB1531/lodestar/issues/324) had already settled the same question
for the read direction by replacing the decode with a `memcpy` of the same byte count. The write
direction never got that treatment. It has one now: `bench/Lodestar.Text.Benchmarks -- save-phases`,
four rows each a strict subset of the one above it.

**Conditions, which matter for what follows.** Four cores of an Intel Xeon @ 2.80GHz (AVX2 and
AVX-512F present), .NET 10.0.11, a shared cloud container — **not** the Intel i7-4770S the rest of
the performance guide was taken on, and not the nightly runner. `embedding_index_save` reads
16.9–20.6 ms there against the published 5.949 ms, so the machine is roughly 3× slower on this
row. Nine rounds, phases interleaved one round each, medians with the spread of every run.
**The shares and the ratios below are the transferable part; the absolutes are not.**

| phase | median | share of save | GB/s |
| --- | ---: | ---: | ---: |
| `save_total` | 18.185 ms | 100% | 0.84 |
| `write_base64_property` | 16.938 ms | 93.1% | 0.91 |
| `base64_encode` | 3.211 ms | 17.7% | 4.78 |
| `block_copy_floor` | 3.251 ms | 17.9% | 4.72 |

Two facts come out of it, and the second is the one that decides.

**Encoding is 17.7% of the save, not most of it.** The throughput half of the estimate was close —
measured, 4.78 GB/s against the 3 GB/s assumed. The conclusion drawn from it was not, because the
budget it was compared against was never 5.949 ms of encoding.

**The encode costs nothing over moving the bytes.** `block_copy_floor` copies the same 15.36 MB and
does not encode it: **3.251 ms against the encode's 3.211 ms.** They are the same number.
`Base64.EncodeToUtf8` is **bandwidth-bound, not compute-bound** — the vectorised path saturates the
memory subsystem before it saturates the ALUs. That is the write-direction twin of #324's finding
that decoding costs ~1.3 ms *over* moving the bytes at all.

## Decision

### The encode is not parallelised

The design was sound and is refused anyway. Slicing the float span on 12-byte boundaries — 3 floats,
4 base64 groups — encoding the slices on separate cores into pooled buffers and writing them out in
order is correct by construction, and was expected to return 2.5–3× on four physical cores.

It is refused for two independent reasons, either of which is sufficient.

**Nothing parallelises past a bandwidth it is already at.** The encode runs at the speed of a
`memcpy` on one core. Additional cores contend for the same memory controller rather than adding
throughput. The 2.5–3× estimate assumed the encode was compute-bound; it is not, and that assumption
was never checked because the isolating measurement was never taken.

**The lever cannot clear its own gain bar.** It is worth 17.7% of `embedding_index_save` in total,
against a bar of ≥ 2× set before measuring. A free, perfectly scaling encode caps the row at 1.25×.

What it would have cost is worth naming, because it is what makes the refusal easy rather than
close: a concurrency surface on a save path, a second code path to keep bit-identical forever, and
an `ArtifactSaveOptions` question 0044 already refused once — for a fifth of one row.

[`0018`](0018-multiclass-roc-auc-parallelism-is-opt-in.md) is the counter-example that shows the
line is real rather than a general distaste for threads. There, parallelism was accepted, opt-in,
with the caller naming the worker count — because multiclass ROC-AUC is `O(samples log samples)` of
genuinely independent sorts, which is compute the cores can absorb. The property that decides is not
"is the work independent" — base64 is *more* independent than ROC-AUC — but **"is the work
compute-bound"**. Here it is not.

### The lever is the writer's buffer, and it is taken

`Utf8JsonWriter.WriteBase64String` encodes the whole block in one call, so the writer's internal
buffer must grow to hold the entire 20.48 MB encoding by successive doubling — a large-object-heap
allocation and an operating-system page commit per growth, plus a copy of everything written so far.
Writing the vector block alone costs 16.938 ms of which the encode is 3.211; the other ~13.7 ms is
that.

The block is now written a slice at a time straight to the destination — 245 760 bytes per slice,
a multiple of 12, so every slice but the last is a whole number of base64 groups *and* of floats.
That is the condition under which concatenating the slice encodings equals encoding the
concatenation, so **the bytes on disk do not change**, and `ChunkedBlockTests` pins it at nine sizes
around the slice boundary against `Base64Numbers.WriteSingles`, which stays in the codebase off
every save path purely as the oracle.

`embedding_index_save` goes from a median of 20.550 ms to 12.727 ms — **1.61×** — over 24 runs in
both orders, with no overlap between the two sets of twelve. A `memcpy` of the same block carried as
the noise floor moved 21% across the same windows.

### 0044's price table is re-based, and its decision stands

This is the amendment. [0044](0044-compression-belongs-to-the-caller.md) prices compression in
**multiples of a save**, and that save is now 1.61× faster. Every `× save` figure in its table is
therefore understated against the current code:

| | 0044 as written | against the sliced save |
| --- | ---: | ---: |
| gzip `Fastest` | 26.67× | ~43× |
| gzip `Optimal` | 37.62× | ~61× |
| brotli `Fastest` | 3.68× | ~5.9× |

**0044's decision is unaffected, and strengthened.** Compression got relatively more expensive, not
less, because the thing it is priced against got cheaper. The library still does not compress an
artifact and still does not offer an option to; the caller still wraps the stream on both sides.
Only the numbers move, and they move the way 0044 would want them to.

The `× size` and `× load` columns are untouched — nothing here changes what an artifact occupies or
what reading one costs.

### The binary sidecar is argued on size in **both** directions

0011's own `#324 update` block concluded that a binary format "would not buy back the load time this
decision was worried about, so it should be argued on the size rather than on the speed." That was a
statement about reading. It now holds for writing too, and for a sharper reason: since the encode
costs nothing over a `memcpy`, **removing base64 removes no measurable time in either direction.**

The 1.33× expansion — 5.2 MB on a 20.5 MB artifact — remains a real thing to want. It remains the
only argument for a sidecar, and it remains 0011's to make.

## Consequences

- `ArtifactIo` gains `SaveWithBlock` / `SaveWithBlockAsync`, which own the whole sequence: the writer
  emits the property name, is flushed and disposed, the value goes to the stream, and the closing
  brace is written by hand. A writer left on a property name refuses to close its object, which is
  `SkipValidation = false` doing the job [0011](0011-persistence-format.md) keeps it on for. Handing
  each artifact a stranded writer to be careful with was the alternative; owning the sequence in one
  place means no artifact can get it wrong. **The large block must be the last property**, and it is
  `ArtifactIo` rather than any artifact that depends on that.
- [`EmbeddingIndex.SaveAsync`](../reference/embeddings/search/embeddingindex-saveasync.md) loses its intermediate `MemoryStream`. It existed because the writer
  flushed synchronously when its buffer filled, so the artifact was buffered twice and both buffers
  doubled. The head is now the only thing that flushes and it is bounded; the block goes through
  `WriteAsync`, and a test asserts the two paths emit identical bytes.
- **`embedding_index_load` is no longer a valid control for a change to the save path**, and this is
  a measurement obligation rather than a footnote. It moved 1.22× *slower* on the after side, in all
  eight runs, in both orders. The cause is this change working: the old save path grew the
  large-object heap by ~20 MB per call and left its pages committed for whatever ran next in the same
  process, and what ran next was the load. The load did not get slower — it stopped being
  subsidised. Any load figure taken in a process that saved first carries a warmed heap, which
  includes rows `compare-persistence` already publishes.
- `bench/Lodestar.Text.Benchmarks -- save-phases` is committed, so the profile behind this decision
  is re-runnable rather than remembered. Its phases run round-robin rather than one phase to
  completion: a first cut ran them back to back and reported `write_base64_property` at 136.7% of
  `save_total`, which is impossible for a strict subset of the same work.
- **This decision's absolutes were not taken on the bench machine**, and the section above says so.
  The shares, the encode-against-`memcpy` comparison and the 1.61× are ratios measured within single
  windows and transfer; the millisecond figures do not. Re-running `save-phases` on the i7-4770S
  would refine the table without touching what it decides — unless `base64_encode` and
  `block_copy_floor` come apart there, which is the one result that would reopen the refusal above.
