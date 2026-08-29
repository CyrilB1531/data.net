# 0052 — Pre-sizing the artifact file buys nothing on a delayed-allocation filesystem

**Status:** accepted · **Date:** 2026-08-28

## Context

[0051](0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md) took step 1 of
[#429](https://github.com/CyrilB1531/lodestar/issues/429) and left one item unbuilt.
[#432](https://github.com/CyrilB1531/lodestar/issues/432) is it: the save writes ~20 MB through
an 80 KB buffer — 252 `write` calls, each extending the file — so setting the length up front
should let the filesystem allocate once.

It could not be shown against any published row, because every save row wrote to a
`MemoryStream`. So the row came first: `embedding_index_save_file`, with `np.save` to a path as
its Python pair, on the reasoning [#336](https://github.com/CyrilB1531/lodestar/issues/336) used
for the load half — **the file path is the one a caller takes**.

## Decision

**The artifact file is not pre-sized.** `JsonArtifact.OpenWrite` keeps opening with
`FileMode.Create` and nothing sets a length.

Interleaved round-robin, ext4 on a block device — checked, not a tmpfs. The hypothesis alone:
20 589 008 bytes through an 80 KB-buffered `FileStream`, no JSON and no base64, 25 rounds a run.

| | run 1 | run 2 | run 3 |
| --- | ---: | ---: | ---: |
| plain | 5.149 ms | 4.998 ms | 5.135 ms |
| `SetLength` first | 5.177 ms | 5.081 ms | 5.016 ms |

Under 2% apart and apart in *both directions*. On the real save path, pre-sizing came out slower
in two runs of three.

**The mechanism, because an unexplained null result is not evidence.** ext4 defers allocation to
writeback and sizes it to what is there, so the per-write extension this would absorb never
happens; `ftruncate` up front only makes a sparse extent writeback still has to allocate. The
floor confirms it: `File.WriteAllBytes` of the finished artifact costs 4.86 ms against the whole
save's 7.67, and the 2.8 ms between them is the base64 encode 0051 prices at 3.211 on this
machine. Nothing is left over.

It was not free either. The estimate covers the vector block, and the head and the ids are
written past it, so pre-sizing obliges a truncation afterwards — a crash between the two leaves a
file *longer* than its document. That is a new invariant on `JsonArtifact` for zero.

## Consequences

- **#432 closes as refused, not as done** — a roadmap item someone should see was measured and
  declined rather than skipped. 0051's refusal of parallel base64 is the same argument: a sound,
  cheap, obviously right design, refused because the measurement came back empty. There the
  memory subsystem was already doing the work; here the filesystem is.
- `embedding_index_save_file` stays, and is not scaffolding for a refused change: it is the save
  row this project should have had since #336, written to a path of its own so neither direction
  measures a file the other just touched.
- **The reopening condition is a filesystem that charges per extension** — NTFS advances a
  valid-data-length and zero-fills rather than deferring. **Nothing here was measured on
  Windows**, so this covers ext4 rather than every filesystem, and the row is what would settle
  it there.
