# 0206 — The sort inside the binary ROC curve

**Issue:** [#0206](https://github.com/CyrilB1531/lodestar/issues/0206) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

## Problem

[#86](https://github.com/CyrilB1531/lodestar/issues/86) parallelised multiclass ROC-AUC and named the sort inside one binary curve as the remainder. Profiled at a million samples it is **91% of the curve** — 6 ms building the points, 91 ms sorting, 3 ms accumulating — so it was the only part worth touching.

## What was measured, including what was rejected

**Radix beats introsort above 8 192 samples.** Four LSD passes over 16-bit digits of an order-preserving encoding: **1.27× at a million, 1.58× at a hundred thousand**, on an i7-4770S with only the threshold differing between the two columns.

**Sorting an index array was measured and rejected.** The issue named it as a candidate; it wins marginally at a hundred thousand and **loses by 13% at a million**, where the gather costs more than the smaller items save.

**No parallelism, and that is the point.** A parallel sort would nest inside the region #86 already parallelises on the multiclass path. Sequential was enough.

## What shipped

The radix sort behind a measured threshold, not a guessed one.
