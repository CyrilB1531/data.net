# 0286 — calibration_curve: the reliability curve #174 split out

**Issue:** [#0286](https://github.com/CyrilB1531/lodestar/issues/0286) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-19

## Problem

[#174](https://github.com/CyrilB1531/lodestar/issues/174) shipped `BrierScore` and `LogLoss` and left the calibration family's third member out, because a curve was a shape the package had nowhere. [#212](https://github.com/CyrilB1531/lodestar/issues/212) settled that shape, so this is the same shape and one more type.

## Two things the issue assumed that turned out otherwise

Both read from scikit-learn 1.9's own source rather than transcribed:

- **`strategy='quantile'` takes its edges from `np.percentile`** — a linear interpolation between neighbouring order statistics, **not** the weighted percentile [decision 0024](../../decisions/0024-weighted-percentile.md) pinned for the medians. The two disagree in the third decimal, so `WeightedPercentile` is deliberately not reused and the remark on `Percentile` says why.
- **There is no `sample_weight` at all**, where both its siblings take one.

## What shipped

`CalibrationCurve` on [decision 0040](../../decisions/0040-a-curve-is-a-sealed-class-per-curve.md)'s shape, its frozen corpus, and the pages that say which module it comes from.
