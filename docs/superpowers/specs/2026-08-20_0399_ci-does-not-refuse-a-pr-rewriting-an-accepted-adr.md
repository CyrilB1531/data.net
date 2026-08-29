# 0399 — CI does not refuse a PR that rewrites an already-accepted ADR

**Issue:** [#0399](https://github.com/CyrilB1531/lodestar/issues/0399) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-20

## Problem

[#385](https://github.com/CyrilB1531/lodestar/issues/385) decided that an amendment is its own decision. **Nothing enforced it**, and three lots had already rewritten [ADR 0004](../../decisions/0004-bit-parallel-kernels.md) in place before anyone noticed.

## What was decided

`tools/check_adr_immutable.py`, diffing against the pull request's base. Its edges are the interesting part:

- **Only files that already existed at `--base` are covered** — a brand-new ADR in the same pull request is unrestricted, since it has no reader to mislead yet.
- **Even a `> **#NNN update:**` blockquote no longer passes.** That convention was itself superseded; allowing it would keep the practice alive under a different spelling.
- **`docs/decisions/README.md` is exempt**, being the index rather than a decision.
- **CI only, never the pre-commit hook** — [ADR 0046](../../decisions/0046-check-adr-immutable-runs-in-ci-only.md). Only a `pull_request` event has a base to diff against; a push to `main` is already merged, so there is nothing left to catch.

## What shipped

The guard and its ADR. This session relied on the first edge: ADR 0051 and 0052 were both edited freely in the pull requests that introduced them.
