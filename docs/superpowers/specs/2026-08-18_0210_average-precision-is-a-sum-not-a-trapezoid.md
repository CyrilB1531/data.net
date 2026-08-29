# 0210 — average_precision_score is a sum, not a trapezoid

**Issue:** [#0210](https://github.com/CyrilB1531/lodestar/issues/0210) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

## Problem

`average_precision_score` was the ranking metric the family most visibly lacked. It is also the one most easily implemented wrongly: it **adds each recall step times the precision at that step**, and is deliberately not the trapezoid `auc(recall, precision)` applies.

## What was measured

The trapezoid reads two thresholds apart as though the curve were straight between them, and comes out **optimistic**. Against scikit-learn 1.9.0 on the worked case: `0.8333333333333333` against `0.7916666666666666`; on a row whose scores are all tied, `0.5` against `0.75`.

**Reproducing the wrong one is the mistake this issue exists to avoid**, so the frozen corpus carries the trapezoid beside every binary case and a test asserts the two never converge by accident.

## What shipped

`AveragePrecision`, reusing `BinaryRoc`'s walk — it already sorts by descending score and consumes a tied group at once, so only the accumulator differs.
