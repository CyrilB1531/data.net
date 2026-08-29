# 0320 — rapidfuzz gets 3x more out of the LCS recurrence than we do

**Issue:** [#0320](https://github.com/CyrilB1531/lodestar/issues/0320) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-20

## Problem

The nightly put `Indel` **2.40× behind rapidfuzz at 512** while `Levenshtein` sat at **1.08×** — same run, same corpus. Both take the blocked bit-parallel path and both pay the same equality table, **so a cost they share cannot explain a gap only one of them has.**

Read the other way it is sharper: rapidfuzz's `Indel` is 3.03× faster than its own `Levenshtein`, where ours was 1.37× faster than ours.

## What the diagnosis found

`BitParallelLcs.TryBlocked` called `Advance(...)` **once per text character**. `Myers.TryBlocked` writes its block loop out by hand and says why in the file's own header — *"It is also the hot path: helper calls here cost measurably."* The LCS kernel, written later in [#273](https://github.com/CyrilB1531/lodestar/issues/273), had not inherited that.

## What was measured

**1.10× at length 512**, four replications interleaved with `Levenshtein` as an untouched control, on an i7-4770S. **Nothing at 128** — a 110-character pattern spans two 64-bit blocks against eight at 512, so inlining a two-iteration loop saves little.

## What was left

At 12.6 µs against 2.9 the kernel was still ~4.3× behind, and the remainder is neither the helper nor the table's 500 ns clear. **What remained was algorithmic**, and became [#357](https://github.com/CyrilB1531/lodestar/issues/357).
