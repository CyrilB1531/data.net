# 0396 — LoadAsync still reads an oversized artifact into one array

**Issue:** [#0396](https://github.com/CyrilB1531/lodestar/issues/0396) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-21

## Problem

[#377](https://github.com/CyrilB1531/lodestar/issues/377) taught `EmbeddingIndex.Load` to read past the CLR's `byte[]` limit in segments, **and stopped there.** `LoadAsync` kept reading into one array, so **the same artifact loaded one way and threw the other.**

**Not a missing feature but two overloads of one method disagreeing**, discovered at the size where it hurts.

## What was decided

`JsonArtifact.ReadAllSegmentsAsync` is the counterpart, and `LoadAsync` takes the same decision on the same threshold, **through the same internal seam a test drives at kilobytes** rather than at two gibibytes. The chain they build is one implementation, so they cannot drift apart again — which is the actual fix, the parity being only its symptom.

A cancelled read throws rather than parsing a partial chain.

## What shipped

The async segmented read, and the seam that makes the pair testable at a size a test can afford.
