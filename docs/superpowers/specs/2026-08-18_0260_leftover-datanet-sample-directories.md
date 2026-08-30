# 0260 — Leftover samples/DataNet.* after the Lodestar rename

**Issue:** [#0260](https://github.com/CyrilB1531/lodestar/issues/0260) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

## Problem

The DataNet → Lodestar rename left `samples/DataNet.Sample` and `samples/DataNet.DocSnippets` in working copies.

## What could and could not be fixed by a commit

**Neither carries a tracked file or a `.csproj`** — 209 MB of build output and five regenerable `*.g.cs`. **No commit can remove them**: that part is a deletion in each checkout that has them, and saying so is the honest half of closing this.

**What the repository can carry is the rule that would have kept them quiet.** `.gitignore` named `samples/Lodestar.DocSnippets/Generated/` — the one project that has generated sources *today* — so the same directory under the old name showed as untracked noise in every `git status`.

## What shipped

`.gitignore` ignores generated sources under **any** sample project rather than one by name. The class of fault, not the instance.
