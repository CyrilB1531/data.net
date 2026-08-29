# 0404 — The bit-parallel gate is calibrated on Latin

**Issue:** [#0404](https://github.com/CyrilB1531/lodestar/issues/0404) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-21

## Problem

One constant per kernel decided when to leave the dynamic program. It had been calibrated on an ASCII corpus, and after [#302](https://github.com/CyrilB1531/lodestar/issues/302) and [#382](https://github.com/CyrilB1531/lodestar/issues/382) that constant governed **two regimes** — and [#383](https://github.com/CyrilB1531/lodestar/issues/383) measured **the two kernels no longer crossing together on wide input.**

## Why this could not be settled where it was found

Three things had to exist first, and each became its own issue:

- **A corpus with a wide half** — [#406](https://github.com/CyrilB1531/lodestar/issues/406). Every bucket was ASCII.
- **Buckets the gate can actually see** — [#409](https://github.com/CyrilB1531/lodestar/issues/409). A length-8 pair mutated at 10% trims to a median pattern of **0**.
- **A method that is not a gate benchmark** — [#407](https://github.com/CyrilB1531/lodestar/issues/407). Below the gate both rows go to the dynamic program, so the ratio reads 1 exactly where the crossing would be.

## Where it ended

[ADR 0047](../../decisions/0047-one-gate-per-kernel-not-one-per-alphabet.md), then [0048](../../decisions/0048-the-gate-depends-on-the-kernel-and-the-alphabet.md) amending it, then [0049](../../decisions/0049-two-gates-per-kernel-tested-where-the-width-is-known.md). **Three decisions on one question, each overturned by evidence the previous one lacked** — which is the record this thread leaves.
