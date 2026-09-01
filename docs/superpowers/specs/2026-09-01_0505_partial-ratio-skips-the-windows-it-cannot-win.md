# 0505 — `PartialRatio` skips the windows it cannot win

**Issue:** [#505](https://github.com/CyrilB1531/lodestar/issues/505) ·
**Status:** accepted · **Date:** 2026-09-01

## Problem

The last losing row of [#438](https://github.com/CyrilB1531/lodestar/issues/438)'s Fuzzy box.
[#494](https://github.com/CyrilB1531/lodestar/issues/494) closed the other two by deleting
allocations; this one **already allocated nothing**, so there was no waste to remove and it did
not move. The distance was the work done.

`SlideMax` walks the shorter string across the longer and computes a full
`Indel.NormalizedSimilarity` at every offset — `n + m − 1` of them, each one a bit-parallel LCS
over the window. Every offset paid, including the ones that could not possibly hold the answer.

## The bound

An Indel alignment inserts and deletes but never substitutes, so it can match **at most the
multiset intersection** of its two operands. With `common` characters shared between the pattern
and the window,

```text
similarity ≤ 2 · common / (m + window)
```

which is exact, cheap, and monotone in what the window contains. A window whose ceiling cannot beat
the best score found so far cannot change the maximum, so it is skipped without being scored. The
result is identical by construction — this removes work, never a candidate.

Maintaining `common` costs nothing per window. Both `start` and `end` only ever advance across the
slide, so each character of the text enters the window once and leaves once: the count is
maintained, never recomputed.

`MatchCeiling` is a `ref struct` over two `stackalloc` tables of 257 `int`. Latin-1 gets a slot
each and everything above shares the last one — over-counting there can only **raise** the ceiling,
so a window is never skipped that should have been scored. That keeps one code path instead of a
wide-character fallback.

The extraction into `MatchCeiling` is also what keeps `SlideMax` under the cognitive-complexity
limit the analyzer enforces; inline, the same logic read 23 against the 15 allowed.

## What it bought

The container was markedly more loaded during this lot than during #494's — `Ratio`, whose code
did not change, reads 3.3 against the incumbent here where it read 2.26 then. So the honest
measurement is an **A/B in the same window**, taken minutes apart on the same host by stashing the
change:

| | Lodestar | FuzzySharp | ratio |
| --- | ---: | ---: | ---: |
| before | 22 195 ns | 17 805 ns | 0.80 |
| **after** | **17 998 ns** | 17 944 ns | **1.00** |

**1.23× on the same host, and parity with the incumbent** — which is what #505 asked for and no
more. It is not a win, and reporting it as one would be the mistake #438 exists to prevent.

Allocation stays at zero, which was the one thing the previous implementation already had right.

## Testing

`tests/oracles/fuzz.json` replays `partial_ratio` and `wratio` against rapidfuzz, and
`FuzzOracleTests` runs it. Every case passes unchanged — no expectation was touched, which is the
whole safety net for a pruning rule: a bound that were ever too tight would show up here as a
wrong maximum rather than as a slower run.

The full suite passes on both target frameworks: **4 405 tests**, same counts as before.
