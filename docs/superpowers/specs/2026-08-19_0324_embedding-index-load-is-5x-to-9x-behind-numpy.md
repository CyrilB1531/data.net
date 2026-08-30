# 0324 — embedding_index_load is 5x to 9x behind numpy

**Issue:** [#0324](https://github.com/CyrilB1531/lodestar/issues/0324) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-19

## Problem

The load direction was the furthest behind Python anything here published, and [ADR 0011](../../decisions/0011-persistence-format.md) had priced it — base64 inside JSON against `numpy.load`'s raw block — without anyone revisiting it since.

## What the profile said, and it was not the format

`EmbeddingIndex.Load` instrumented phase by phase on an i7-4770S over the artifact's own 20 589 007 bytes:

| phase | cost | share |
| --- | ---: | ---: |
| reading the payload into a buffer | ~4.5 ms | ~29% |
| vector block — allocation **and** base64 decode | ~10.8 ms | ~50% |
| finite scan, SIMD | ~1.6 ms | ~9% |
| 10 000 ids | ~0.7 ms | ~5% |

**Replacing the decode with a `memcpy` of the same byte count** is what separated the format from the implementation: decoding costs ~1.3 ms *over* moving the bytes. **The budget is allocation and page commit, not the encoding.**

## What was decided, and the sentence that outlived the lot

`GC.AllocateUninitializedArray` recovered only the zeroing — *"most of that phase is the operating system committing pages on first touch, which no allocation strategy avoids."* That sentence is what [#435](https://github.com/CyrilB1531/lodestar/issues/435) is the exception to, and what [ADR 0051](../../decisions/0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md) found again on the write side.

## What shipped

Three passes where there were five, **35.35 MB allocated to move 15 MB of floats where it used to take 90**, and a 2.9× improvement with the format untouched.
