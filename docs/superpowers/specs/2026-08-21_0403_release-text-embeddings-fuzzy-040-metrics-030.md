# 0403 — Release: Text, Embeddings and Fuzzy 0.4.0, Metrics 0.3.0

**Issue:** [#0403](https://github.com/CyrilB1531/lodestar/issues/0403) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-21

## Problem

Four independently versioned packages, released in one day, with an ordering constraint between two of them.

## The constraint that shapes the release

`Lodestar.Fuzzy` reaches `Lodestar.Text` through a **`PackageReference` on a published floor** pinned in `src/Directory.Packages.props`, and a CI job asserts through evaluated MSBuild that no `src/` project carries a `ProjectReference`. So **Fuzzy cannot ship until the `Lodestar.Text` its floor names is served by nuget.org.**

## What shipped

Cut in two steps on the same day: `Lodestar.Text`, `Lodestar.Embeddings` and `Lodestar.Metrics` first; `Lodestar.Fuzzy` once `Lodestar.Text 0.4.0` was live — and moving that floor is the whole of what Fuzzy published, its source being untouched since `0.3.1`.

## What the release revealed

That the floor had been left naming a `Lodestar.Text` predating the kernels Fuzzy runs on — [#415](https://github.com/CyrilB1531/lodestar/issues/415).
