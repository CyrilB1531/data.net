# 0280 — Samples: one file per API class, discoverable by name

**Issue:** [#0280](https://github.com/CyrilB1531/lodestar/issues/0280) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-19

## Problem

**Someone meeting `MutualInformation` cannot guess its example lives in `Lot5Metrics.cs`**, 591 lines under a name that mentions neither. Neither a file-name search nor an IDE's go-to-file reaches it; only a full-text search does, and only if they think to try.

## What was decided

One file per public class, named after it — [decision 0041](../../decisions/0041-a-sample-is-named-after-its-class.md) — with two edges the ADR carries:

- **An enum gets no file.** It is demonstrated through the class whose parameter it is, and a file exercising one alone would have to invent a use.
- **An internal type gets none either**, and that is not a convention but the samples' purpose: a consumer cannot reach it, so a sample proving it survives packaging would be proving nothing.

## What shipped

`Lodestar.Text` converted first — 35 classes, one file each — with `tools/check_sample_coverage.py` enforcing it per package, and the remaining three packages still on their `Lot*` files until their own lots land.
