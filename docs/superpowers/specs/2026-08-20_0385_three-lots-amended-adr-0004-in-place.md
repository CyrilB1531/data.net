# 0385 — Three lots amended ADR 0004 in place

**Issue:** [#0385](https://github.com/CyrilB1531/lodestar/issues/0385) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-20

## Problem

Three lots had edited [ADR 0004](../../decisions/0004-bit-parallel-kernels.md) in place as their work changed what it said. **An accepted decision that keeps being rewritten is not a record**: a reader cannot tell what was decided when, or what the decision looked like to the people who acted on it.

## What was decided

**An amendment is its own decision.** A later ADR carries `**Amends:** NNNN` on its status line and states what falls; the amended one is left exactly as written.

The older in-body `> **#NNN update:**` blockquote convention **has itself been superseded** by that rule — which matters, because several ADRs still carry such blocks from before it and a reader meeting one should know it is history rather than the current shape.

## What followed

[#399](https://github.com/CyrilB1531/lodestar/issues/399) is the guard, because a convention nothing enforces is a convention that decays. The one exception it allows is deliberate: **a brand-new ADR in the same pull request is unrestricted**, since it has no reader yet to mislead.
