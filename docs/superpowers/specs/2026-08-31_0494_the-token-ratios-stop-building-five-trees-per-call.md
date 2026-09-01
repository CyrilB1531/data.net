# 0494 — The token ratios stop building five trees per call

**Issue:** [#494](https://github.com/CyrilB1531/lodestar/issues/494) ·
**Status:** accepted · **Date:** 2026-08-31

## Problem

[#438](https://github.com/CyrilB1531/lodestar/issues/438)'s Fuzzy box measured
Raffinert.FuzzySharp 6.0.0 against `Lodestar.Fuzzy` over the same pair, both returning the same
value to the last digit of the double. Three of the four rows did not favour us, and the two token
ratios lost on time **and** allocation: `TokenSetRatio` at 5 824 B against 1 944 B for one
43-character pair, `WRatio` at 7 200 B against 3 128 B.

`Ratio` — the row built on `Indel` — was ours by 2.1× and allocated nothing, which is what made the
other rows a defect rather than a fact about the algorithm.

## Where the bytes were

`TokenSet` built **five `SortedSet<string>`** per call:

```csharp
var setA = new SortedSet<string>(Tokenize(a), StringComparer.Ordinal);
var setB = new SortedSet<string>(Tokenize(b), StringComparer.Ordinal);
var intersection = new SortedSet<string>(setA, StringComparer.Ordinal);
intersection.IntersectWith(setB);
var diffA = new SortedSet<string>(setA, StringComparer.Ordinal);
diffA.ExceptWith(setB);
var diffB = new SortedSet<string>(setB, StringComparer.Ordinal);
diffB.ExceptWith(setA);
```

Each is a red-black tree with a node per element, over a handful of tokens, and three of them are
copies of the first two. `WRatio` calls `TokenSetRatio` and `PartialTokenSetRatio`, so it paid for
ten.

## What changed

The sets are only ever used sorted and distinct, which two arrays give without a tree:

- **`SortDistinct`** sorts a token array ordinally in place and moves the distinct tokens to the
  front, returning how many there are. No second array — the range operator would have needed
  `RuntimeHelpers.GetSubArray`, which netstandard2.0 does not have.
- **`Partition`** walks both arrays once and fills the shared, first-only and second-only lists.
  The set version walked each tree three times to compute the same three answers.
- **`Join`** returns early when the second list is empty, which is the common case.

The enumeration order is the sorted one either way, which is what the joins depend on — and
`Join`'s `.Trim()` stays, because an empty intersection still puts a leading space in front.

## What it bought

Container run, so the times wait on a named machine
([ADR 0051](../../decisions/0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md)); the
allocation is a property of the code path.

| operation | before | after | ratio before | after |
| --- | ---: | ---: | ---: | ---: |
| `TokenSetRatio` | 3 486 ns, 5 824 B | **1 181 ns, 1 448 B** | 0.56 | **1.65** |
| `WRatio` | 4 647 ns, 7 200 B | **2 143 ns, 2 760 B** | 1.06 | **2.28** |

Both now beat the incumbent on time and on allocation, which is what #494 asked for. Three of the
four rows favour us; the fourth is below.

## `PartialRatio` is a different problem, and is filed as one

It is the one losing row that **already allocates nothing**, so there was no waste for this change
to remove and it moved by nothing. The distance is the work done: `SlideMax` scores every offset,
where rapidfuzz restricts the alignment candidates first and scores a handful. That is an algorithm
change with an oracle to hold it, and it is
[#505](https://github.com/CyrilB1531/lodestar/issues/505) rather than a widening of this lot.

## Testing

`tests/oracles/fuzz.json` replays `token_sort_ratio`, `token_set_ratio` and `wratio` against
rapidfuzz, and `FuzzOracleTests` runs it. Every case still passes, unchanged — no expectation was
touched, which is the whole safety net for rewriting a set operation into a merge.

The full suite passes on both target frameworks: **4 405 tests**, same counts as before.
