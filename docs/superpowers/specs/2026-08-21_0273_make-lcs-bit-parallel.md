# 0273 — Make LCS bit-parallel, which is where Indel's fast path lives

**Issue:** [#0273](https://github.com/CyrilB1531/lodestar/issues/0273) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-21

## Problem

`Indel` — and therefore `fuzz.ratio` — reaches its answer through the LCS recurrence, which had only a dynamic program. The bit-parallel work had gone into Levenshtein's Myers kernel, which `Indel` does not take.

## What the corpus had to gain first

**Every bucket of `bench/corpus/pairs.json` was ASCII**, so no number on the page described what either side does above Latin-1, and a later sweep ([#407](https://github.com/CyrilB1531/lodestar/issues/407)) could not calibrate a gate against a corpus exercising only the Latin regime. There are eight buckets now: the same four lengths from 27 Latin symbols, and four more from 27 **CJK** ones.

**CJK and not emoji, and the reason is in the generator's own docstring.** ASCII was chosen so UTF-16 units and code points coincide and both sides measure the same quantity. CJK is inside the BMP, so that survives; a supplementary character is one code point and two units and would break it.

## What shipped

The blocked bit-parallel LCS kernel, its property tests against the dynamic program — which is what later caught a wrong reasoning in [#357](https://github.com/CyrilB1531/lodestar/issues/357) — and the wide half of the corpus.
