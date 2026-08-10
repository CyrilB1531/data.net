# Design — #8: a changelog, and cutting 0.2.0

**Date:** 2026-08-05 · **Issue:** #8 · **Branch:** `release/8-changelog-0.2.0` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

`v0.1.0` is on nuget.org with **no record of what it contains**, and 23 pull
requests have landed since. A consumer who upgrades has no way to know what
changed, and the maintainer has no way to answer "when did this behaviour change?"
without reading git log.

## Decisions

### D1 — Keep a Changelog format, SemVer discipline

`CHANGELOG.md`, with `0.1.0` **reconstructed from the git history** and `0.2.0`
written from the merged work. Reconstructing 0.1.0 is worth the effort: a
changelog that starts at the second release tells a reader the first one is
undocumented forever.

### D2 — 0.2.0 is a minor bump, and the reasoning is stated

Nothing public was removed or renamed. The four new stemmers and the
`netstandard2.0` target are **additive**; the performance work is
behaviour-preserving, and the oracle corpora are what prove it rather than a
claim.

### D3 — Two entries are written for someone debugging, not skimming

This is the substance of the issue. A changelog that lists features is a press
release; the two entries below are the ones that will save someone an afternoon.

- **The Regex match timeout is the release's one behavioural change.** Input that
  previously hung the calling thread now raises `RegexMatchTimeoutException`. That
  is the *point* of #25, but it is still a change, so it belongs under **Changed**
  — not filed away under *Fixed* where an upgrader will not look for it.
- **The Levenshtein figures carry their limit.** Long strings are 20–33× faster,
  but the bit-parallel path needs a Latin-1 pattern; CJK and emoji inputs still
  take the DP and those numbers do not describe them. A changelog quoting the
  speedup without the caveat would be accurate and misleading at once.

### D4 — Record what was never true, not only what changed

`coverlet.collector` was missing, so **coverage was never actually collected**.
That belongs in the changelog: a reader who trusted a coverage figure from 0.1.0
needs to know it did not exist.

### D5 — Fix the shipped notices while cutting the release

`LICENSE`, `NOTICE` and `THIRD-PARTY-NOTICES.md` go inside every package. Cutting
a version is the moment they are actually looked at, so the attribution is
corrected here — as its own commit, because it is a different concern from the
changelog.

### D6 — No tag is pushed from this branch

`v0.2.0` triggers the release workflow and publishes to GitHub Packages, and
**nuget.org publication is irreversible for a given version**. That is the
maintainer's call after this merges, not something bundled into a documentation
pull request.

The branch moves `Version` in `Directory.Build.props` and stops there.

## Out of scope

- The release itself.
- Per-package versioning (#64), which will later replace the single solution-wide
  `Version` this branch moves. Not yet — one concern per branch.

## What "done" means

`CHANGELOG.md` covering 0.1.0 and 0.2.0; `Version` at `0.2.0`; all three packages
packing at that version with both `lib/` folders; markdownlint clean; **no tag**.
