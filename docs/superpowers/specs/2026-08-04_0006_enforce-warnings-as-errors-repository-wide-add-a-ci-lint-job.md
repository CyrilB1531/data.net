# Design — #6: warnings-as-errors repository-wide, and a CI lint job

**Date:** 2026-08-04 · **Issue:** #6 · **Branch:** `chore/6-warnings-as-errors-and-lint` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

`TreatWarningsAsErrors` is declared on the three library projects. Everywhere
else — `tests/`, `bench/` — warnings accumulate silently, and nothing checks the
Markdown or the C# formatting at all.

The asymmetry is the defect. A test project that warns is a test project nobody
reads the output of, and a repository whose documentation is its main deliverable
(the migration guides) has no gate on that documentation.

## Decisions

### D1 — Move the property to the root, do not copy it

`TreatWarningsAsErrors=true` goes in the root `Directory.Build.props` and the
three per-project declarations are **removed**.

A copy would leave four places where the answer could differ, and the one that
matters — whether a *new* project inherits it — would still be no. A move gives
one answer for `src`, `tests` and `bench` alike, including projects that do not
exist yet.

### D2 — The lint job runs the two checks that have no gate today

- `dotnet format --verify-no-changes`
- markdownlint

Both as a `lint` job in `ci.yml`, so they fail the pull request rather than being
something to remember.

### D3 — `.markdownlint.json` turns off exactly two rules, each for a reason

- **MD013 (line length)** — off. The prose here is hard-wrapped by hand at a
  consistent width already; MD013 would be re-litigating a decision the tree has
  made consistently.
- **MD024 (duplicate headings)** — scoped to *siblings*. `CHANGELOG.md` and the
  ADRs legitimately repeat headings like "Context" across sections.

Nothing else is disabled. A lint configuration that grows a rule every time a
check is inconvenient stops being a gate.

### D4 — The gate must pass on the tree it is added to, in the same PR

A CI check that lands red is not a gate, it is a nuisance the next contributor
learns to ignore. Both checks are expected to fail against the current tree, and
fixing what they find is part of this change rather than a follow-up.

**What that means concretely, and it is a lot of noise:** roughly 150 markdown
findings, nearly all of them table-pipe spacing and underscore emphasis, plus a
handful of `dotnet format` whitespace violations. Mechanical fixes are applied
with `markdownlint-cli2 --fix`; anything the tool cannot fix is a decision and is
made by hand.

### D5 — Fix content errors found while fixing formatting, but notice them

The unlabelled code fence in the README holds the repository tree, and that tree
names `DataNet.sln` when the solution file is `DataNet.slnx`. Correct it.

The general point: a formatting sweep is the moment the documentation gets read
closely, which is the only time anyone would notice such a thing. Do not let the
mechanical framing stop you from fixing what you see — but keep it in the PR
description so it is reviewed rather than smuggled.

## Out of scope

- The SonarLint backlog (#7). Warnings-as-errors will surface Roslyn warnings, not
  Sonar findings; those are a separate concern with a separate branch.
- Any analyzer *addition*. This turns existing warnings into errors; it does not
  enable new rules.

## What "done" means

Build clean under the new setting on every project; `dotnet format
--verify-no-changes` clean; markdownlint 0 issues; the `lint` job green on its
first run.
