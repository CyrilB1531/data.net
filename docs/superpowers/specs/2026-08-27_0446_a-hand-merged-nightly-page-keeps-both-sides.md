# 0446 — A hand-merged nightly page silently keeps both sides

**Issue:** [#0446](https://github.com/CyrilB1531/lodestar/issues/0446) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-27

## Problem

A nightly page ended up carrying **two `BlockedTableBenchmarks` sections** — the 25th's numbers and the 24th's under the same heading — which then failed the lint job on MD024.

## What replaying the merge showed, and what it corrected

The first diagnosis was that **git had silently combined hunks. That was wrong.** Replaying the merge behind #446 shows git already declines to auto-merge these pages — **but it declines hunk by hunk: 22 separate conflicts in one file**, one of which a hand resolution settled by keeping both sides.

**Both parents held one section; the merge commit held two.** So the fault is not that git merged, but that a 22-marker conflict in a generated file is a resolution nobody can perform reliably.

## What was decided

Mark the three generated pages **`-merge`** in `.gitattributes`. git then leaves the file **whole** on a conflict and reports it, instead of handing over a file with 22 markers in it. Measured: **22 → 0**.

## The correction worth keeping

A wrong story was nearly shipped twice — first "git combines hunks", then "the class was measured twice in one run" (it was a manual merge). **Replaying the merge is what settled both**, and neither would have been settled by reading the result.
