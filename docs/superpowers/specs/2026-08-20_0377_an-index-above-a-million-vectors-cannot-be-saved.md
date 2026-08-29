# 0377 — An index above ~1 million vectors cannot be saved

**Issue:** [#0377](https://github.com/CyrilB1531/lodestar/issues/0377) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-20

## Problem

An index artifact had to fit **one `byte[]`**, because `ReadAllBytes` returns one. The CLR's array ceiling was therefore the index's ceiling, and **the format's 1.34× expansion came straight off it**: about 1 043 000 vectors at 384 dimensions where a raw block allowed 1 398 000. A corpus of 1.2 million embeddings — an ordinary size for semantic search — could not be written at all.

**Worse, the wall sat behind one the documentation invites the caller to remove.** `CheckTotalBytes` refuses cleanly at 256 MB; raise `MaxTotalBytes` past the array ceiling, as a user with a large corpus will, and the growable path fails inside a `MemoryStream` instead of at a check that names anything.

## The alternative that was not needed

[#374](https://github.com/CyrilB1531/lodestar/issues/374) proposed moving the vector block out of readable text, with artifact compatibility as the gate. **That is not needed: the ceiling is a property of the buffer, not of the format.** `Utf8JsonReader` reads a `ReadOnlySequence` of many segments, and the machinery downstream already anticipated it — `ReadToken` branches on `HasValueSequence`, and `Decode`'s fast path already falls back for a segmented token.

## What shipped

The artifact read into segments past the threshold and parsed from the sequence. **The bytes on disk do not move, any earlier artifact still loads, and no ADR amends 0011.** The ceiling becomes the decoded data — the `float[]` itself, some 5.5 million vectors at 384 dimensions.

`MaxSingleBuffer` moved from a private const to `ArtifactLimits` **for testability and nothing else**: proving a two-gibibyte path by allocating two gibibytes is a test nobody runs, which is how such a path ships uncovered.
