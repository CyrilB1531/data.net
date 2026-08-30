# 0256 — The live wiki banner names a page the archive never held

**Issue:** [#0256](https://github.com/CyrilB1531/lodestar/issues/0256) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

## Problem

The banner on every live wiki page names a page in the archive, and its target was computed from `main`. `_declared_landing` returns the first non-glob page the map declares, which for `Lodestar.Metrics` is `docs/guides/metrics.md` — **added after the v0.2.0 tag was cut**, so the archive never held it.

## What was measured

The 0.2.0 archive rebuilt both ways over the same **227 banner targets**: before, **112 pages pointed at `Metrics-0.2.0-metrics`, which does not exist**. After, zero.

## What was decided

Resolve the banner's target against **the archive it points into**, not against `main`. The banner now names `Metrics-0.2.0-accuracy` — which the sidebar already resolved to, through `_resolve_landing`. The fix is using the function that was already right rather than writing a new one.

## What shipped

The resolution change, and 108 pages that stopped 404-ing.
