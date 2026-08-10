# Design — #19: publish analysis to SonarQube Cloud from CI

**Date:** 2026-08-04 · **Issue:** #19 · **Branch:** `ci/19-sonarcloud-analysis` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

Issue #7 cleared the SonarLint backlog locally, in an IDE, on one machine. Nothing
publishes analysis, so there is no shared view of quality, no coverage figure, and
no gate a pull request has to pass.

## Decisions

### D1 — The scanner wraps the build: `begin` → `build` → `end`

SonarQube Cloud analyses .NET by **installing the Roslyn analyzers and observing
the compilation**. The generic scan action does not compile anything, so it would
produce a green job and an empty analysis — the worst possible outcome, because it
looks like success.

### D2 — Three changes to the generated template, each for a reason

The template SonarQube Cloud generates does not work here:

- **Add `actions/setup-dotnet`.** The libraries target `net10.0`; the scanner
  analyses whatever the build compiles. Without the SDK the build fails — or
  silently analyses nothing.
- **`ubuntu-latest`, not `windows-latest`**, matching the rest of CI, with paths
  and shell adjusted.
- **Guard against fork pull requests.** They are not given `SONAR_TOKEN` and would
  *fail* rather than skip.

### D3 — Coverage was never collected, and publishing is what reveals it

The suite passes `--collect:"XPlat Code Coverage"`, but **no `coverlet.collector`
package was ever referenced**, so the collector is simply absent:

```text
Data collector 'XPlat Code Coverage' not found
```

The step has always been a silent no-op: it warns, and the job passes.

This is not cosmetic. The default quality gate requires coverage on new code, so
publishing with no coverage would fail **every** pull request from the first run.

The fix is to reference `coverlet.collector` in the three test projects and
collect in **OpenCover** format — which is what the .NET path of SonarQube Cloud
reads. The default Cobertura output would be ignored and shown as 0 %, which is
indistinguishable from having no tests.

### D4 — Verify locally what can be verified locally

Before the first CI run: the YAML parses and has the expected steps and triggers;
the exact `build` and `test` commands the workflow runs are executed by hand; and
**three `coverage.opencover.xml` reports exist**, one per test project, which is
what `sonar.cs.opencover.reportsPaths` globs.

A workflow whose first execution is also its first test is a workflow that fails
in a place where the feedback loop is minutes long.

### D5 — Three things can only be confirmed on the first real run, and are named

Written into the pull request rather than discovered later:

- **Whether the `#pragma warning disable S…` suppressions from #7 carry over.**
  They should — the scanner runs the same SonarAnalyzer rules through Roslyn — but
  it is worth checking rather than assuming, and the fallback is recorded.
- **Whether multi-targeting double-counts issues**, since every file now compiles
  twice (`net10.0` and `netstandard2.0`).
- **Whether to make the quality gate a required check.** Not yet: wait until a few
  runs show the baseline is clean, so it does not immediately block every pull
  request on pre-existing findings.

Naming an unknown is not hedging. Each of these changes what the next branch does.

## Out of scope

- Making the gate a required check (D5 defers it deliberately).
- Any source change beyond the three test project files.

## What "done" means

The workflow runs on pushes to `main` and on pull requests; analysis and coverage
appear in SonarQube Cloud; the three unknowns of D5 recorded in the pull request.
