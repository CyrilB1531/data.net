# 0415 — Lodestar.Fuzzy's floor names a Lodestar.Text that predates its kernels

**Issue:** [#0415](https://github.com/CyrilB1531/lodestar/issues/0415) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-21

## Problem

`Lodestar.Fuzzy` reaches `Lodestar.Text` through a published floor pinned in `src/Directory.Packages.props`. **The floor still named a version predating the kernels Fuzzy actually runs on**, so a consumer resolving the minimum got a `Lodestar.Text` without them — and `fuzz.ratio` ran the old path with none of `Lodestar.Fuzzy 0.4.0`'s measured gains.

## Why the release revealed it

[#403](https://github.com/CyrilB1531/lodestar/issues/403) cut in two steps precisely because the floor must be *served* before Fuzzy ships. Going through that ordering is what made the stale floor visible: the thing being waited on was the thing that was wrong.

## The changelog detail that mattered

The entry first pointed at **#403, the release that revealed the gap**, rather than **#415, the issue that closes with the change**. Corrected: the commit link still names `8a1573c`, the commit that raised the floor. **An entry naming the wrong issue sends the next reader to the wrong conversation.**

## What shipped

The floor raised, and `tools/check_version_floor.py` — offline and instant — catching the three version numbers drifting apart from here.
