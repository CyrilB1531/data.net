# 0390 — A branch's first nightly page never gets a PR

**Issue:** [#0390](https://github.com/CyrilB1531/lodestar/issues/0390) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-21

## Problem

A branch's **first-ever** nightly always reported *"neither page changed; nothing to open"*, with real content just written.

## Why

`git diff` with no `--cached` **says nothing about an untracked file**, and `docs/guides/branch/nightly_run.md` and `benchmark_latest.md` are not in a new branch's history yet.

**Measured on `perf/377-index-ceiling`** — runs 32412241406 and 32419005481: both logs show the pages written, both skip opening a pull request, and no `bench/nightly-perf-377-*` branch exists anywhere. The evidence is the *absence* of a branch, which is the kind a log alone would not have given.

## What shipped

`git add -- $pages` before the diff, **matching the pattern the wiki workflow's own "Push what changed" step already used.** The fix is adopting a shape that was already right elsewhere in the same repository — which is also why it was cheap to be confident in.
