# 0265 — Packaging gate: member granularity, not type granularity

**Issue:** [#0265](https://github.com/CyrilB1531/lodestar/issues/0265) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

## Problem

The gate matched **per type**, so one referenced member made every other method, overload and property on that type invisible. That blind spot is what let [#262](https://github.com/CyrilB1531/lodestar/issues/262), [#263](https://github.com/CyrilB1531/lodestar/issues/263) and [#264](https://github.com/CyrilB1531/lodestar/issues/264) ship green.

## The evidence that decided it

**Type coverage was in fact intact** — all 14 new public types across nine merged PRs were reached — **and every gap the audit found was member-level.** That argues for going down a level rather than for patching the three samples.

## The rules, and why each has an edge

- **An enum stays whole.** Its members leave a type reference and never a member one, so member granularity would flag every enum value forever.
- **A property counts as reached by either accessor**, since an object initializer emits the setter and a `Console` line the getter.

## What shipped

The gate at member granularity: **383 members, 383 referenced, 44 documented exclusions — up from 2**. The exclusion count rising is the honest part: members that genuinely cannot be reached from a sample now each carry a reason instead of being covered by a sibling. [ADR 0009](../../decisions/0009-packaging-gate.md) is amended rather than annotated.
