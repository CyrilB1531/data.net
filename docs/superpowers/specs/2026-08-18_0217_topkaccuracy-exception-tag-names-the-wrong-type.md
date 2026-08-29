# 0217 — TopKAccuracy.Score's exception tag names the wrong type

**Issue:** [#0217](https://github.com/CyrilB1531/lodestar/issues/0217) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

## Problem

The prose describing a member exists **twice** — an XML documentation comment in the source and a reference page under `docs/reference/` — and nothing confronted the two.

**This is what that costs.** `TopKAccuracy.Score` tagged `ArgumentException` where it throws `ArgumentOutOfRangeException`, **and a reader caught it rather than a gate.** It could as easily have fallen the other way, leaving the published page naming a type a reader would then fail to catch.

## What was decided

Fix the tag, and — because one instance is not the point — make the confrontation a gate. The reference gate parses `<exception cref>` from the assembly and holds it to the page's **Exceptions** rubric as a set. That became [#258](https://github.com/CyrilB1531/lodestar/issues/258) and [ADR 0038](../../decisions/0038-the-gate-confronts-an-exception-tag-with-the-page-that-documents-it.md).

## What shipped

The corrected tag, and the check that means the next one fails a build rather than a caller's `catch`.
