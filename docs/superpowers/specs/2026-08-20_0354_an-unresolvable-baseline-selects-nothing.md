# 0354 — An unresolvable baseline makes select_benchmarks select nothing, silently

**Issue:** [#0354](https://github.com/CyrilB1531/lodestar/issues/0354) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-20

## Problem

**An absent baseline was already read as ignorance** — `main()`'s own comment says so — and measures everything. **An unresolvable one took the opposite branch silently**: `changed_files()` ran `git diff` on it, caught the `CalledProcessError`, and returned an empty list, **which `select()` reads exactly like a real "no changes" range.**

Same not-knowing, opposite conclusion, and no message either way.

## What was decided

`git rev-parse --verify <since>^{commit}` tells the two apart **before** the diff runs. An unresolvable baseline now takes the same branch an absent one does — measure everything — and says so.

## The shape worth carrying forward

**A failure that looks identical to a success is the dangerous kind.** An empty selection and a correct empty selection are the same value; only the reason differs, and the reason has to be established before the value is produced, not inferred from it.
