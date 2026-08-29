# 0212 — A curve is a shape this package had nowhere

**Issue:** [#0212](https://github.com/CyrilB1531/lodestar/issues/0212) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-19

## Problem

[#174](https://github.com/CyrilB1531/lodestar/issues/174) shipped `BrierScore` and `LogLoss` and left the third member of the calibration family out, because **a curve is a shape this package had nowhere**. `roc_curve`, `precision_recall_curve` and `det_curve` all return several parallel arrays rather than a number.

## What was settled

**A sealed class per curve** — [decision 0040](../../decisions/0040-a-curve-is-a-sealed-class-per-curve.md). Not a tuple, not an array of arrays: a named type per curve, so the arrays cannot be mixed up by position.

## Two things the issue assumed that turned out otherwise

Both read from scikit-learn 1.9's own source rather than transcribed:

- **`strategy='quantile'` takes its edges from `np.percentile`**, a linear interpolation between neighbouring order statistics — **not** the weighted percentile [decision 0024](../../decisions/0024-weighted-percentile.md) pinned for the medians. The two disagree in the third decimal, so `WeightedPercentile` is deliberately not reused and the remark on `Percentile` says why.
- **There is no `sample_weight` at all**, where both its siblings take one.

## What shipped

Three curve types, `calibration_curve` on the same shape, and the reference pages that say which module each comes from.
