# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

DataNet is a data-science toolkit for C#/.NET whose thesis is deliberately narrow:
don't rewrite Python's ecosystem, write native code only where .NET has a real gap
— text (distances, vectorization, tokenizers, embeddings) and scikit-learn-parity
metrics — with **no Python at runtime**. Everything else is delegated to existing
.NET libraries, and that delegation is documented in `docs/migration/`.

`CONTRIBUTING.md` is the authoritative process document. This file covers what a
session needs in order to be productive quickly, and the traps that cost time.

## Where a fact belongs

Each document below has one subject; content whose subject is another document's
belongs there instead, with a link left behind. The source column is what tells
you whether to correct the document itself or something upstream of it.

| document | its source | its subject |
| --- | --- | --- |
| `bench/README.md` | the `bench/` harness projects and scripts, hand-maintained | **how to measure** — the harness, the corpus, the commands |
| `docs/guides/performance.md` | a benchmark run on a named machine | **what was measured** — every number, with its machine and its window |
| `tools/README.md` | the scripts under `tools/`, hand-maintained | what each tool does and how to run it |
| `CONTRIBUTING.md` | the project's own process, hand-maintained | the process a contributor follows |
| `CLAUDE.md` | what a session has found, hand-maintained | what a session needs to be productive, and the traps that cost time |
| `docs/equivalence.md` | the oracle corpora in `tests/oracles/*.json`, replayed against the C# they compare | the Python call to C# counterpart mapping, with each divergence |
| `docs/migration/` | the .NET package chosen for each need | what is delegated to another .NET library, and why |
| `CHANGELOG.md` | the merged pull requests, per release | what changed, per release |
| `docs/decisions/` | the ADRs' own `**Status:**` lines, indexed in `docs/decisions/README.md` | a decision, with its options and its loser |
| root `README.md` | the project as it stands, hand-maintained | what the project is, and where to go next |

## Commands

```bash
dotnet build DataNet.slnx -c Release      # both target frameworks; warnings are errors
dotnet test DataNet.slnx -c Release       # runs the suite twice: net10 and netstandard2.0 assemblies
dotnet format DataNet.slnx --verify-no-changes
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
```

Neither `python` nor `python3` is safe to assume on both platforms: Ubuntu 24.04 ships
`/usr/bin/python3` and no `python` by default, while python.org's Windows installer (and
`winget install Python.Python.3.12`, which wraps it) ships `python.exe` and no `python3.exe` —
a `python3` that does resolve there is often the Microsoft Store app-execution alias, which
opens the Store instead of running anything.

```bash
# POSIX (bash/zsh)
python3 tools/check_version_floor.py      # offline, instant; catches the three version numbers drifting apart
python3 tools/check_machine_paths.py      # catches a tracked file holding a path under someone's home directory
```

```powershell
# PowerShell
python tools/check_version_floor.py
python tools/check_machine_paths.py
```

A single test, or one area:

```bash
dotnet test DataNet.slnx -c Release --filter "FullyQualifiedName~SpanishSnowball"
dotnet test tests/DataNet.Text.Tests -c Release --filter "FullyQualifiedName~Levenshtein"
```

**Read the test count, not the colour.** A `--filter` that matches nothing exits
zero and reports success. This has produced false confidence here more than once.

Oracle corpora (see *Oracle validation* below), run from outside the repository:

```bash
# POSIX (bash/zsh)
cd /tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py
```

```powershell
# PowerShell
cd $env:TEMP
$env:PYTHONSAFEPATH = '1'
<repo>\.venv-oracles\Scripts\python.exe <repo>\tools\generate_oracles.py
Remove-Item Env:PYTHONSAFEPATH   # POSIX sets it only for this one command; PowerShell must clear it back out
```

Both need a neutral directory because `nltk` refuses to import under the repository (see
*Oracle validation* below) — `/tmp` on POSIX, `$env:TEMP` on PowerShell.

Guide snippets, benchmarks, packaging (see the `python`/`python3` split above):

```bash
# POSIX (bash/zsh)
python3 tools/extract_doc_snippets.py && dotnet build samples/DataNet.DocSnippets -c Release
```

```powershell
# PowerShell — split, not chained with `&&`, which needs PowerShell 7+
python tools/extract_doc_snippets.py
dotnet build samples/DataNet.DocSnippets -c Release
```

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*Levenshtein*'
for p in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy src/DataNet.Metrics; do
  dotnet pack "$p" -c Release -o ./artifacts
