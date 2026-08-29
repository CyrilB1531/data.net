# 0238 — The reference link gate excludes docs by bare file name

**Issue:** [#0238](https://github.com/CyrilB1531/lodestar/issues/0238) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-17

## Problem

The gate that holds a page to linking the members it names skipped reference pages **by file name**. So any document merely *sharing* a name with a reference page left the gate silently: `docs/guides/vectorization.md` stopped being checked the moment `docs/reference/text/vectorization.md` existed, and an unlinked member survived it.

## What was decided

**Select on the path, not on the name.** A document is a reference page when its path starts with `reference/`, which is what the exclusion actually meant. A guide and an index are allowed to share a subject, and one of them is not a reference page for it.

## What shipped

The path-based test, and the guide it had been silently skipping brought back under the gate.
