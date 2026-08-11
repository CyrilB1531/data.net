# Design — #36: keep major action updates out of the grouped Dependabot PR

**Date:** 2026-08-04 · **Issue:** #36 · **Branch:** `chore/36-dependabot-major-updates` ·
**Checkout:** `<repo>`

## Problem

`.github/dependabot.yml` groups every action update into a single pull request:

```yaml
groups:
  actions:
    patterns: ['*']
```

That is right for routine bumps — one weekly pull request instead of six. It is
wrong for major versions.

**#32 is the demonstration.** It arrived as six *major* jumps at once —
`checkout` v4→v7, `upload-artifact` v4→v7, `setup-python` v5→v7, `setup-dotnet`
v4→v6, `cache` v4→v6, `setup-java` v4→v5 — reviewable only as a block, with no way
to take four and hold two.

Majors are exactly the updates that deserve individual review, because they are
the ones that can change behaviour. Grouping them removes the ability to decide.

## Decisions

### D1 — Restrict the group to minor and patch

```yaml
groups:
  actions:
    patterns: ['*']
    update-types: [minor, patch]
```

Dependabot then opens majors individually. Minor and patch still group into one
weekly pull request, which is the ergonomics the group was introduced for.

### D2 — The real reason is that two workflows are never exercised by CI

This looks like a review-ergonomics preference. It is not.

`release.yml` and `release-nuget-org.yml` are **tag- and dispatch-triggered, so no
pull request ever exercises them**. A major bump to `checkout`, `setup-dotnet` or
`upload-artifact` in those files is unverified by CI and surfaces only at publish
time — the one moment where failure is most expensive and least recoverable.

Reviewing such a change on its own, rather than inside a six-action block, is the
difference between a considered decision and a rubber stamp.

### D3 — Verify the parsed configuration, not the text

Confirm `dependabot.yml` parses and the group resolves to `['minor', 'patch']`.
The schema is easy to get subtly wrong, and a mistyped key is ignored silently —
leaving the old behaviour with a file that looks fixed.

## Out of scope

- Accepting or rejecting #32 itself.
- Any action version change.

## What "done" means

Majors arrive as individual pull requests; minor and patch still group into one
weekly pull request; the parsed configuration confirmed.
