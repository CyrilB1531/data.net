# 0044 — Compression belongs to the caller, not to the artifact format

**Status:** accepted · **Date:** 2026-08-20

## Context

[#378](https://github.com/CyrilB1531/lodestar/issues/378) is what this records.
[ADR 0011](0011-persistence-format.md) chose versioned JSON written with `System.Text.Json`, with
float blocks in base64. Base64 spends eight bits to carry six, so an artifact lands at about 1.33×
the raw block it holds — 8 231 006 bytes where the vectors are 6 144 000. #378 opened on the
observation that deflate takes that expansion back almost exactly, and asked whether the format
should carry it: how a reader would recognise a compressed artifact, whether it would be opt-in,
and what `ArtifactSaveOptions` would cost to invent.

Two of those three questions dissolved under measurement, and the third answered the issue.

**`Load` already reads a compressed artifact, with no change at all.** A decompressing stream is
neither seekable nor of known length, so it takes the growable path in `JsonArtifact.ReadAllBytes`
— the same path a network stream takes. Verified round-trip from memory and from a `.json.gz`
file: same count, same dimension, same search results. So there is no format to recognise and no
old artifact to keep compatible, because nothing about the bytes on disk changes.

**And the price is not affordable as a default.** Measured on a synthetic 4 000 × 384 index through
the real `Save` and `Load`, Intel i7-4770S, .NET 10.0.10, warmed, median of 7, five modes
interleaved in one window ([the performance guide](../guides/performance.md#compressing-an-index-issue-378)
has the full table):

| | × size | × save | × load |
| --- | ---: | ---: | ---: |
| gzip `Fastest` | 0.760 | **26.67×** | 7.19× |
| gzip `Optimal` | 0.747 | 37.62× | 5.92× |
| brotli `Fastest` | **0.738** | 3.68× | 5.19× |

[#323](https://github.com/CyrilB1531/lodestar/issues/323),
[#324](https://github.com/CyrilB1531/lodestar/issues/324),
[#336](https://github.com/CyrilB1531/lodestar/issues/336) and
[#377](https://github.com/CyrilB1531/lodestar/issues/377) are four lots spent taking copies and
zeroing out of this path. The cheapest compression available multiplies the load by 5.19 and the
save by 3.68, to buy 26% of a disk — and the price grows with the artifact rather than staying
put: the benchmark corpus's 20 MB index pays 76.8x the save and 14.8x the load at `Optimal`,
against 37.62x and 5.92x on the 8 MB one. The indexes big enough for the size to matter are the
ones where compressing costs the most.

## Decision

**The library does not compress an artifact, and does not offer an option to. A caller who wants
compression wraps the stream, on both sides.** `index.Save(new GZipStream(file, level))` writing,
and [`EmbeddingIndex.Load`](../reference/embeddings/search/embeddingindex-load.md) reading from a
`GZipStream(file, CompressionMode.Decompress)`.

What follows from that:

- **No `ArtifactSaveOptions`.** It was #378's heaviest stated cost, and the opt-in it would have
  carried already exists as a constructor the caller writes. A flag would add public surface, a
  reference page and a packaging-gate reference to express what one wrapper already expresses.
- **No magic-byte sniffing on load.** `Load` refusing raw gzip bytes is the right answer, and it
  already says which byte was wrong: `'0x1F' is an invalid start of a value`. Sniffing would make
  `Load` silently accept a shape the caller did not mean to hand it, to save that caller one
  wrapper they wrote deliberately on the other side.
- **No change to ADR 0011**, and none to what a written artifact looks like. #100's byte-for-byte
  compatibility is untouched because nothing was touched.
- **gzip is the documented recipe, not brotli**, despite brotli being smaller on every level and
  seven times cheaper to write. `BrotliStream` does not exist on `netstandard2.0`, and a recipe
  that works on one of two target frameworks is not one this project publishes. The guide names
  brotli as the .NET-10-only alternative for a caller who is not on the older target.

## Consequences

The capability was accidental — true by construction, decided by nobody, tested by nothing. Two
things make it a contract rather than a coincidence:
`tests/Lodestar.Embeddings.Tests/Persistence/CompressedArtifactTests.cs` pins the round trip, the
asynchronous read, the diagnosable failure on undecompressed bytes, and that `MaxTotalBytes` is
enforced on what the artifact *expands to* rather than on what it occupies. `compare-persistence`
carries `embedding_index_save_gzip` and `embedding_index_load_gzip` beside the plain rows, against
numpy's `savez_compressed`, so the table above is re-measured on every nightly rather than
remembered from this one.

The loser is worth naming: a format-level `Compression = Deflate` would have made small artifacts
the default and put this project's own numbers back where they were two releases ago. The size was
never in doubt. It was priced, and it is the caller's call to make.
