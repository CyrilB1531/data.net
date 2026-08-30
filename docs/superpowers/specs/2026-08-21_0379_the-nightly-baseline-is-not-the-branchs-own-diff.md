# 0379 — The nightly baseline on a feature branch is not that branch's own diff

**Issue:** [#0379](https://github.com/CyrilB1531/lodestar/issues/0379) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-21

## Problem

A branch's **first-ever** nightly always concluded *"neither page changed; nothing to open"* — with real content just written.

## Why

`git diff` with no `--cached` **says nothing about an untracked file**, and `docs/guides/branch/nightly_run.md` and `benchmark_latest.md` are not yet in a new branch's history.

**Measured on `perf/377-index-ceiling`** (runs 32412241406 and 32419005481): both logs show the pages written, both still skip opening a pull request, and no `bench/nightly-perf-377-*` branch exists anywhere.

## What shipped

`git add -- $pages` before the diff, **matching the pattern the wiki workflow's own "Push what changed" step already used** — the fix is adopting the shape that was already right elsewhere in the same repository.