done
```

## Architecture

Four independently versioned packages under `src/`:

| Package | Holds |
| --- | --- |
| `DataNet.Text` | distances, phonetics, set similarity, stemmers, tokenizers, sparse vectorizers (`CsrMatrix`), persistence. No third-party dependency beyond polyfills. |
| `DataNet.Embeddings` | sub-word tokenizers (WordPiece, SentencePiece, BPE/byte-level BPE), batch encoding pipeline, pooling, SIMD kNN `EmbeddingIndex`, ONNX inference. ONNX Runtime is isolated here. |
| `DataNet.Fuzzy` | `fuzz.*`, `process.extract`, blocking deduplication. |
| `DataNet.Metrics` | classification metrics at scikit-learn parity. |

Four cross-cutting facts explain most of the layout, and none of them is visible
from a single file.

### 1. Two target frameworks, one public API

Everything ships `net10.0;netstandard2.0` in a single package. `netstandard2.0`
reaches equivalent behaviour through conditional compilation, **never a reduced
API**. Gaps are closed in a fixed order: PolySharp polyfills → `System.Memory` /
`System.Numerics.Vectors` / `System.Text.Json` referenced only on that target →
hand-written fallback. `src/Shared/` holds `Guard`, `StringCompat` and friends,
compiled into every library under `DataNet.Internal` with a global using, so no
call site carries an `#if`.

The `*.NetStandard.Tests` projects **link the same test sources** and pin
`SetTargetFramework=netstandard2.0` on the project reference. That is why
`dotnet test` runs everything twice: without it the assemblies shipped to .NET
Framework, Mono and Unity would be compile-verified but never executed. Any new
test file is picked up by both automatically.

The one deliberate behavioural split is `VectorMath.Dot` — `Vector<T>` SIMD on
net10, scalar loop on netstandard2.0.

### 2. Versions are per package, and `src/` references packages, not projects

Each publishable project declares its version in a sibling `src/<Package>/Version.props`
and nowhere else. `DataNet.Fuzzy` reaches `DataNet.Text` through a
`PackageReference` on a **published floor** pinned in `src/Directory.Packages.props`
— which is what makes `git clone && dotnet build` work with no pack step. A CI job
asserts through evaluated MSBuild that no `src/` project carries a
`ProjectReference`.

When a branch edits two packages together, the floor points at an older
`DataNet.Text` than your working tree:

```bash
# POSIX (bash/zsh)
export DataNetUseProjectRefs=true   # local developer loop only; CI never sets it
```

```powershell
# PowerShell
$env:DataNetUseProjectRefs = 'true'   # local developer loop only; CI never sets it
```

Unset it before measuring anything — with it on you are building a graph that will
never ship. A branch whose `DataNet.Fuzzy` needs new `DataNet.Text` API **cannot
go green**; release `DataNet.Text` first, raise the floor, then land the other
side. Release tags are `<PackageId>/v<Version>`.

### 3. Conformance is proven by frozen oracles, not by hand-written expectations

Every algorithm replays reference values captured from the canonical Python
library (rapidfuzz, jellyfish, textdistance, difflib, scikit-learn, nltk,
HuggingFace `tokenizers`, sentencepiece, numpy, ONNX Runtime) into
`tests/oracles/*.json`, compared at `1e-9` for floats and exactly for strings.
Python is a **development dependency only**.

Three traps, each of which has already cost a session:

- **Run the generator from a neutral working directory.** `nltk` refuses to import
  its dependencies when they appear to live under the current directory, so a run
  from the repository root fails with `ImportError: Blocked import of regex from
  current working directory` even with `PYTHONSAFEPATH` set.
- **Read the generator's own exit code**, never a pipeline's. `python … | tail`
  reports `tail`'s status, so a failed generation looks successful — and the drift
  check that follows then proves nothing, because nothing was regenerated.
- **The `Oracles are reproducible` job is occasionally flaky** — the same commit
  has gone red then green, because drift depended on which CPU the runner landed
  on. Re-run before believing it. On failure the job uploads the regenerated
  corpora as an artefact so the comparison can be made off the runner.

Where behaviour deliberately diverges from the Python reference, it goes in
`docs/decisions/` — nineteen ADRs so far, and they are the fastest way to
understand why something looks wrong. `docs/equivalence.md` maps each Python call
to its C# counterpart; **a row lands in the same commit as the function**, not
afterwards.

### 4. The analyzers gate the build, not the pull request

