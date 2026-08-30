# 0406 — The bench corpus is ASCII, so no sweep can reach the wide regime

**Issue:** [#0406](https://github.com/CyrilB1531/lodestar/issues/0406) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-21

## Problem

Every bucket of `bench/corpus/pairs.json` was ASCII. **No number on the page described what either side does above Latin-1**, and [#407](https://github.com/CyrilB1531/lodestar/issues/407) could not sweep a gate against a corpus exercising only the Latin regime.

## The choice of alphabet, which is not arbitrary

**CJK, and not emoji**, and the generator's own docstring carries the reason: ASCII was chosen so **UTF-16 units and code points coincide** and the two sides measure the same quantity. CJK is inside the Basic Multilingual Plane, so that property survives. **A supplementary character is one code point and two units and would break it** — the comparison would stop being like-for-like without anything saying so.

## What shipped

Eight buckets: the same four lengths from 27 Latin symbols, and four more from 27 CJK ones.
