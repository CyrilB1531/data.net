# 0300 — The equality table is sized to Latin-1 rather than to the pattern

**Issue:** [#0300](https://github.com/CyrilB1531/lodestar/issues/0300) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-20

## Problem

The bit-parallel kernels allocated a 256-entry equality table on every call regardless of the pattern, **and that costs twice**: the allocation itself, and the zeroing of entries no pattern character will ever index.

## What followed from it

Sizing the table to the pattern is the fix, and it opened the question [#301](https://github.com/CyrilB1531/lodestar/issues/301) and [#302](https://github.com/CyrilB1531/lodestar/issues/302) answer — what to do about a pattern whose characters do not fit a dense table at all. The three are one thread: size it to the pattern, stop zeroing what `stackalloc` already zeroed, and give the rare characters a side table so CJK stops falling back.

## What shipped

The pattern-sized table, and with [#208](https://github.com/CyrilB1531/lodestar/issues/208) a measured **2.09× for Levenshtein and 2.19× for Indel** on the length-32 bucket.
