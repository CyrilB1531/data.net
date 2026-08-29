# 0207 — The four guards run after the push

**Issue:** [#0207](https://github.com/CyrilB1531/lodestar/issues/0207) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

## Problem

The four offline guards were CI steps, so the first thing that told a contributor a machine path had reached a tracked file was a red job on a pull request. [#133](https://github.com/CyrilB1531/lodestar/issues/133) deferred the hook that would move them earlier and promised it "its own issue, with its own decision about installation and opt-out".

## The decisions, recorded as ADR 0037

- **A tracked `.githooks/pre-commit`, POSIX sh, installed with one command and no new dependency**: `git config core.hooksPath .githooks`.
- It resolves `python3` then `python`, because **neither name resolves on both supported platforms**.
- It reports **every** guard that failed rather than the first.
- It says that `--no-verify` skips it, rather than leaving that to be discovered.
- **A machine with no Python commits anyway.** The guards are a development dependency; refusing a commit for want of one would break work they would have passed.

## Two assumptions the issue made that were false

`check_nuspec_dependencies.py` cannot run before a commit at all — it reads the `.nuspec` files a pack produces, which do not exist yet. Both discoveries changed the scope rather than being worked around.

## What shipped

The hook, its ADR, and the installation line in `CONTRIBUTING.md`.
