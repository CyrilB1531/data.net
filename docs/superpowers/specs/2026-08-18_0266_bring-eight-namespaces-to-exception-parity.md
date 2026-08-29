# 0266 — Bring the eight unchecked namespaces to exception parity

**Issue:** [#0266](https://github.com/CyrilB1531/lodestar/issues/0266) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

## Problem

`exceptionsUnchecked` in `docs/wiki-map.json` names the namespaces whose `<exception cref>` tags are not yet held to their reference pages. The list only ever shrinks, and eight namespaces were still on it.

## What the reconciliation found

**The pages were right in all 40 Text cases**, which is what the issue predicted — **but each was read against the `throw` rather than copied across**:

- the five set-similarity scorers all route through `QgramCounts.Compute`, measured throwing `ArgumentOutOfRangeException` at `qval` 0 and −5;
- the seven stemmers all throw `ArgumentNullException` on a null word, **each called rather than assumed** — they are static classes, which the first probe got wrong by trying to construct them;
- the 28 vectorization members were exercised one by one.

Being right is not the same as being checked, and the point of the lot is the second.

## What shipped

`Lodestar.Text`'s list emptied first, then the rest, with the gate holding them from there.
