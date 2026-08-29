# 0215 — Dcg.Score takes a logBase scikit-learn refuses

**Issue:** [#0215](https://github.com/CyrilB1531/lodestar/issues/0215) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-18

## Problem

`Dcg.Score` accepted a `logBase` of zero or a negative. **scikit-learn refuses both; ours returned a silent `NaN`** — a value that flows on through an aggregation and surfaces somewhere else entirely.

## Why silence is the defect rather than the divergence

The package's contract is parity with the reference. A divergence that **throws** is a difference a caller finds immediately; one that returns `NaN` is a difference they find in a report, days later, with nothing pointing back here.

## What shipped

`ArgumentOutOfRangeException` on a `logBase` the reference refuses, the frozen corpus carrying the refused values so the behaviour is replayed rather than asserted by hand, and the `<exception cref>` tag — which [#217](https://github.com/CyrilB1531/lodestar/issues/217) then found *another* member had got wrong, and turned into a gate.
