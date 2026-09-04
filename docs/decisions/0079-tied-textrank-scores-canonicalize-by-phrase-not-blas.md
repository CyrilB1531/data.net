# 0079 — Tied TextRank scores in the oracle canonicalize by phrase, not by BLAS

**Status:** accepted · **Date:** 2026-09-04

## Context

[#541](https://github.com/CyrilB1531/lodestar/issues/541): the *Oracles are reproducible* job
failed on [#540](https://github.com/CyrilB1531/lodestar/pull/540), a pull request whose diff
removes an unrelated constructor and touches nothing near `keywords_textrank.json`. The failure:

```text
keywords_textrank.json: cases[0].expected[1].phrase: "diophantine" vs "linear"
keywords_textrank.json: cases[0].expected[2].phrase: "linear" vs "diophantine"
keywords_textrank.json: cases[0].expected[3].phrase: "criteria" vs "natural"
keywords_textrank.json: cases[0].expected[4].phrase: "natural" vs "criteria"
```

Both are swaps of adjacent entries, not value drift — measured, the gaps between adjacent scores
in `cases[0]`:

```text
numbers      0.526895906655717     gap 5.820e-02
diophantine  0.4686942795397464    gap 2.776e-16   <-- swapped pair
linear       0.46869427953974613   gap 1.906e-01
criteria     0.27808395073496167   gap 1.110e-16   <-- swapped pair
natural      0.27808395073496156
```

Both swapped pairs sit `1e-16`-`1e-15` apart — one or two units in the last place, far inside the
`1e-9` `tools/compare_oracles.py` and every oracle suite compares floats at. [Decision
0077](0077-the-keyword-extractors-take-their-oracles-lists-and-not-their-own.md) §3 already forced
the generator to select TextRank's dominant eigenvector by eigenvalue rather than by
`scipy.linalg.eig`'s column position, because a repeated eigenvalue leaves that column order
BLAS-build-dependent. That fixed the **values**. It did not fix their **order**: when two
published scores are tied this close, `summa.keywords.keywords`'s descending sort is ordering them
by the same floating-point noise 0077 already named, and a different BLAS build breaks that near-
tie the other way. Applying the canonicalization below surfaced two further ties, in `cases[1]`
and `cases[2]`, that had frozen in non-canonical order without ever tripping a CI failure — the
same exposure, on ties no runner had yet happened to break the other way.

`TextRankOracleTests.AssertRankingMatches` (`tests/Lodestar.Text.Tests/Keywords/TextRankOracleTests.cs`)
already treats this class of tie as free: it partitions the expectation into maximal runs of
adjacent entries within `1e-9` of each other and permits any permutation inside a run, checking
only that a real rank gap lands the right phrases and scores at the right position. The corpus and
that test were not agreeing by construction — the corpus was frozen in whatever order one
generating machine's BLAS produced, and the test's permutation allowance was doing the actual work
of tolerating it, silently, until two machines' noise disagreed on the same tie in a way the test's
own run-relative sort happened not to absorb.

## Decision

`generate_keywords_textrank` canonicalizes tie order before freezing, using the exact model the
test already applies on replay: partition the published result into maximal runs of **adjacent**
entries whose scores are within `1e-9`, and sort each run by phrase, ordinally. Entries outside a
run keep the reference's own order. The two rejected pairs settle into `diophantine, linear` and
`criteria, natural` (already ordinal order, so `cases[0]` does not move on a machine that already
produced that order); `cases[1]`'s `learning, learn` becomes `learn, learning`, and `cases[2]`'s
`trilling, purring` and `rodents, carnivorous` become `purring, trilling` and `carnivorous,
rodents`.

**Rejected: teaching `tools/compare_oracles.py` to tolerate a reordering.** The gate is right —
[decision 0073](0073-the-oracle-gate-compares-numbers-not-bytes.md) already settled that it
compares what the suites compare, positionally, because a corpus that reordered an array has
changed in a way no float tolerance explains for every *other* corpus it walks. Weakening it to
permit a permutation would serve this one corpus's convenience at the cost of the exact-order
guarantee every other corpus relies on it for, and would still leave the corpus itself
machine-dependent — the fix belongs in what freezes the order, not in what checks it.

## What enforces it

`_canonicalize_tied_runs` in `tools/generate_oracles.py`, called on `summa.keywords.keywords`'s
published result before it is written to a case. `tests/oracles/keywords_textrank.json` was
regenerated twice and compared with `tools/compare_oracles.py` each time: `ok 98 corpora agree`
on both runs, and `git status` clean after the second, so the generator is a fixed point. Against
the pre-fix corpus the only movement is the ties named above; nothing else in any of the 98
corpora moved. What this cannot show from one machine is the BLAS disagreement itself — that
evidence is the CI failure quoted in *Context*, which is what a canonical order removes.
`TextRankOracleTests.AssertRankingMatches` needed no change — corpus and test now agree by
construction rather than by the test's permutation allowance quietly absorbing whatever order one
machine froze.
