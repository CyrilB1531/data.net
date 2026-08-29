# 0413 — The blocked equality table is sized from the pattern length

**Issue:** [#0413](https://github.com/CyrilB1531/lodestar/issues/0413) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-21

## Problem

[#302](https://github.com/CyrilB1531/lodestar/issues/302) sized the blocked routes' side rows from the pattern's **length**, so the table went from `256 × ceil(m/64)` words to about **m² / 32** — for every pattern, an ASCII one included. **A pattern of a thousand `'a'`s allocated the rows for two thousand wide symbols it does not have**, and the table is `Clear()`ed after renting, so that is *touched* memory rather than reserved address space.

## The overflow under it

**Past m ≈ 262 000 the product wrapped in unchecked `int`.** At 262 017, `Rent` threw `ArgumentOutOfRangeException` out of a path that had promised to fall back rather than throw.

## What was decided

**Size from the pattern's characters above U+00FF, not from its length** — which is the quantity the side table actually holds — and a pattern too long to tabulate takes the dynamic program **instead of wrapping in `int`**. A fallback that throws is not a fallback.

## What shipped

The sizing fix and the guarded fallback, with the changelog carrying it as a bug fix rather than a performance change.
