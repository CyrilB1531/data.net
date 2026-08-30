# 0382 — Lift the Latin-1 restriction on the blocked bit-parallel path

**Issue:** [#0382](https://github.com/CyrilB1531/lodestar/issues/0382) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-21

## Problem

[#302](https://github.com/CyrilB1531/lodestar/issues/302) lifted the restriction on the single-word path. The blocked path — the one long inputs take — still refused a pattern holding a character above U+00FF.

## What it changed about the gates

**One constant per kernel now governs two regimes**, and [#383](https://github.com/CyrilB1531/lodestar/issues/383) measured the two kernels no longer crossing in the same place. That is what [ADR 0048](../../decisions/0048-the-gate-depends-on-the-kernel-and-the-alphabet.md) records, and what [#407](https://github.com/CyrilB1531/lodestar/issues/407) then had to sweep.

## What shipped

The side table on the blocked route, and with it the last place a kernel refused an alphabet.
