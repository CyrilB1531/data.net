# 0301 — The single-word equality table is zeroed on every call

**Issue:** [#0301](https://github.com/CyrilB1531/lodestar/issues/0301) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-20

## Problem

The equality table is **indexed by the character**, so a pattern holding anything above U+00FF could not be represented and both single-word kernels refused — **CJK and emoji never took the bit-parallel path in the UTF-16 mode.**

## The alternative that was measured and rejected

**Generalising the whole table is what the code-point path already does**, and [#208](https://github.com/CyrilB1531/lodestar/issues/208) measured it crossing the dynamic program at a pattern of 10 where the character path crosses at 8. **The renaming costs more than the 256-entry table it replaces**, so generalising was the wrong trade.

## What was decided

**The dense table stays exactly as it is for the common characters**, and the rare ones go in an open-addressed **side table** beside it, built only when the pattern has one. A pattern of pure ASCII pays nothing.

## What shipped

The side table on both single-word kernels, and the zeroing removed where `stackalloc` had already done it.
