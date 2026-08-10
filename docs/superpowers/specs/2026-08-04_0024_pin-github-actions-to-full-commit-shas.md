# Design — #24: pin GitHub Actions to full commit SHAs

**Date:** 2026-08-04 · **Issue:** #24 · **Branch:** `chore/24-pin-actions-to-sha` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

`githubactions:S7637` (Major). Actions are referenced by **mutable tags** —
`actions/checkout@v4`, `actions/setup-dotnet@v4`, `NuGet/login@v1`. A tag can be
repointed at any commit by whoever controls the action repository, so what runs in
CI can change with no commit here.

That matters most in the release workflows, which hold `id-token: write` and can
mint a nuget.org publishing key.

The repository is also **already inconsistent**: `sonarcloud.yml` pins
`setup-java`, `checkout` and `cache` this way. Doing it everywhere in one pass is
cheaper than deciding case by case.

## Decisions

### D1 — Pin every third-party action to a 40-character commit SHA, version in a trailing comment

All 16 references across the four workflows. The comment is what keeps the file
readable and what Dependabot refreshes.

### D2 — Pinning is not upgrading, and the two must not be mixed

The tags in use resolve to the v4/v5 series; the latest releases are v6 and v7.
Each reference is pinned to **what its tag resolves to today**, so this changes
nothing about what runs — only whether it can change underneath us.

Moving major versions is a separate decision with its own breaking-change risk.
Bundling it here would turn a security fix into a behavioural one, and neither
could then be reviewed on its own terms.

### D3 — Two traps that make this more than a search-and-replace

- **`NuGet/login@v1` is an annotated tag.** The ref points at a *tag object*, not
  a commit, so it must be dereferenced. Pinning the tag object's own SHA would
  fail silently — the workflow would simply not resolve, or worse, resolve to
  something unexpected.
- **`sonarcloud.yml` pins `actions/checkout` to a different SHA** than the tag
  resolves to today, so the repository already carries two pins for one action.
  Unify them: two answers to "which checkout do we run" is exactly the maintenance
  trap pinning exists to avoid.

### D4 — Pins are only safe if maintained, so add Dependabot in the same branch

A pin that is never updated freezes a known-vulnerable version. That is a real
regression against a mutable tag, which at least receives patches.

`.github/dependabot.yml` for the `github-actions` ecosystem: Dependabot
understands SHA pins and refreshes **both** the SHA and its version comment,
grouped into one weekly pull request rather than one per action.

Without this, the change trades one risk for another and calls it security.

### D5 — Verify by audit, not by eye

Every YAML file parses, and an audit for any `uses:` reference not matching a
40-character SHA returns nothing. Sixteen references across four files is exactly
the size where reading them all feels sufficient and is not.

## Out of scope

- Upgrading any action's major version (D2).
- Workflow injection (#21) and dependency installation (#22).

## What "done" means

All 16 references pinned with a version comment; both `checkout` pins unified;
`NuGet/login` dereferenced correctly; `.github/dependabot.yml` present; the audit
returning nothing.
