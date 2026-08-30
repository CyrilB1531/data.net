# 0357 — Blocked LCS kernel: close the residual gap at 128 and 512

**Issue:** [#0357](https://github.com/CyrilB1531/lodestar/issues/0357) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-20

## Problem

[#320](https://github.com/CyrilB1531/lodestar/issues/320) took the mechanical half — a helper called once per text character — and left the rest here, **having measured that the table's per-call clear was 4% of the call and concluded the remainder was algorithmic.** It was, and it was one line.

## The reasoning, checked before a line moved

`Advance` threaded **two** chains between words: the addition's carry and the subtraction's borrow. **The borrow was provably always zero.** `u` is `v & peq`, so its set bits are a subset of `v`'s, and subtracting a bit-subset cannot borrow — `v - u` is exactly `v & ~u`.

**Checked over 200 000 random 64-bit draws before the edit**, and the property tests [#273](https://github.com/CyrilB1531/lodestar/issues/273) added against the dynamic program are what would have caught the reasoning being wrong.

## Why the asymmetry belongs to LCS and not to Myers

Myers carries substitution and its subtraction has no such shape, **which is why its blocked loop rightly keeps both chains** — and why our blocked Myers sat at parity with rapidfuzz's while our blocked LCS did not. One serial dependency too many, in a loop running `text.Length × blocks` times.

## What was measured

**1.56× at 512 and 1.43× at 128**, four replications interleaved with `Levenshtein` as control, no overlap between the series. Unlike #320 this moves 128 too, and that is the mechanism: **amortising a call pays in proportion to the blocks it covers; removing a dependency pays wherever the loop runs.**
