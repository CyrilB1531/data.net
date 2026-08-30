# 0263 — Show the two Silhouette overloads that take a distance matrix

**Issue:** [#0263](https://github.com/CyrilB1531/lodestar/issues/0263) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

## Problem

The second of the three sample gaps. `Silhouette` has overloads taking a precomputed distance matrix — the shape a caller arriving from `silhouette_score(metric='precomputed')` needs — and no sample reached them.

## Why it was invisible

The packaging gate matched **per type**. `Silhouette` was reached by another overload, so these two were never counted missing. See [#262](https://github.com/CyrilB1531/lodestar/issues/262) for the audit and [#265](https://github.com/CyrilB1531/lodestar/issues/265) for the fix to the gate itself.

## What shipped

Both overloads exercised in the metrics sample, and [ADR 0009](../../decisions/0009-packaging-gate.md) amended rather than annotated — the note above the original bullet says the granularity moved and points at the new section; the bullet is left as written, because the reasoning for the gate is unchanged.
