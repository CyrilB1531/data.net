# Design — #107: close the gap between the local build's analysis and the PR gate

**Date:** 2026-08-10 · **Issue:** #107 · **Branch:** `chore/107-analysis-parity` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

SonarCloud's quality gate allows zero new violations, so any diagnostic the
scanner ingests has to be fixed before a pull request can merge. Two classes of
diagnostic reach the scanner without reaching `dotnet build`:

1. **`samples/` is not analysed at all.** `src/`, `tests/` and `bench/` each
   reference `SonarAnalyzer.CSharp`; `samples/` has no `Directory.Build.props`,
   so nothing analyses that tree. ADR 0009's packaging gate requires every new
   exported type to be reachable from `samples/DataNet.Sample/Lot3Embeddings.cs`,
   so new code lands there on every feature — in the one area no analyser reads.
2. **The .NET code-quality analysers sit at the SDK default.** No
   `EnableNETAnalyzers`, `AnalysisLevel` or `AnalysisMode` is set anywhere.
   `CA1845` and `CA1859` are therefore `note`-severity locally — invisible in
   build output — while the compiler still writes them to the SARIF error log the
   scanner reads, and SonarCloud counts them as `external_roslyn` issues.

Pull request #106 spent a CI round discovering both.

## Measurements taken before deciding

Every number below comes from the SARIF error log of an actual compilation
(`-p:ErrorLog=…,version=2`), which is exactly what the scanner ingests. Results
carrying a `suppressions` entry are excluded, because an in-source `#pragma` is
honoured by SonarCloud too.

**Baseline.** At SDK defaults the whole solution produces **zero unsuppressed
diagnostics**. The `note`-severity CA findings that do exist — CA1822 ×10,
CA1845 ×9, CA2249 ×4, CA2208 ×1 — all already carry pragmas from earlier work.

**Cost of each `AnalysisMode`:**

| Mode | CA rules raised to `warning` | Live findings, `src`+`tests`+`bench` | `samples` |
| --- | --- | --- | --- |
| `Default` (today) | none; each rule keeps its built-in severity | 0 | 0 (no analyser) |
| `Minimum` | 92 | 0 | 0 |
| `Recommended` | 145 | 524 (517 of them CA1707) | 5 |
| `All` | 280 | 636 | 19 |

**Two facts that shape the design:**

- `AnalysisMode=Minimum` already contains CA1845 and CA1859, and its
  `.globalconfig` contains only `= warning` entries — it disables nothing. `All`
  is a superset, so **the gap #106 hit is closed by any mode above `Default`.**
- **None of the 16 rules behind the 655 findings is enabled by default in the
  SDK**, so SonarCloud reports none of them today. The sweep is therefore a
  deliberate strictness upgrade, not the gap-closing part of this change, and the
  ADR must say so rather than let the finding count imply otherwise.

**`Generated/` needs no exclusion mechanism.** SonarCloud excludes
`samples/DataNet.DocSnippets/Generated/**` from analysis, and ADR 0015 cited that
tree as a reason to keep the analyser out of `samples/`. Measured: a probe class
carrying commented-out code and an underscore name, appended to
`Generated/Quickstart.g.cs`, produces **0 warnings**; the identical probe in the
hand-written `SnippetContext.cs` produces **S125, S101 and S1186**. Roslyn's
generated-code detection keys on the `.g.cs` suffix and both SonarAnalyzer and the
CA analysers honour it. The local build matches SonarCloud's exclusion for free.

## Decisions

### D1 — Settings live once, in the root `Directory.Build.props`

Beside the existing `$(DataNetSonarAnalyzerVersion)` pin:

- `EnableNETAnalyzers=true` — required, not decorative: the SDK enables the .NET
  analysers only for `net5.0`+, so without it the `netstandard2.0` leg of every
  multi-targeted project goes unanalysed.
- `AnalysisLevel=10.0` — **pinned, not `latest`**. This is ADR 0015's own
  argument about the SonarAnalyzer pin: adding rules to a build where warnings are
  errors breaks it, so the bump must be a deliberate edit rather than a side
  effect of CI resolving a newer `10.0.x` SDK.
- `AnalysisMode=All`.

All four areas inherit them, `samples/` included once D2 lands.

### D2 — `samples/Directory.Build.props`

A new file that imports the repository root explicitly — MSBuild stops at the
nearest `Directory.Build.props`, so without the import the samples would *lose*
the root's `TreatWarningsAsErrors` and package identity — and adds
`SonarAnalyzer.CSharp` with `PrivateAssets="all"`, naming
`$(DataNetSonarAnalyzerVersion)` on the `PackageReference` the way `bench/` does,
because `samples/` has no Central Package Management either.

Restore is unaffected: `samples/NuGet.config` maps only `DataNet.*` to the local
feed and everything else to nuget.org. Verified by building both sample projects
with the analyser injected.

**Known limitation, to be stated in the ADR:** `samples/` builds only after a
`pack`, so this gate lives in the `Sample consumes the packages` and doc-snippets
CI jobs, not in `dotnet build DataNet.slnx`. A contributor's plain solution build
still does not analyse `samples/`. That is strictly better than today, where
nothing does.

### D3 — How the 655 findings are treated

