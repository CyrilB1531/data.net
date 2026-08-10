# Design — #70: anchor the Sonar exclusions to an explicit base directory

**Date:** 2026-08-06 · **Issue:** #70 · **Branch:** `chore/70-pin-sonar-project-base-dir` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

Issue #68 made this workflow depend on relative pattern resolution.
`sonar.exclusions` and `sonar.test.exclusions` are matched against whatever
directory the scanner decided was the base — and the scanner logs, on every run,
that its auto-detection of that directory **already changed once** between major
versions:

> Starting with Scanner for .NET v8 the way the `sonar.projectBaseDir` property is
> automatically detected has changed and this has an impact on the files that are
> analyzed and other properties that are resolved relative to it like
> `sonar.exclusions` and `sonar.test.exclusions`.

Left implicit, an upgrade that moves the base makes `tests/oracles/**` match
nothing. The exclusions then **fail open**: the binary fixtures are indexed again,
the `Invalid character encountered` warning returns, and 4.5 MB of generated JSON
corpora re-enter analysis.

**Nothing goes red**, because a `WARN` never fails a job. So the regression
surfaces as a warning in a log nobody is reading, on a day unrelated to the change
that caused it.

## Decisions

### D1 — Pin the base directory explicitly

```yaml
/d:sonar.projectBaseDir="$GITHUB_WORKSPACE"
```

With a comment explaining what it anchors, in the style of the two already in that
step.

### D2 — `$GITHUB_WORKSPACE`, not a literal path and not `.`

The runner guarantees it, and **it does not depend on the shell's working
directory** when the scanner starts. A literal path breaks on a self-hosted
runner; `.` reintroduces the very ambiguity being removed.

### D3 — Pin the value already in effect, read from a log

`sonar.projectBaseDir` determines the paths SonarCloud uses as **file keys**.
Changing it would close and reopen every issue as new, and disturb blame
attribution.

So the value is taken from the log of the run on #69 rather than reasoned about:

```text
Base dir: /home/runner/work/data.net/data.net
```

which is `$GITHUB_WORKSPACE`. **This is a no-op today and a guarantee
afterwards** — and that combination is the whole design. A "safety" change that
re-keys a project is not safe.

## Why this is worth a branch at all

The failure it prevents has three properties that make it expensive: it is
**silent** (a warning, not an error), **delayed** (surfacing at some future
scanner upgrade), and **misattributed** (landing on whichever pull request is open
that day). Those are exactly the failures worth spending a branch on before they
happen.

## Out of scope

- Any change to the exclusion patterns themselves (#68).
- Upgrading the scanner.

## What "done" means

`sonar.projectBaseDir` pinned to `$GITHUB_WORKSPACE`; the value confirmed
identical to what the scanner already resolved; the workflow parsing; **no issue
re-keyed**.
