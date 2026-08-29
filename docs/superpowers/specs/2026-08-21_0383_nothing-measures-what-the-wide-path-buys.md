# 0383 — Nothing measures what the wide bit-parallel path buys

**Issue:** [#0383](https://github.com/CyrilB1531/lodestar/issues/0383) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-21

## Problem

The corpus was ASCII, so no published number described what either side does above Latin-1 — and after [#302](https://github.com/CyrilB1531/lodestar/issues/302) and [#382](https://github.com/CyrilB1531/lodestar/issues/382) there was a whole regime nothing measured.

## The finding that reopened the gates

**The two kernels no longer cross the dynamic program in the same place** on the wide alphabet. One constant per kernel had been calibrated on Latin and now governed two regimes that behave differently — which is [#404](https://github.com/CyrilB1531/lodestar/issues/404), and eventually [ADR 0048](../../decisions/0048-the-gate-depends-on-the-kernel-and-the-alphabet.md) and [0049](../../decisions/0049-two-gates-per-kernel-tested-where-the-width-is-known.md).

## Why a benchmark could not settle it

**A gate benchmark cannot place a gate.** Below the gate the dispatch sends both rows to the dynamic program, so **the ratio reads 1 exactly where the crossing would be**. Placing it needs [#208](https://github.com/CyrilB1531/lodestar/issues/208)'s method instead — edit the constant, rebuild, read the committed corpus end to end at each value — which [#406](https://github.com/CyrilB1531/lodestar/issues/406) finally made reachable by giving the corpus a wide half.
