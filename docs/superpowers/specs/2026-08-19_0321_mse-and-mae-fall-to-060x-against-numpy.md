# 0321 — mse and mae fall to 0.60x against numpy at a million rows

**Issue:** [#0321](https://github.com/CyrilB1531/lodestar/issues/0321) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-19

## Problem

The nightly put `mse` and `mae` at **0.60× against numpy** at a million rows — below the gate `performance.md` sets — while `r2` on the same run stayed above it.

## What that shape means

[Decision 0027](../../decisions/0027-vectorized-accumulation.md) gave `R2` and `ExplainedVariance` a `Vector<double>` accumulation gated on `outputCount == 1` and `Vector.IsHardwareAccelerated`. **`Outputs.WeightedMean` — the walk `mse`, `mae` and `RootMeanSquaredError` take — kept a scalar loop.**

The published table had already said what that cost without naming it: **at a million rows `r2` did two passes over the data for less than `mse` spent on one.** That is the signature of a missing vectorization, and a runner with AVX-512 made it loud because numpy's compiled loops widen with the hardware and a scalar loop does not.

## What shipped

The shared walk vectorized on the same gate its siblings already used.
