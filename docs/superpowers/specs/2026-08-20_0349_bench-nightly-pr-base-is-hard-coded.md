# 0349 — bench-nightly's PR base is hard-coded to main

**Issue:** [#0349](https://github.com/CyrilB1531/lodestar/issues/0349) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-20

## Problem

`workflow_dispatch` can target a feature branch, and the **"Publish the page"** and **"Push what changed"** steps ran unconditionally — **overwriting the live wiki with that branch's whole `docs/` tree**, not just its own `nightly_run.md`.

## What was decided

**Only the live-wiki side-channel is scoped to `main`.** The pull request still opens against whatever ref was dispatched, because that is the point of dispatching against a branch. The two are different questions and had been answered by one unconditional step.

## What shipped

The condition on the publish steps, and with [#367](https://github.com/CyrilB1531/lodestar/issues/367) the wider fix to what a dispatched nightly is allowed to touch.
