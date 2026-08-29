# 0340 — The pandas migration promises a bridge that does not exist

**Issue:** [#0340](https://github.com/CyrilB1531/lodestar/issues/0340) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-20

## Problem

`docs/migration/pandas.md` told a reader arriving from pandas that a DataFrame → sparse-matrix bridge was **planned**, with no issue behind the claim and no way to tell whether that meant next week or never.

## What was decided

**There is nothing to build.** The vectorizers take `IEnumerable<string>` and `CsrMatrix` has exposed `Values`, `ColumnIndices` and `RowPointers` since 0.1.0, so the join is a LINQ expression in each direction.

`CLAUDE.md` reserves native code for a real gap in .NET, **and this is not one** — both sides already exist. A bridge would be a wrapper around two things a caller can already put together, carrying a maintenance cost and buying nothing.

## What shipped

The promise replaced by the expression it was promising. **Answering the question is the fix; a roadmap entry would have been the wrong repair.**