16 rules. Area-wide `NoWarn` for the ones whose finding is an artefact of what
that area *is*; an in-source `#pragma` with a reason for a site-specific
judgement, per `CONTRIBUTING.md` and ADR 0015; a real fix where the rule is right.

`NoWarn` is written in the area's `Directory.Build.props` with a comment giving
the reason, following the idiom `samples/DataNet.DocSnippets` already uses for
`CS0219`.

| Rule | n | Treatment | Reason |
| --- | --- | --- | --- |
| CA1707 | 517 | `NoWarn` in `tests/` + `bench/` | `Method_Case_Expected` is the xunit and BenchmarkDotNet naming convention |
| CA1515 | 35 | `NoWarn` in `tests/` + `bench/` | xunit and BenchmarkDotNet require public types |
| CA5394 | 18 | `NoWarn` in `tests/` + `bench/` | `Random` builds corpora, not secrets |
| CA1062 | 17 | `NoWarn` in `tests/`; fix the 4 in `src/` | a test helper does not validate its arguments; `src/` has `Guard` |
| CA1303 | 16 | `NoWarn` in `bench/` + `samples/` | printing to the console is what a sample and a harness do |
| CA1308 | 11 | `#pragma` in `src/` | Snowball, Porter and WordPiece are *defined* on lowercase; `ToUpperInvariant` changes results |
| CA1307 | 9 | fix | add `StringComparison.Ordinal`; `StringCompat.cs` covers the overloads netstandard2.0 lacks |
| CA1305 | 9 | fix | `CultureInfo.InvariantCulture`, in `samples/` too — a reader should see the correct form |
| CA1849 | 8 | `NoWarn` in `tests/`; `#pragma` for the 2 in `src/` | tests call the sync overload on purpose to compare it against the async one — the reason the S6966 pragma already there gives |
| CA1814 | 4 | `#pragma` in `src/` | `double[,]` / `long[,]` are the dense-matrix shape; jagged costs an allocation per row |
| CA1812 | 4 | `NoWarn` in `bench/` | BenchmarkDotNet instantiates by reflection |
| CA1819 | 3 | `#pragma` in `src/` | `CsrMatrix`'s three arrays *are* the CSR format, and are public API since 0.1.0 |
| CA1008 | 1 | `#pragma` in `src/` | `SentencePieceType` mirrors the protobuf's values, which start at 1 — ADR 0013 |
| CA1720 | 1 | `#pragma` in `src/` | `AnalyzerKind.Char` mirrors scikit-learn's `analyzer='char'` and is public API since 0.1.0; renaming it is a break for a naming rule |
| CA2251 | 1 | fix | `string.CompareOrdinal(a, b) == 0` → `string.Equals(a, b, StringComparison.Ordinal)`, `FeatureVocabularyJson.cs:184` |
| CA1001 | 1 | `#pragma` in `bench/` | `BatchEmbeddingBenchmarks` disposes `_embedder` from `[GlobalCleanup]`; BenchmarkDotNet owns the lifecycle, so `IDisposable` on the class is the wrong shape |

Seven area-wide `NoWarn` entries absorb **609** of the 655. **46 individual
judgements** remain — 23 of them real fixes (CA1307 ×9, CA1305 ×9, CA1062 ×4 in
`src/`, CA2251 ×1) and 23 pragmas with a reason.

Every `NoWarn` added here is safe against the gap this issue closes, because none
of these 16 rules is enabled by default and SonarCloud therefore reports none of
them. That has to be re-checked if the list grows.

### D4 — Documentation

- **ADR 0018**, `the-net-code-quality-analysers-run-in-the-build-too`. It records
  the measurements above, and **amends ADR 0015's "Why `samples/` stays out"**,
  which this change reverses: the reason that ADR gave — that `Generated/` would
  light up prose — is measured false, and the other reason, that a contributor's
  build does not include the samples, is true but is an argument for a narrower
  gate rather than for none.
- ADR 0015 gets a pointer to 0018 in that section, so a reader who lands there
  first is not misled.
- `CONTRIBUTING.md` gets the policy: area-wide `NoWarn` with a reason for what an
  area *is*, in-source `#pragma` with a reason for a site.
- Anything that counts or enumerates ADRs is re-read before the PR — counts and
  "see X" references go stale silently.

### D5 — Verification, demonstrated rather than asserted

ADR 0015 proved its gate by making it fail on purpose; this one does the same:

1. A deliberate CA violation added to a file in **each** of `src/`, `tests/`,
   `bench/` and `samples/` fails that area's build, and passes once removed. The
   `samples/` case is the one that did not exist before, so it is the one that
   matters most.
2. `dotnet build DataNet.slnx --configuration Release` is green with no
   `TreatWarningsAsErrors` override.
3. `dotnet test DataNet.slnx --configuration Release` is green.
4. Both sample builds are green against a local feed and an isolated
   `NUGET_PACKAGES`, as CI does — otherwise they judge the published packages.
5. SonarCloud is read on the pushed branch before the PR is called done. A green
   build is not a clean Sonar.

## Out of scope

- Raising `AnalysisLevel` past `10.0`, or any SonarAnalyzer version bump.
- The `samples/` gate reaching `dotnet build DataNet.slnx` — it cannot without
  putting the samples in the solution, which ADR 0009 forbids.
- The other rules SonarCloud counts but no analyser here runs: duplication and
  coverage, which need the server.
