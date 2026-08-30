# 0057 — The `.npy` read serves a stream and a buffer differently

**Status:** accepted · **Date:** 2026-08-30

## Context

[#474](https://github.com/CyrilB1531/lodestar/issues/474) built the first cross-language row where
both sides read the same `.npy` and return something searchable — `np.load` against
[`NpyFile.Read`](../reference/embeddings/persistence/npyfile-read.md) followed by
[`EmbeddingIndex.FromBlock`](../reference/embeddings/search/embeddingindex-fromblock.md). Every
other index row puts our JSON artifact against numpy's raw block, which prices a format and
reports it as a speed; this one prices the ingest and nothing else.

Its first reading on a hosted runner, three rounds on the same 15 360 128 bytes, was
**0.21–0.23× wall and 0.19× cpu** — numpy four to five times faster on bytes neither side has to
decode. [#466](https://github.com/CyrilB1531/lodestar/issues/466) found why, and it is not the
language: **the block was copied three times before an index held it, where numpy copies it
once.** The stream became a bounded buffer, the buffer became an exact `byte[]`, those bytes
became the `float[]` a block carries — and the block was copied once more into the index's store.

Removing the second of those alone, in this lot's first commit, moved the row to **0.34–0.36×
wall and 0.29–0.30× cpu** on the same runner: about 1.8 ms, the shape of one `memcpy` of this
block. That is the measurement this decision argues from. One copy of 15.36 MB is worth roughly a
fifth of the row, so the remaining two are worth taking too.

Two callers reach the reader, and they do not want the same thing. One holds a `Stream` and has
no bytes of its own. The other already holds the whole file — a blob, a cache entry, an embedded
resource — and has therefore already accepted its lifetime. A single entry point has to serve the
second as though it were the first, and copy what it was handed.

## Decision

**Two entry points, one contract each.**

[`NpyFile.Read`](../reference/embeddings/persistence/npyfile-read.md)`(Stream)` reads the payload
straight into the `float[]` the block carries. The header is read in stages, so the element count
is known — and refused against `ArtifactLoadOptions` — before anything is allocated. It costs one
copy on `net10.0` and two on `netstandard2.0` — the split below — asks nothing of its caller
either way, and the array it fills is one nobody else holds.

[`NpyFile.Read`](../reference/embeddings/persistence/npyfile-read.md)`(ReadOnlyMemory<byte>)`
copies nothing. `NpyBlock.Values` aliases the caller's bytes through an internal
`MemoryManager<float>` over the payload slice, and those bytes must not change while the block
lives — the contract
[`EmbeddingIndex.Load`](../reference/embeddings/search/embeddingindex-load.md)`(ReadOnlyMemory<byte>)`
already states, for the same reason and to the same kind of caller.

**`NpyBlock.OwnedArray` is filled by the stream reader and by nothing else.** It is what
[`EmbeddingIndex.FromOwnedBlock`](../reference/embeddings/search/embeddingindex-fromownedblock.md)
may adopt under [0056](0056-a-block-may-be-adopted-and-the-invariant-is-the-callers-to-keep.md),
and only the stream path has an array to surrender. A borrowed block leaves it null because there
is no array behind it, and a block built by hand leaves it null because 0056's ownership transfer
is reached through the method that documents it or not at all.

What the four routes cost, against numpy's one copy:

| route | copies |
| --- | ---: |
| `Read(Stream)` → `FromBlock` | 2 |
| `Read(Stream)` → `FromOwnedBlock(block.OwnedArray)` | **1** |
| `Read(ReadOnlyMemory<byte>)` → `FromBlock` | **1** |
| `Read(ReadOnlyMemory<byte>)` → `FromOwnedBlock` | not available |

The fourth is refused rather than made to look available: a view has no array to surrender, so
`OwnedArray` is null and the adopting factory cannot be reached with it.

## Consequences

- **What was refused** is a view on every path — `NpyBlock.Values` aliasing the payload whether
  the bytes came from a stream or from the caller. It removes the byte-to-float copy without
  restructuring the read, which is why it looked cheapest. It caps the chain at two copies,
  because a view cannot be adopted and so forecloses removing the copy into the index, and it
  charges an aliasing contract to every caller including the one who only passed a `FileStream`.
  Reading into the array dominates it: one copy rather than two, and no contract at all on the
  path most callers take.
- **The `netstandard2.0` split is a consequence of this shape, not a discovery made under it.**
  Reading a stream straight into a destination of one's own choosing needs
  `Stream.Read(Span<byte>)`, and `Stream.ReadExactly` with it; both are .NET 7 and later. The
  shared `StreamFill.Exactly` reads into the destination directly on `net10.0` and stages through
  an 80 KB chunk otherwise, so the stream overload costs one copy there and two here — one public
  API and one behaviour at two speeds, the split
  [`VectorMath.Dot`](../reference/embeddings/search/vectormath-dot.md) already makes and which
  `StreamFill`'s own remarks name as its precedent. The memory overload copies nothing on either
  target, nothing about it depending on a `Stream` API.
- **What would change this decision** is a caller found holding a `NpyBlock` past the lifetime of
  the bytes it borrowed — a block cached beyond a pooled buffer's return, or read from memory that
  is then rewritten. That would make the memory overload's contract the wrong default and argue
  for a copying overload beside it, which is an addition to a published package rather than a
  change to one.
