# 0214 — The oracle generator panics on every run and exits 0

**Issue:** [#0214](https://github.com/CyrilB1531/lodestar/issues/0214) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-18

## Problem

`tools/generate_oracles.py` printed a panic on every run **and exited 0** — which is exactly how a skipped corpus would look. Conformance in this project is proven by frozen oracles, so a generator that fails silently is a hole under the whole test suite.

## Why it was invisible

**A pipeline reports its last command's status.** `python … | tail` returns `tail`'s exit code, so the generator's own failure never reached the caller, and the drift check that follows then proved nothing — because nothing had been regenerated.

## What was decided

Fix the panic, **and make the trap explicit where a session will meet it**: `CLAUDE.md` now says to read the generator's own exit code and never a pipeline's, alongside the other two oracle traps (a neutral working directory for `nltk`, and the occasionally flaky reproducibility job).

## The shape worth carrying

**A failure that exits 0 is worse than one that crashes**, and this is the second instance of the same family in the tooling — [#354](https://github.com/CyrilB1531/lodestar/issues/354) is the other, where an unresolvable baseline read exactly like "nothing changed".
