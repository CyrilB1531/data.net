# 0363 — Skip PR-triggered workflows on draft PRs

**Issue:** [#0363](https://github.com/CyrilB1531/lodestar/issues/0363) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-20

## Problem

Lint, Build and Sonar all ran on a draft pull request — CI minutes spent and notifications sent for work still in progress, before the author asked for review.

## The subtlety in the condition

`github.event.pull_request.draft == false`, **not** `!github.event.pull_request.draft`.

GitHub Actions coerces `null == false` to **true** — both sides cast to 0 for a boolean comparison — so the same condition is also correct, and unconditionally true, on `ci.yml`'s `push` trigger, where `github.event.pull_request` does not exist. The negation form would have silently disabled the push runs.

## What shipped

One condition on `pull_request` events. No other trigger or filter changed.
