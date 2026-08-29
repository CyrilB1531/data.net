# 0216 — Three ranking metrics omit sample_weight, and three equivalence rows call them identical

**Issue:** [#0216](https://github.com/CyrilB1531/lodestar/issues/0216) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-16

## Problem

`Dcg.Score`, `Ndcg.Score` and `TopKAccuracy.Score` took no `sampleWeight`. The reference has had one on all three since forever, and **three rows of `docs/equivalence.md` called them identical anyway** — which is the half that made a reader's map wrong rather than merely incomplete.

## What had to be measured, and the fixture that was wrong first

**`top_k_accuracy_score(normalize=False)` sums the weights of the hits rather than counting them.** With weights `[1,1,1,5]` it returns `3.0` — the same as unweighted, because all three hits there carry weight 1, which is why the first fixture measured nothing. Weighted `[5,1,1,1]` it returns `7.0`. Every fixture frozen here now puts a weight other than 1 on a sample that **hits**.

**That path never divides**, so a zero-sum weight vector gives `0` where the fraction raises. Both reproduced.

## What shipped

The parameter on all three, new corpus rows that exercise it, and the three equivalence rows corrected.
