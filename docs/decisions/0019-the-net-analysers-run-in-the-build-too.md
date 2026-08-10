# 0019 — The .NET code-quality analysers run in the build too, and `samples/` is analysed at all

**Status:** accepted · **Date:** 2026-08-10

## Context

[0015](0015-sonar-rules-in-the-build.md) put the Sonar rules in the build so a
finding would be a compile error rather than a comment on a pull request. It left
two ways for a diagnostic to reach SonarCloud's quality gate — which allows zero
new violations — without ever reaching `dotnet build`.

**1. `samples/` was analysed by nothing local.** `src/`, `tests/` and `bench/`
each reference `SonarAnalyzer.CSharp`; `samples/` had no `Directory.Build.props`
at all, so no analyser read that tree. This is not a quiet corner:
[0009](0009-sample-consumes-a-local-feed.md)'s packaging gate requires every newly
exported type to be reachable from `samples/DataNet.Sample/Lot*.cs`, so **every
feature lands code there** — in the one area no analyser opened locally. The
scanner reads the samples in CI (#77), so the round trip 0015 set out to remove
was not merely possible there: it was the only route a finding had.

**2. The .NET code-quality analysers sat at the SDK default.** No
`EnableNETAnalyzers`, `AnalysisLevel` or `AnalysisMode` was set anywhere. CA1845
and CA1859 are `note` severity by default, which means they never appear in build
output — while the compiler still writes them to the SARIF error log the Sonar
scanner ingests, and the gate counts them as `external_roslyn` issues against a
threshold of zero.

Pull request #106 spent a CI round discovering both.

## Measurements taken before deciding

Every number in this section was measured at commit `09f0ad3`, this branch's
point of departure, and read out of the SARIF error log of an actual compilation
(`-p:ErrorLog=…,version=2`) — which is what the scanner ingests, rather than what
the console prints. Results carrying a `suppressions` entry are excluded, because
an in-source `#pragma` is honoured by SonarCloud too. The `samples` column, and
the `.g.cs` probe below, were taken with the analyser reference of the decision
below already in place, since without it there is nothing there to measure. A
count without the commit it was taken at is not reproducible; re-measuring at a
later commit will give different numbers, and should.

**Baseline.** At the SDK defaults the whole solution produces **zero unsuppressed
diagnostics**. The `note`-severity CA findings that do exist — CA1822 ×10,
CA1845 ×9, CA2249 ×4, CA2208 ×1 — all already carry pragmas from earlier work.

**What each `AnalysisMode` costs:**

| Mode | CA rules raised to `warning` | Findings in `src`+`tests`+`bench` | Findings in `samples` |
| --- | --- | --- | --- |
| `Default` (before this change) | none; every rule keeps its built-in severity | 0 | 0 (no analyser) |
| `Minimum` | 92 | 0 | 0 |
| `Recommended` | 145 | 524 (517 of them CA1707) | 5 |
| `All` | 280 | 636 | 19 |

Two readings of that table shape everything below.

**The gap #106 hit is closed by any mode above `Default`.** `AnalysisMode=Minimum`
already contains CA1845 and CA1859 — the two rules that cost #106 its CI round —
and its `.globalconfig` contains only `= warning` entries, so it disables nothing.
`All` is a superset of it. The 655 findings `All` raises across the four areas are
therefore **not** a backlog of gate failures that had been accumulating: at
`Minimum`, which already closes the gap, this repository was at zero. `All` is a
**deliberate strictness upgrade taken on top of** the fix, and its cost is stated
here so that a future reader can decide it was not worth it.

**None of the 16 rules behind those 655 findings is enabled at the SDK default.**
SonarCloud therefore reports none of them today, which is why the area-wide
`NoWarn` entries below reopen no gap: they switch off rules the remote gate does
not count. That is a property of *these* 16 rules, not of `NoWarn` in general —
**it has to be re-checked whenever one of the lists grows**. A `NoWarn` for a rule
that *is* enabled by default strips the diagnostic from the SARIF error log too,
so SonarCloud stops counting it as well: the hazard is not a round trip, it is a
silent lowering of the bar. The rule stops being enforced anywhere, locally or
remotely, and nothing anywhere reports that it stopped.

**`Generated/` needs no exclusion mechanism.** 0015 kept the analyser out of
`samples/` partly because `samples/DataNet.DocSnippets/Generated/` is extracted
verbatim from the guides, and analysing prose written for a reader would raise
findings whose fix belongs in a Markdown file. Measured, with
`-p:TreatWarningsAsErrors=false` so that every diagnostic prints rather than the
build stopping at the first: a probe class carrying commented-out code, an
underscored method name and an empty body, appended to
`Generated/Quickstart.g.cs`, produces **0 warnings of any kind**; the byte-identical
probe in the hand-written `SnippetContext.cs` beside it produces **`warning S125`
(commented-out code) and `warning S1186` (empty method body)**. Roslyn's
generated-code detection keys on the `.g.cs` suffix, and both SonarAnalyzer and
the CA analysers honour it — the second half is the control that makes the first
half mean "suppressed", rather than "nothing ran". The local build reproduces
SonarCloud's `sonar.exclusions` for free, with no configuration. That reason for
keeping `samples/` out is measured false, and this ADR amends 0015 accordingly.

## Decision

### The settings live once, in the repository-root `Directory.Build.props`

Beside the existing `$(DataNetSonarAnalyzerVersion)` pin, and inherited by all
four areas:

- `EnableNETAnalyzers=true` — not decorative. The SDK enables these analysers
  only for `net5.0` and later, so without it the `netstandard2.0` leg of every
  multi-targeted project goes unanalysed, which is half of `src/`.
- `AnalysisLevel=10.0` — **pinned, not `latest`**. This is 0015's own argument
  about the SonarAnalyzer pin, applied to the SDK: adding rules to a build where
  warnings are errors breaks that build, so the bump must be an edit somebody
  makes on purpose, with the cleanup it implies, rather than a side effect of CI
  resolving a newer `10.0.x` SDK on a morning nobody chose.
- `AnalysisMode=All`.

### `samples/` gets its own `Directory.Build.props`

It imports the repository root explicitly — MSBuild stops at the *nearest*
`Directory.Build.props`, so without that import the samples would silently lose
`TreatWarningsAsErrors` and the package identity properties — and adds
`SonarAnalyzer.CSharp` with `PrivateAssets="all"`, naming
`$(DataNetSonarAnalyzerVersion)` on the `PackageReference` the way `bench/` does,
because `samples/` has no Central Package Management either. Restore is
unaffected: `samples/NuGet.config` maps only `DataNet.*` to the local feed and
everything else to nuget.org. The single `$(DataNetSonarAnalyzerVersion)` pin is
therefore read from **four** places now, not the three 0015 recorded: `src/` and
`tests/` through Central Package Management, and `bench/` and `samples/` on the
`PackageReference` itself.

### The 655 findings: what was fixed, and what was switched off

Seven rules are switched off area-wide, absorbing **609** of the 655. Each is
written as a `NoWarn` in that area's `Directory.Build.props`, with a comment
naming every rule in the list and why it does not apply there — the idiom
`samples/DataNet.DocSnippets` already used for `CS0219`.

| Rule | n | Off in | Because |
| --- | --- | --- | --- |
| CA1707 | 517 | `tests/`, `bench/` | `Method_Case_Expected` is the xunit and BenchmarkDotNet naming convention |
| CA1515 | 35 | `tests/`, `bench/` | both frameworks discover only public types |
| CA5394 | 18 | `tests/`, `bench/` | `Random` builds corpora here; there is nothing to keep secret |
| CA1062 | 17 | `tests/` | a test helper's arguments come from the suite, not from a caller; the four in `src/` were fixed |
| CA1303 | 16 | `bench/`, `samples/` | printing to the console is what a sample and a harness do |
| CA1849 | 8 | `tests/` | the suite calls the synchronous `Save` on purpose, to compare it against `SaveAsync`; that comparison is what it asserts — the two in `src/` carry a pragma instead |
| CA1812 | 4 | `bench/` | BenchmarkDotNet instantiates its types by reflection, so "never instantiated" is always wrong there |

The remaining **46 findings were each judged individually**: 15 are real fixes,
and 31 are `#pragma warning disable` in the source with the reason above them, per
`CONTRIBUTING.md` and 0015.

The 15 fixes: nine `CA1305` where a number was formatted in the current culture
(five in `samples/`, four in `tests/`), four `CA1062` in `src/` where a public
entry point genuinely did not guard its arguments — those threw
`NullReferenceException` before and throw `ArgumentNullException` now, with tests
— one `CA1307` in `tests/`, and one `CA2251` replacing
`string.CompareOrdinal(a, b) == 0` with `string.Equals(a, b, StringComparison.Ordinal)`.
The `samples/` CA1305 fixes changed printed output on a French machine, from
comma decimals to period decimals: that divergence between a contributor's console
and CI's *was* the defect.

The 31 pragmas are dominated by rules that are right in general and wrong about
this code in particular: `CA1308` on the Snowball, Porter and WordPiece
implementations, which are *defined* on lowercase input so `ToUpperInvariant`
would change their results; `CA1814` on the `double[,]`/`long[,]` that are the
dense-matrix shape; `CA1819` on `CsrMatrix`'s three arrays, which *are* the CSR
format and have been public API since 0.1.0; `CA1008` on `SentencePieceType`,
whose values mirror the protobuf's and start at 1 ([0013](0013-sentencepiece-parity-scope.md));
`CA1720` on `AnalyzerKind.Char`, which mirrors scikit-learn's `analyzer='char'`;
and `CA1001` on `BatchEmbeddingBenchmarks`, whose lifecycle BenchmarkDotNet owns.

### Why CA1307 is fixed in `tests/` and suppressed in `src/`

This asymmetry is deliberate and will otherwise read as an oversight. Of the nine
CA1307 sites, the one in `tests/` was fixed by passing `StringComparison.Ordinal`;
the eight in `src/` carry a pragma. The reason is the target frameworks:
`string.IndexOf(char, StringComparison)` and
`string.Replace(string, string?, StringComparison)` **do not exist on
`netstandard2.0`**, which every `src/` assembly multi-targets and no test project
does. Both calls are ordinal on every runtime that has the overload, so taking the
rule's advice in `src/` would change nothing except whether the code compiles.

## Consequences

- **A finding is a compile error in all four areas, demonstrated rather than
  asserted.** A deliberate violation was added to one file in each area and the
  build was run with `--no-incremental`, so that the compiler — and therefore the
  analysers — actually ran rather than a cached result being replayed:

  | Area | Probe | Result |
  | --- | --- | --- |
  | `src/` | `ToLowerInvariant` in `DataNet.Text/Distances/Hamming.cs` | `error CA1308`, once per target framework |
  | `tests/` | `IndexOf("x")` in `DataNet.Text.Tests/Distances/LevenshteinOracleTests.cs` | `error CA1866` and `error CA1310` |
  | `bench/` | `ToLowerInvariant` in `DataNet.Text.Benchmarks/VectorizerBenchmarks.cs` | `error CA1308` |
  | `samples/` | `IndexOf("x")` in `DataNet.Sample/Lot1Distances.cs` | `error CA1866` and `error CA1310` |

  None of the rules that fired is on that area's own `NoWarn` list, so the probes
  also show the lists did not over-reach. The `samples/` row is the one that did
  not exist before: it establishes that the samples build genuinely runs the
  analysers and fails on a real violation, rather than returning a green
  incremental result nothing read. That a probe in *this same file* was silent
  before `samples/` had an analyser and a build error after it did was measured
  separately, when the analyser reference first landed — on a different
  violation: commented-out code, `S125`, which produced no diagnostic at all
  beforehand and `error S125` afterwards.

- **The gap #106 hit is closed, and the strictness upgrade is separable.** If a
  future reader decides `All` costs more than it returns, dropping to `Minimum`
  keeps the fix and discards the upgrade; dropping to `Default` reopens #107.

- **`samples/` is gated in CI, not in a contributor's solution build.** This is
  the honest limit of the change. The samples consume the packages from a local
  feed, so building them requires a `pack` first, and they are deliberately
  outside `DataNet.slnx` ([0009](0009-sample-consumes-a-local-feed.md)) — so their
  analysis happens in the `Sample consumes the packages` and `Guide snippets
  compile` jobs, and in the `Build and analyze` job's own samples build, but
  **not** in `dotnet build DataNet.slnx`. A contributor who runs only the solution
  build still does not analyse `samples/`; they will hear about it from CI. That
  is strictly better than nothing reading the tree at all, and it is worse than
  the other three areas — both halves are true.

- **The standard is one that new code already meets.** Pull request #108 landed on
  `main` while this branch was in flight, bringing roughly 1400 lines of new
  parallel ROC-AUC code in `DataNet.Metrics` and 528 lines of new tests. Measured
  after rebasing onto it: **zero findings under `AnalysisMode=All`.** The 655 is a
  one-off inventory, not a running tax.

- **Raising `AnalysisLevel` is its own change, with its own cleanup.** A newer
  SDK's `10.0.x` band adds rules, and adding rules to a build where warnings are
  errors breaks it. Pinning turns that from something CI does to you into
  something you do on purpose — the same argument, and the same discipline, as
  the SonarAnalyzer pin in 0015.

- **An area-wide `NoWarn` is now a documented mechanism, and it is the narrower
  claim of the two.** `CONTRIBUTING.md` states the split: a rule an area trips *by
  being that area* goes in that area's `Directory.Build.props` with a comment
  naming each rule and why; a rule one call site disagrees with stays a
  `#pragma warning disable` in the source with its reason above it; neither is
  added without the reason. Both must be re-examined against the "enabled by
  default" check above whenever the lists grow.

- **The SonarCloud job is unchanged.** It still reads the whole project, including
  the Python under `tools/`, and still reports duplication and coverage, which no
  local analyser can. A green local build is still not a green quality gate.
