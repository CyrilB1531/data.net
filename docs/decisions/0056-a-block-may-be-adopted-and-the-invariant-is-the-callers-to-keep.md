# 0056 — A block may be adopted, and the invariant is the caller's to keep

**Status:** accepted · **Date:** 2026-08-29 · **Refines:** [`0053`](0053-the-payload-buffer-is-not-pooled-because-residency-outlives-the-load.md)

## Context

[#474](https://github.com/CyrilB1531/lodestar/issues/474) gave
[`EmbeddingIndex`](../reference/embeddings/search/embeddingindex.md) a way to take a whole block of
vectors, which [0055](0055-the-artifact-gets-a-binary-sidecar-once-a-block-can-be-ingested-whole.md)
made the sidecar's precondition. Two factories came out of it:
[`EmbeddingIndex.FromBlock`](../reference/embeddings/search/embeddingindex-fromblock.md), which
copies the block once, and
[`EmbeddingIndex.FromOwnedBlock`](../reference/embeddings/search/embeddingindex-fromownedblock.md),
which does not copy it at all — the caller's array becomes the index's backing store.

The second one is the subject here, because it puts a permanent obligation on a caller and the
library has refused exactly that shape once already.

[0053](0053-the-payload-buffer-is-not-pooled-because-residency-outlives-the-load.md) ended on the
sentence this decision refines:

> the caller who wants pooling can rent and pass the bytes to
> [`EmbeddingIndex.Load`](../reference/embeddings/search/embeddingindex-load.md)`(ReadOnlyMemory<byte>)`,
> which parses in place and never pools.

That is an exposure invariant, and it has two halves. The library **reads** a caller's memory,
which is why the overload exists at all; and the library **never takes** it — no buffer of the
caller's is retained, written to, or handed to a pool. [0054](0054-the-payload-buffer-is-pooled-after-all-because-the-collection-is-the-cost.md)
kept both halves when it reversed the pooling: the rent lives at the `Load(Stream)` call site and
not one line lower, precisely so that the memory overload's exposure stays the caller's own.

**`FromOwnedBlock` holds the mirror image of that invariant.** Load's exposure is the library
reading a caller's memory for the length of one call, and it ends when the call returns —
everything the index keeps afterwards is its own. Adoption runs the other way and does not end:
the index reads the caller's array for as long as the index lives, so the obligation moves to the
caller and stays there. That is the interesting part, and the part 0053 never had to price. A
breach is silent in a way a borrow's cannot be — an array returned to an `ArrayPool` and re-rented
elsewhere becomes this index's embeddings, and the index goes on scoring queries against another
renter's bytes without raising anything. With
[`BlockNormalization.Normalize`](../reference/embeddings/search/blocknormalization.md) the transfer
is visible sooner and just as permanent: the array is normalized in place, so the caller's own
values change under it.

## Decision

**Both factories ship public, and the invariant is the caller's to keep.**

[`FromBlock`](../reference/embeddings/search/embeddingindex-fromblock.md) is what the
documentation points a reader at: it costs one pass over the block, asks nothing of the caller, and
leaves the array untouched and reusable.
[`FromOwnedBlock`](../reference/embeddings/search/embeddingindex-fromownedblock.md) is the opt-in
beside it, for a caller who has measured that copy and does not want to pay it. Taking it means
three things, and the library cannot check any of them: the array is not written to afterwards, it
is never returned to a pool, and `Normalize` rewrites its values in place.

The name carries the whole of it. `FromOwnedBlock` says the block is owned by the index, not lent
to it; a caller who reads the name and reaches for it anyway is choosing the trade rather than
walking into it.

## Consequences

- **What was refused** is the copying factory public with the adopting one an internal seam used
  only by the load path. It keeps the permanent invariant out of every caller's hands, and
  [`NpyFile.Read`](../reference/embeddings/persistence/npyfile-read.md)'s freshly allocated array —
  which nobody else holds — is exactly the case it would serve. It was refused for reach: a caller
  holding a block from anywhere else (a model's output, a memory-mapped file, a column store)
  would have no way to avoid a copy the library can see is unnecessary, and would have no way to
  ask for one.
- **What would change this decision** is evidence that the invariant is broken in practice — an
  issue reporting scores that drift, or a caller found returning an adopted array to a pool. The
  reversal is to make
  [`FromOwnedBlock`](../reference/embeddings/search/embeddingindex-fromownedblock.md) internal,
  which is a source-breaking change to a published package and therefore a major version, not a
  patch.
- **The invariant is asserted, not only written down.** `EmbeddingIndexBlockTests` writes to an
  adopted array after the index was built and watches the score move, so a future change that
  quietly copies instead of adopting fails a test rather than passing one.
- **There is no benchmark row for adoption**, and none is missing. It assigns four fields and is
  constant time whatever the block's size, so its ceiling is the block read that already has a row;
  a row of its own would publish noise. `bench/README.md` §12 says so beside the rows.
