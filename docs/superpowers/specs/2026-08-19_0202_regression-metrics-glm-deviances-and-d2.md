# 0202 — Regression metrics, lot 2: the three GLM deviances and the three D² scores

**Issue:** [#0202](https://github.com/CyrilB1531/lodestar/issues/0202) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-19

## Problem

`Lodestar.Metrics` had the squared and absolute errors and nothing that priced a prediction under a distribution. The three GLM deviances and the three D² scores are one family with a power parameter, and a caller who guesses which power admits which inputs guesses wrong.

## What decided the design

**The regime table is the family's whole content**, so it is published rather than left to be inferred: for each Tweedie power, the distribution it corresponds to and what each of the two operands may be. One boundary was measured at both ends and called out — **a zero truth is legal from power 1 up to but not including 2**.

**The two D² scores disagree on a truth that never varies, and neither is wrong.** `D2Tweedie` raises; `D2AbsoluteError` answers 0. Reproducing one and hiding the other would have made a reader's map wrong, so each page names the other and the equivalence rows carry scikit-learn's exception on one side and this one's on the other.

## What shipped

Six types, fourteen reference pages, six `docs/equivalence.md` rows and the sample's lot 6. Three self-counts elsewhere moved with them — the regression index's own "reading it once saves reading it eleven times", and two in `Outputs.cs` counting the kernels sharing its walk.

## What it did not do

Nothing was left open. The lot closes the regression family for the members scikit-learn exposes.
