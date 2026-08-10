# Design — #34: the Sonar job fails on every Dependabot pull request

**Date:** 2026-08-04 · **Issue:** #34 · **Branch:** `fix/34-skip-sonar-for-dependabot` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

`Build and analyze` fails on every Dependabot pull request — first seen on #32:

```text
SONAR_TOKEN:
The format of the analysis property sonar.token= is invalid
##[error]Process completed with exit code 1.
```

The token is empty. **Dependabot-triggered runs do not receive repository
secrets**; they read from a separate Dependabot secrets store.

The guard added with the workflow in #19 only covers forks:

```yaml
if: github.event_name == 'push' || github.event.pull_request.head.repo.full_name == github.repository
```

Dependabot branches live in **this** repository, so the condition passes, the job
runs, and the scanner fails on an empty token.

## Why this is worse than a red tick

The combination is self-defeating. #24 added Dependabot **precisely so the action
SHA pins stay maintained** — pins that are never updated freeze known-vulnerable
versions. Every one of those pull requests now arrives with a failing check, and
once the Sonar gate becomes required (#12) they are unmergeable.

The automation added to keep the pins safe would be blocked by the analysis added
two pull requests earlier.

## Decisions

### D1 — Skip analysis for Dependabot

```yaml
if: >-
  github.event_name == 'push' ||
  (github.event.pull_request.head.repo.full_name == github.repository &&
   github.actor != 'dependabot[bot]')
```

A dependency bump changes no source, so there is nothing for the analysis to say.

### D2 — Not the alternative, and the reason is recorded

Adding `SONAR_TOKEN` to the repository's **Dependabot secrets** would let analysis
run. That is only worth doing if analysing dependency bumps is genuinely wanted;
skipping is the lighter option and the common practice.

Recording the rejected option matters here because the fix looks like avoidance.
It is a choice, and someone should be able to reverse it knowingly.

### D3 — A skipped check must still satisfy a required check

GitHub counts a skipped check as satisfied, so these pull requests are not left
pending. This is the property that makes D1 safe once #12 lands, and it is worth
stating — it is also why the required check must be **this repository's own job**
rather than SonarQube Cloud's `SonarCloud Code Analysis`, which is never posted at
all on such a pull request.

### D4 — The guard's existing fork condition stays

Fork pull requests have the same problem for a different reason. Replacing the
condition instead of extending it would reopen that hole.

## Out of scope

- Making the Sonar gate a required check (#12).
- Any change to what is analysed on ordinary pull requests.

## What "done" means

Dependabot pull requests no longer fail the Sonar job; pushes and ordinary pull
requests still analyse; the fork guard intact.
