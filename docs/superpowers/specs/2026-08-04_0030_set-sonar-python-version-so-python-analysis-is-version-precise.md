# Design — #30: tell SonarQube Cloud which Python version the code targets

**Date:** 2026-08-04 · **Issue:** #30 · **Branch:** `chore/30-sonar-python-version` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

The last analysis warns:

> Your code is analyzed as compatible with all Python 3 versions by default. You
> can get a more precise analysis by setting the exact Python version in your
> configuration via the parameter `sonar.python.version`

**That default is not neutral.** With no version set, the analyser must assume
every Python 3 release, so it suppresses any rule whose validity depends on the
version — hiding real findings — and can raise ones that do not apply.

A warning that reduces analysis quality is not a cosmetic warning.

## Decisions

### D1 — Declare 3.12, because that is what actually runs

`tools/generate_oracles.py` runs on **3.12** in the `Oracles are reproducible`
job. The analysis should say what is true, not the newest version available.

```text
/d:sonar.python.version="3.12"
```

### D2 — The value tracks `python-version` in `ci.yml`

Two places now state the Python version. They must move together, and the
scanner argument carries a comment saying so.

This is the kind of duplication that is cheap to introduce and silent when it
drifts — the analysis would simply become imprecise again, with no warning,
because a version *is* set.

### D3 — Verify what can be verified now, and name what cannot

The workflow YAML parsing is checkable locally. **Whether the warning clears is
not** — it only shows on the next analysis. Say so rather than claiming the
outcome.

## Out of scope

- Changing the Python version anywhere.
- Any other scanner parameter.

## What "done" means

`/d:sonar.python.version="3.12"` in the scanner `begin` step with a comment tying
it to `ci.yml`; the workflow parsing; the warning confirmed cleared on the
dashboard after merge.
