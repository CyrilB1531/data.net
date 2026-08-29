# 0262 — Weight a ranking metric in the sample, the way #223 made possible

**Issue:** [#0262](https://github.com/CyrilB1531/lodestar/issues/0262) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

## Problem

One of three sample gaps an audit found. They matter beyond the sample: `samples/` is the packaging gate, and a member no sample reaches is a member nothing proves survives packaging.

## What the three gaps revealed

**Type coverage was intact — all 14 new public types across nine merged PRs were reached — and every gap the audit found was member-level.** The gate matched per type, so **one referenced member made every other method, overload and property on that type invisible**. That blind spot is what let #262, #263 and #264 ship green.

That argues for taking the gate down a level rather than for patching three samples, which is [#265](https://github.com/CyrilB1531/lodestar/issues/265).

## What shipped

The weighted ranking call in the sample, and — with its siblings — the evidence that moved [ADR 0009](../../decisions/0009-packaging-gate.md)'s contract from types to members.
