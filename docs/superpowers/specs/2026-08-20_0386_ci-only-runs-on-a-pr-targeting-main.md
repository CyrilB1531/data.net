# 0386 — ci.yml only runs on a PR targeting main

**Issue:** [#0386](https://github.com/CyrilB1531/lodestar/issues/0386) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-20

## Problem

`ci.yml`'s `pull_request` trigger filtered on `main`, **so a pull request stacked on a feature branch skipped CI entirely** — and looked green, because a pull request with no required checks reports nothing failing.

## Why that is worse than a red build

A stacked pull request is exactly the case where a contributor most wants the checks: the base is unmerged work, so the combination has never been built anywhere. **The filter removed the checks precisely where they were most informative**, and did it silently.

## What shipped

The branch filter removed from the `pull_request` trigger. This session met the residue of the same class twice: **GitHub does not re-run a pull request's checks when its base moves**, so a fix pushed to a base leaves the stacked pull request red until its branch is updated.
