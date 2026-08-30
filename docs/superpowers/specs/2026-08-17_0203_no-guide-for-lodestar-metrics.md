# 0203 — No guide for Lodestar.Metrics

**Issue:** [#0203](https://github.com/CyrilB1531/lodestar/issues/0203) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-17

## Problem

**42 members documented one by one, and nothing saying which to reach for.** The reference answers "what does this do"; nobody had answered "which of these do I want", which is the question a reader arrives with.

## What was decided

A guide, on the shape the other packages' guides already had — routing first, then the families, with the reference pages as the destination rather than the substance. A guide that restates 42 member pages is a fourth copy of the same prose; **its content is the choices between them.**

## What shipped

`docs/guides/metrics.md`, and — because the guide is the landing page a package's wiki channel resolves to — a dependency on `wiki-map.json` that [#256](https://github.com/CyrilB1531/lodestar/issues/256) later tripped over: the archive for a tag cut **before** this guide existed cannot resolve to it.
