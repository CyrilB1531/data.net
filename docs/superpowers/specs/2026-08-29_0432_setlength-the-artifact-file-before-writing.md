# 0432 — SetLength the artifact file before writing it

**Issue:** [#0432](https://github.com/CyrilB1531/lodestar/issues/0432) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-29

## Problem

The save writes ~20 MB through an 80 KB buffer — **252 `write` calls, each extending the file** — so setting the length up front should let the filesystem allocate once.

**The issue blocked itself and said so**: every persistence row reporting a save wrote to a `MemoryStream`, so no published figure could have shown it. `embedding_index_save_file` had to exist first, with `np.save` to a path as its Python pair.

## The measurement, on ext4 on a block device

20 589 008 bytes through an 80 KB-buffered `FileStream`, no JSON and no base64, 75 interleaved rounds:

| | run 1 | run 2 | run 3 |
| --- | ---: | ---: | ---: |
| plain | 5.149 ms | 4.998 ms | 5.135 ms |
| `SetLength` first | 5.177 ms | 5.081 ms | 5.016 ms |

**Under 2% apart, and apart in both directions.** On the real save path, pre-sizing came out slower in two runs of three.

## The mechanism, because an unexplained null result is not evidence

**ext4 defers allocation to writeback and sizes it to what is there**, so the per-write extension this would absorb never happens; `ftruncate` up front only makes a sparse extent writeback still has to allocate. The floor confirms it: `WriteAllBytes` of the finished artifact costs 4.86 ms against the save's 7.67, and the 2.8 ms between them is the encode — **nothing left over.**

## What shipped

The row, which outlives the refused change, and [ADR 0052](../../decisions/0052-pre-sizing-the-artifact-file-buys-nothing-on-a-delayed-allocation-filesystem.md). **Nothing was measured on Windows**, and the reopening condition is a filesystem that charges per extension.
