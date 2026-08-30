# 0302 — Lift the Latin-1 restriction on the UTF-16 bit-parallel path

**Issue:** [#0302](https://github.com/CyrilB1531/lodestar/issues/0302) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-21

## Problem

`Levenshtein.Distance`, `Lcs.SubsequenceLength` and therefore `Indel` and `fuzz.ratio` sent a pattern holding a character above U+00FF back to the dynamic program in the UTF-16 mode. The kernels were refusing an alphabet.

## What the corpus had to gain first

**Every bucket of `bench/corpus/pairs.json` was ASCII**, so no number on the page described what either side does above Latin-1. Eight buckets now: the same four lengths from 27 Latin symbols, and four more from 27 **CJK** ones.

**CJK and not emoji.** ASCII was chosen so UTF-16 units and code points coincide and both sides measure the same quantity; CJK is inside the BMP so that survives, where a supplementary character is one code point and two units and would break it.

## How the cost was proven to be zero on the Latin path

Not with a stopwatch. **The JIT's output was dumped for the method carrying the inlined single-word path, on this branch and on its parent: two identical listings, 83 instructions, addresses normalised.** That is exact, where a corpus timing could only bound it.

## What shipped

The side table on both routes — see [decision 0043](../../decisions/0043-the-equality-table-is-sized-to-the-pattern.md) — so the kernels no longer refuse an alphabet.
