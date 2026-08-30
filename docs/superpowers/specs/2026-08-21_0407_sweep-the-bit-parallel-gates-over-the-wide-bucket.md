# 0407 — Sweep the bit-parallel gates over the wide bucket

**Issue:** [#0407](https://github.com/CyrilB1531/lodestar/issues/0407) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-21

## Problem

Place two gate constants against evidence rather than against the Latin calibration they inherited.

## Why a benchmark cannot do it

**A gate benchmark cannot place a gate.** Below the gate the dispatch sends both rows to the dynamic program, so **the ratio is 1 exactly where the crossing would be read.** The instrument is blind precisely where the answer is.

## The method used instead

[#208](https://github.com/CyrilB1531/lodestar/issues/208)'s: **edit the constant, rebuild, read the committed corpus end to end at each value.** Six values per kernel, **two passes in opposite order so drift between successive builds lands on both ends** rather than biasing one.

## What it concluded

**Leave them where they are.** The sweep is what makes that a result rather than an omission — and [#409](https://github.com/CyrilB1531/lodestar/issues/409) then found the corpus itself could not see below 8, which reopened it.
