# 0047 — One bit-parallel gate per kernel, not one per alphabet

**Status:** accepted · **Date:** 2026-08-21 · **Amends:** [`0043`](0043-the-equality-table-is-sized-to-the-pattern.md)

## Context

`Levenshtein.MyersMinPatternLength` and `Lcs.BitParallelMinPatternLength` are both 8,
swept over an ASCII corpus in #208 and #273. Since #302 and #382 a pattern above
U+00FF takes the bit-parallel kernel too, through the side table 0043 records — so a
constant calibrated on one alphabet governs two.

Issue #383 measured the two kernels on synthetic bands and found they no longer cross
in the same place on wide input: LCS is under the dynamic program at band 8, Myers straddles
parity there and does not clearly win until band 10. A benchmark cannot place a gate,
though — below it the dispatch sends both rows to the DP, so the ratio is 1 exactly
where the crossing would be read. Issue #406 gave the corpus four CJK
buckets so the sweep #208 used could reach the wide regime at last.

## Decision

**One constant per kernel, at 8, shared by both alphabets.** The sweep is in
[`../guides/performance.md`](../guides/performance.md): each gate value measured over
the whole corpus, two passes per kernel in opposite order, agreeing to 5.3%.

Every curve rises with the gate, in both alphabets and both kernels, and **the wide
regime is consistently the less sensitive of the two** — raising the gate from 4 to 16
costs Latin 2.13× on Levenshtein and 2.47× on Indel, against 1.51× and 1.50× on CJK.

## The two shapes refused

- **A gate per alphabet.** It would give precision to the regime that asks for least:
  CJK's curve is the flat one. The dispatch would also have to know the pattern's width
  before choosing, which is the scan the kernel performs anyway — so the test would be
  paid by the Latin-1 path, which is `fuzz.ratio`'s, to benefit the path that gains
  little. 0043 refused to charge that path for the side table; this refuses the same
  bargain in the gate.
- **Moving the shared value.** Both alphabets prefer 4 over 8 in the only bucket whose
  patterns straddle the gate, and that is not enough to act on for the reason #208 gave
  and this corpus does not lift. See below.

## Consequences

- **The sweep answers the alphabet question and not the value question**, and the two
  should not be conflated again. Bucket 32 is the only one whose patterns straddle any
  candidate gate: 128 and 512 trim to medians of 110 and 493, far above every value,
  and bucket 8 trims to a median pattern of **0** — 10% of 8 characters mutated leaves
  nothing after `Affixes.Trim`. Every row below the gate therefore still rests on one
  bucket.
- **The wide buckets reproduce that hole rather than filling it.** CJK's length-8
  bucket also trims to 0, the edit rate being what produces it, not the alphabet. A
  bucket built to leave a short pattern is what would answer the value question, and it
  does not exist yet.
- **Summing ns/pair across buckets is the wrong statistic here**, and picks a different
  winner: the 512 bucket is roughly 95% of any total and the gate cannot touch it, so
  the sum reports that bucket's run-to-run noise as a result about the constant.
- **The corpus is the instrument, not the gate benchmarks.** Their bands reproduce to
  about 12% on a short job; two corpus passes in opposite order agree to 5.3%. Where
  the two disagree — #383 read Myers at parity on band 8, where moving the gate from 8
  to 10 costs the CJK bucket 4.4% — the corpus is the one measuring shipped input.
