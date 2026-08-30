# 0378 — Deflate the artifact: the whole 1.34x back, without a second format

**Issue:** [#0378](https://github.com/CyrilB1531/lodestar/issues/0378) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-20

## Problem

Deflate takes back the artifact's 1.33× base64 expansion **almost exactly**, which is what the issue opened on. What it *costs* was never measured, and that turned out to be the whole answer.

## What was measured

**26.67× the save and 7.19× the load** for gzip Fastest — on a path [#323](https://github.com/CyrilB1531/lodestar/issues/323), [#324](https://github.com/CyrilB1531/lodestar/issues/324), [#336](https://github.com/CyrilB1531/lodestar/issues/336) and [#377](https://github.com/CyrilB1531/lodestar/issues/377) had just spent four lots making fast. On the benchmark corpus's larger index, 76.8× and 14.8×.

**Brotli Fastest dominates deflate on all three axes and still costs 3.68× the save.** `BrotliStream` does not exist on `netstandard2.0`, so gzip is the recipe this project can publish.

## Two of the issue's three questions dissolved under measurement

`Load` already reads a compressed artifact when the caller wraps the stream — there was nothing to build for the read side.

## What was decided

**The library does not compress and does not offer an option to.** The caller wraps the stream on both sides, which works today and needed no library change. [ADR 0044](../../decisions/0044-compression-belongs-to-the-caller.md) records it, and the embeddings guide documents the recipe with its price.
