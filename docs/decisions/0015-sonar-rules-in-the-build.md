# 0015 — The Sonar rules run in the build, not only after the push

**Status:** accepted · **Date:** 2026-08-06

## Context

`SonarAnalyzer.CSharp` was referenced nowhere in this repository: not in
`Directory.Build.props`, not in `src/Directory.Packages.props`, not in any
`.csproj`, not in a workflow.

The consequence was structural rather than a matter of discipline.
`TreatWarningsAsErrors` covers the analyzers a project actually references, and
SonarAnalyzer was not one of them, so `dotnet build` could not fail on a Sonar
rule however many warnings were errors. A code review reading a diff could not
see them either. The only two things that did were **SonarCloud, after the
push**, and **SonarLint in VS Code, for files that happened to be open**.

That second one is incidental coverage, and the gap it leaves is measurable: a
one-off local analyzer run over `src/DataNet.Metrics` reported 12 findings in
code committed several tasks earlier, in files nobody had opened. None of them
was wrong, and all of them would have come back from SonarCloud on the pull
request — which is the round trip this decision removes.

## Decision

`SonarAnalyzer.CSharp` is an **analyzer-only `PackageReference`** —
`PrivateAssets="all"`, so it reaches no published `.nuspec` — in **`src/`,
`tests/` and `bench/`**.

The version is pinned once, as `$(DataNetSonarAnalyzerVersion)` in the
repository-root `Directory.Build.props`, and read from three places because the
three areas reference packages differently: `src/` and `tests/` have Central
Package Management and take it through their `Directory.Packages.props`, while
`bench/` has none and names the version on the `PackageReference` itself. One
pin, three uses, nothing to drift.

## Why all three areas, and not `src/` alone

`src/` alone was the cautious option, on the theory that test and benchmark code
trips different rules and turning them all into errors at once may not be the
trade the project wants. Measured before deciding, it isn't a trade at all:

| Area | Findings on first compile under the rules |
| --- | --- |
| `src/` | 7 |
| `tests/` | 4 |
| `bench/` | 0 |

Four findings is not a cleanup, and `bench/` was already clean. Against that,
the root `Directory.Build.props` has said "warnings are errors everywhere: src,
tests and bench alike" since the beginning, and SonarCloud already reports on all
three — so scoping the local build more narrowly than the remote gate would
recreate the round trip in miniature, for the code that is read most often.

## Why `samples/` stays out

> **Amended by [0019](0019-the-net-analysers-run-in-the-build-too.md) (2026-08-10).**
> `samples/` now carries the analyser. The reason given below — that
> `Generated/` would light up prose — is measured false: Roslyn skips `.g.cs`
> files as generated code, and SonarAnalyzer honours that.

The samples are analysed by SonarCloud (#77) but do not get the analyzer here.
They consume the packages from a local feed, so building them requires a `pack`
first and they are deliberately outside `DataNet.slnx` (ADR 0009) — the
build a contributor runs does not include them. And
`samples/DataNet.DocSnippets/Generated/` is extracted verbatim from the guides
and already excluded from SonarCloud's analysis: adding the analyzer would light
up prose written for a reader, in a file whose fix belongs in a Markdown
document. The 11 findings above were found without them.

## Consequences

- **A finding is a compile error on the machine that wrote the code.** This is
  demonstrated rather than asserted: a commented-out line added to a file in each
  of `src/`, `tests/` and `bench/` fails that area's build with `error S125`.
- **The first compile under the rules produced 11 findings, and every one was a
  judgement.** Three `S125` were English prose whose semicolon the rule reads as
  a statement — reworded, not suppressed. One `S2306` was a local named `async`,
  renamed. The remaining seven are `#pragma warning disable` with a reason in the
  source, per `CONTRIBUTING.md`: five `S1244` where exact zero is exactly the
  value being guarded against and a tolerance would change the function, one
  `S6966` on the synchronous `Save` a test exists to compare against `SaveAsync`,
  and one `S1133` on an `[Obsolete]` whose removal is already scheduled.
- **The SonarCloud job is unchanged.** It still reads the whole project,
  including the samples and the Python under `tools/`, and reports duplication
  and coverage, which a local analyzer does not. Inside that job the scanner
  installs its own analyzers over the build it observes, so the reference here is
  inert there; the two are pinned independently, and a version skew shows up as
  a finding SonarCloud reports and the local build does not.
- **The analyzer reaches no package.** `PrivateAssets="all"` keeps it out of
  every dependency group, and `tools/check_nuspec_dependencies.py` is exactly the
  assertion that it stays out — its expected graph is exact, so an analyzer that
  ever leaked would fail the run rather than ship.
- **Raising the version is a deliberate act with a cost.** A new SonarAnalyzer
  release adds rules, and adding rules to a build where warnings are errors
  breaks it. The pin is in one place so that the bump is one edit and one
  cleanup, rather than three edits and a slow divergence between areas.