`SonarAnalyzer.CSharp` is referenced by every project under `src/`, `tests/`,
`bench/` and `samples/`, and the .NET code-quality rules run at
`AnalysisMode=All` with `AnalysisLevel` pinned to `10.0`. Warnings are errors
repository-wide, so **a Sonar or `CAxxxx` finding is a compile error on your
machine**. The analyzer version is pinned once as
`$(DataNetSonarAnalyzerVersion)` in the root `Directory.Build.props`; raising it
or `AnalysisLevel` surfaces new rules and is its own change.

- A rule an *area* trips by being that area (xunit's underscored names,
  BenchmarkDotNet's reflection-instantiated types, a sample printing to the
  console) → `NoWarn` in that area's `Directory.Build.props`, with a comment
  naming each rule.
- A rule one *call site* disagrees with → `#pragma warning disable` in the source,
  with the reason above it. Never either without a reason a reviewer can disagree
  with; "too noisy" is not one.
- **Do not reach for `.editorconfig` or `.vscode/settings.json`** for SonarLint
  rules. It ignores the first entirely, and `sonarlint.rules` is application-scope
  so VS Code silently drops it from a workspace file.
- **When suppressed code moves, the suppression stays behind.** Extracting a
  method into a new file leaves the `#pragma` in the old one and the rule
  reappears. This has happened twice here.

`dotnet build DataNet.slnx` does **not** reach `samples/` — they are outside the
solution. Duplication and coverage are visible only to SonarCloud, so a green
local build is not a green quality gate.

## Two gates that constrain how code is written

- **The packaging gate.** `samples/DataNet.Sample` consumes the packages from
  `./artifacts` through `samples/NuGet.config`, and every new public type must be
  reachable from it by a member reference. Adding public API means adding a use of
  it in `Lot*.cs`. Both sample builds need a fresh `pack` **and** an isolated
  `NUGET_PACKAGES`, or they judge the published packages instead of the working
  tree (ADR 0009).
- **The doc-snippets gate.** Every ` ```csharp ` fence in `README.md` and
  `docs/guides/` is extracted from the Markdown and compiled against the packed
  packages — there is no second copy, so a renamed method fails CI. A fence that
  genuinely cannot compile opts out with
  `<!-- docs-compile: skip - reason -->` above it.

## Provenance — two hard rules

- **Never transcribe GPL-licensed code.** The stemmers and phonetic encoders are
  original implementations written from the *published algorithm description*.
  Reading a reference implementation to diagnose one failing case is diagnosis and
  is fine; deriving the implementation from it is not. The oracle is what proves
  behaviour matches, so the source never needs to be copied. See ADR 0003.
- **Never commit model weights.** Test fixtures are small and synthetic; vocabularies
  are fetched against a pinned SHA-256 by `tools/fetch_*.py`.

## Workflow

GitHub flow, one concern per branch, `<type>/<issue>-<kebab-summary>`
(`feat/`, `fix/`, `perf/`, `docs/`, `chore/`). Reference the issue with
`Closes #n`. Everything written in English — code, comments, ADRs, commit
messages, PR bodies. Comments are held to four rules — say why not what, carry
what would check the claim, eight lines above a member, and a marker with its
reason past that. `CONTRIBUTING.md`'s *Claims in comments* is the statement;
`tools/check_comment_length.py` counts the lines and
`.github/instructions/comment_claims.instructions.md` carries what a review
asks about one. Commit messages carry no `feat:`/`fix:` prefix.

`main` is protected by four required checks with no bypass list; "require
approvals" is off because a single maintainer cannot approve their own PR. Do not
commit, merge or tag unless asked. A `perf/` PR carries before/after numbers and
names the machine.

**Clear Sonar findings before committing, not after.** A green build is not a
clean Sonar, and a finding introduced by a pull request blocks its merge.

Design specs and implementation plans live in `docs/superpowers/specs/` and
`docs/superpowers/plans/`, named `<date>_<issue id padded to 4>_<slug>.md`.

### SonarQube MCP server

`.github/instructions/sonarqube_mcp.instructions.md` applies to this repository:
disable automatic analysis with `toggle_automatic_analysis` when starting a task,
call `analyze_file_list` on the files you created or modified at the end, then
re-enable it. Look project keys up with `search_my_sonarqube_projects` rather than
guessing, and do not try to confirm a fix through `search_sonar_issues_in_projects`
— the server will not reflect the change yet.
