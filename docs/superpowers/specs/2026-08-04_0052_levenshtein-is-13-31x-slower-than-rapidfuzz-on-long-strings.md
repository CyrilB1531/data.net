# Design — #52: blocked Myers, so long strings stop losing to rapidfuzz

**Date:** 2026-08-04 · **Issue:** #52 · **Branch:** `perf/52-blocked-myers` ·
**Checkout:** `<repo>`

## Problem

Performance is the project's central argument, and on the headline algorithm this
library loses to Python for anything but short strings.

Measured on an Intel i7-4770S, .NET 10.0.110, rapidfuzz 3.14.5, over the committed
corpus:

| Length | Python (rapidfuzz) | C# (DataNet) | |
| ---: | ---: | ---: | --- |
| 8 | 182.8 ns | **38.5 ns** | C# 4.75× faster |
| 32 | **324.3 ns** | 451.2 ns | Python 1.4× faster |
| 128 | **2 693 ns** | 36 178 ns | Python 13.4× faster |
| 512 | **21 688 ns** | 683 581 ns | Python 31.5× faster |

C# wins only at length 8, where what is really being measured is rapidfuzz's
per-call interop overhead rather than the algorithm.

**Cause:** only *single-word* Myers is implemented. Above a 64-character pattern
the fast path is skipped and `Distance` falls back to the rolling-row `O(n·m)` DP,
while rapidfuzz's C core uses blocked (multi-word) Myers — `O(n·⌈m/w⌉)` — at every
length. The measured curve matches: 32→128 is 4× the length and ~80× the time,
exactly as a DP would behave.

ADR 0004 already recorded this as backlogged, but it never had an issue, so it
appeared nowhere in the tracker.

## Decisions

### D1 — Rule out the cheap explanation first, and record the result

Before touching the algorithm, test whether the DP is simply badly written. A
char-specialised version with bounds checks elided through refs comes out
**slower**:

```text
generic  Dp<char> : 3.50 ns/cell
char-specialised  : 3.97 ns/cell
```

A scalar rolling-row DP is already at its floor. rapidfuzz's effective
0.08 ns/cell is unreachable without computing 64 cells per word operation, so the
gap is **algorithmic, never micro-architectural**.

Record it, so nobody re-litigates it in six months.

### D2 — Blocked Myers, from the published pseudo-code

Myers (JACM 1999). **Never transcribed from a copyleft implementation**, per
ADR 0003.

Bit vectors span `⌈m/64⌉` words with horizontal deltas carried word to word. Only
the last word's bit at `(m-1) mod 64` moves the score; bits above it are never
read, so leaving them set is harmless — carries propagate upward only.

### D3 — The corpus must genuinely exercise the new path, and it does not today

This is the most important decision in the change.

After implementing blocked Myers, **all 168 tests are green and prove nothing**:

```text
total cases : 1241
pattern > 64: 85
  of which Latin-1 (i.e. actually reaching blocked Myers): 0
```

The corpus's `long` family draws from BMP ranges, so every long case contains CJK,
fails the Latin-1 check, and falls back to the DP. **The new path is never
executed.**

So the corpus gains `long_ascii` and `long_latin` families — 89 cases that
genuinely reach it, agreeing with rapidfuzz.

### D4 — Append to the corpus, never insert

Appending leaves the RNG stream intact. Verify that all 1 241 pre-existing cases
keep their id and value, so the added cases are the entire corpus diff. Inserting
would renumber everything and make the diff unreadable — and unreviewable.

### D5 — The figures ship with their limit attached

The equality table is 256 entries, so **CJK and emoji patterns still take the DP**
and these numbers do not describe them. ADR 0004 and the performance guide both
say so.

The length-32 bucket remains 1.4× behind on the single-word path. That is a
different cause and wants its own measurement rather than a guess — say so instead
of quietly rounding it away.

## Out of scope

- Extending the fast path to the code-point mode (`Distance<int>`) or to `Indel`.
  Both are DP-only and both are worth doing; neither is this branch.
- The length-32 gap.

## What "done" means

Blocked Myers implemented from the published description; the corpus extended so
89 cases actually reach it; before/after recorded in ADR 0004 and the performance
guide with the Latin-1 limit stated; the pre-existing 1 241 cases unchanged.
