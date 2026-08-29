# 0367 — bench-nightly publishes the live wiki even when dispatched off a feature branch

**Issue:** [#0367](https://github.com/CyrilB1531/lodestar/issues/0367) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-20

## Problem

The same class as [#349](https://github.com/CyrilB1531/lodestar/issues/349), on the other side of the workflow: a nightly dispatched against a feature branch published **that branch's** documentation tree to the live wiki.

## What was decided

**A dispatched run's ref decides what it may touch.** The live wiki is a side-channel that only `main` writes; everything else a nightly produces — its pages, its pull request — belongs to the ref it ran on.

## Why the pair matters more than either

Two steps in one workflow both assumed `main` because that is where the workflow usually runs. **The assumption was invisible until `workflow_dispatch` made it false**, and it was false in two places rather than one. Fixing only the reported step would have left the other.
