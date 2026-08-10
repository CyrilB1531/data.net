# Design — #84: fail on a Sonar finding before the push instead of after it

**Date:** 2026-08-06 · **Issue:** #84 · **Branch:** `chore/84-sonaranalyzer-in-build` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

The quality gate allows zero new violations, so any finding has to be fixed before
a pull request can merge — but nothing surfaces one until CI has run and
SonarCloud has reported. Every finding costs a round trip, and several recent
branches have spent one.

Worse, the class of failure #47 documented is invisible locally: a suppression
left behind by a code move reappears as a finding **with the build still green**,
because nothing in the build runs the analyzer.

## Decisions

### D1 — `SonarAnalyzer.CSharp` as an analyzer-only `PackageReference`

`PrivateAssets="all"`, in `src/`, `tests/` and `bench/`, so **the rules that gate
the pull request also gate `dotnet build`**. Combined with repository-wide
warnings-as-errors, a finding becomes a compile error on the machine that wrote
the code.

It reaches no published package; `tools/check_nuspec_dependencies.py` asserts
that.

### D2 — The version is pinned once, and read from three places

`$(DataNetSonarAnalyzerVersion)` in the root `Directory.Build.props`.

Three places, because the areas reference packages differently: `src/` and
`tests/` have Central Package Management and take it through their
`Directory.Packages.props`; **`bench/` has none** and names the version on the
reference itself.

Raising it usually surfaces new rules and therefore a cleanup, so it is treated as
its own change rather than a drive-by bump.

### D3 — Scope decided by measurement, not by argument

The issue asked for this to be decided deliberately, so it was measured first:

| Area | Findings on first compile under the rules |
| --- | ---: |
| `src/` | 7 |
| `tests/` | 4 |
| `bench/` | 0 |

**Four findings is not an arbitration**, and `bench/` was already clean. The root
props has said "warnings are errors everywhere: src, tests and bench alike" since
the beginning, and SonarCloud already reports on all three — so scoping the local
build *more narrowly than the remote gate* would recreate the round trip in
miniature, for the code that is read most often.

### D4 — `samples/` stays out, for three stated reasons

- outside `DataNet.slnx`;
- restores from a local feed, so a `pack` has to come first;
- `DocSnippets/Generated/` is already excluded from SonarCloud's analysis.

Recorded in ADR 0015 — including, honestly, that this leaves the samples analysed
only by CI. That reasoning is later revisited and partly reversed by #107, which
is what an ADR is for.

### D5 — The gate is proven by making it fail

A deliberate violation must fail the build, and pass once removed. A gate nobody
has seen fail is not known to work — the same standard #10 and #17 set.

## What this changes about the workflow

**Sonar findings are cleared before a commit, not after.** That belongs in
`CONTRIBUTING.md`, because it changes what "the build is green" means.

## Out of scope

- The .NET code-quality analysers (`CAxxxx`), which sit at SDK defaults and are a
  separate gap — later #107.
- `samples/`.

## What "done" means

The analyzer referenced in the three areas from one pin; the eleven first-pass
findings fixed or suppressed with reasons; the gate demonstrated failing;
ADR 0015 and `CONTRIBUTING.md` written.
