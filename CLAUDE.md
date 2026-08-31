# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Lodestar is a data-science toolkit for C#/.NET whose thesis is deliberately narrow.
Don't rewrite Python's ecosystem, write native code only where .NET has a real gap
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
| `docs/reference/` | the exported types and public methods of the namespaces `docs/wiki-map.json` declares covered, replayed against both target frameworks' assemblies | what each function is for, entry by entry — declaration, parameters, returns, example, remarks |
| `docs/wiki-map.json` | the packages and the pages that ship with each, hand-maintained | which page belongs to which package, and which namespaces the reference gate enforces |
| `CHANGELOG.md` | the merged pull requests, per release | what changed, per release |
| `docs/decisions/` | the ADRs' own `**Status:**` lines, indexed in [`docs/decisions/README.md`](docs/decisions/README.md) | a decision, with its options and its loser |
| root `README.md` | the project as it stands, hand-maintained | what the project is, and where to go next |
| `.claude/skills/` | [obra/superpowers](https://github.com/obra/superpowers), vendored at a pinned commit | how a spec and a plan are written; `.claude/skills/README.md` says what was taken and what was not |

## Commands

```bash
dotnet build Lodestar.slnx -c Release      # both target frameworks; warnings are errors
dotnet test Lodestar.slnx -c Release       # runs the suite twice: net10 and netstandard2.0 assemblies
dotnet format Lodestar.slnx --verify-no-changes
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
python3 tools/check_sample_culture.py     # catches a sample number printed in the contributor's culture
python3 tools/check_repeated_literals.py --base origin/main   # catches a literal this branch pushed past S1192
```

```powershell
# PowerShell
python tools/check_version_floor.py
python tools/check_machine_paths.py
python tools/check_sample_culture.py
python tools/check_repeated_literals.py --base origin/main
```

A single test, or one area:

```bash
dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~SpanishSnowball"
dotnet test tests/Lodestar.Text.Tests -c Release --filter "FullyQualifiedName~Levenshtein"
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
*Oracle validation* below). Neutral means **not an ancestor of the checkout**, which is what
makes `/tmp` the wrong answer in a hosted or sandboxed session: those put the worktree under
`/tmp` itself, and the import guard fires anyway. Check where the repository actually is before
picking — `/var/tmp` serves when `/tmp` cannot.

Guide snippets, benchmarks, packaging (see the `python`/`python3` split above):

```bash
# POSIX (bash/zsh)
python3 tools/extract_doc_snippets.py && dotnet build samples/Lodestar.DocSnippets -c Release
```

```powershell
# PowerShell — split, not chained with `&&`, which needs PowerShell 7+
python tools/extract_doc_snippets.py
dotnet build samples/Lodestar.DocSnippets -c Release
```

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- --filter '*Levenshtein*'
for p in src/Lodestar.Text src/Lodestar.Embeddings src/Lodestar.Fuzzy src/Lodestar.Metrics; do
  dotnet pack "$p" -c Release -o ./artifacts
done
```

## Architecture

Four independently versioned packages under `src/`:

| Package | Holds |
| --- | --- |
| `Lodestar.Text` | distances, phonetics, set similarity, stemmers, tokenizers, sparse vectorizers (`CsrMatrix`), persistence. No third-party dependency beyond polyfills. |
| `Lodestar.Embeddings` | sub-word tokenizers (WordPiece, SentencePiece, BPE/byte-level BPE), batch encoding pipeline, pooling, SIMD kNN `EmbeddingIndex`, ONNX inference. ONNX Runtime is isolated here. |
| `Lodestar.Fuzzy` | `fuzz.*`, `process.extract`, blocking deduplication. |
| `Lodestar.Metrics` | classification metrics at scikit-learn parity. |

Four cross-cutting facts explain most of the layout, and none of them is visible
from a single file.

### 1. Two target frameworks, one public API

Everything ships `net10.0;netstandard2.0` in a single package. `netstandard2.0`
reaches equivalent behaviour through conditional compilation, **never a reduced
API**. Gaps are closed in a fixed order: PolySharp polyfills → `System.Memory` /
`System.Numerics.Vectors` / `System.Text.Json` referenced only on that target →
hand-written fallback. `src/Shared/` holds `Guard`, `StringCompat` and friends,
compiled into every library under `Lodestar.Internal` with a global using, so no
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
and nowhere else. `Lodestar.Fuzzy` reaches `Lodestar.Text` through a
`PackageReference` on a **published floor** pinned in `src/Directory.Packages.props`
— which is what makes `git clone && dotnet build` work with no pack step. A CI job
asserts through evaluated MSBuild that no `src/` project carries a
`ProjectReference`.

When a branch edits two packages together, the floor points at an older
`Lodestar.Text` than your working tree:

```bash
# POSIX (bash/zsh)
export LodestarUseProjectRefs=true   # local developer loop only; CI never sets it
```

```powershell
# PowerShell
$env:LodestarUseProjectRefs = 'true'   # local developer loop only; CI never sets it
```

Unset it before measuring anything — with it on you are building a graph that will
never ship. CONTRIBUTING.md's
[*Working across two packages*](CONTRIBUTING.md#working-across-two-packages) has
the release order that gets a branch with it on to green. Release tags are
`<PackageId>/v<Version>`.

### 3. Conformance is proven by frozen oracles, not by hand-written expectations

Every algorithm replays reference values captured from the canonical Python
library (rapidfuzz, jellyfish, textdistance, difflib, scikit-learn, nltk,
HuggingFace `tokenizers`, sentencepiece, numpy, ONNX Runtime) into
`tests/oracles/*.json`, compared at `1e-9` for floats and exactly for strings.
Python is a **development dependency only**.

Three traps, each of which has already cost a session:

- **Run the generator from a neutral working directory** — CONTRIBUTING.md's
  [*Oracle validation*](CONTRIBUTING.md#oracle-validation) has why and the exact
  error `nltk` raises otherwise.
- **Read the generator's own exit code**, never a pipeline's. `python … | tail`
  reports `tail`'s status, so a failed generation looks successful — and the drift
  check that follows then proves nothing, because nothing was regenerated.
- **The `Oracles are reproducible` job is occasionally flaky** — the same commit
  has gone red then green, because drift depended on which CPU the runner landed
  on. Re-run before believing it. On failure the job uploads the regenerated
  corpora as an artefact so the comparison can be made off the runner.

Where behaviour deliberately diverges from the Python reference, it goes in
[`docs/decisions/`](docs/decisions/README.md), the fastest way to understand why
something looks wrong. `docs/equivalence.md` maps each Python call to its C#
counterpart; **a row lands in the same commit as the function**, not afterwards.

### 4. The analyzers gate the build, not the pull request

`SonarAnalyzer.CSharp` is referenced by every project under `src/`, `tests/`,
`bench/` and `samples/`, and the .NET code-quality rules run at
`AnalysisMode=All` with `AnalysisLevel` pinned to `10.0` — CONTRIBUTING.md's
[*Analyzers*](CONTRIBUTING.md#analyzers) has what that costs a finding. The
analyzer version is pinned once as `$(LodestarSonarAnalyzerVersion)` in the root
`Directory.Build.props`; raising it or `AnalysisLevel` surfaces new rules and is
its own change.

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

`dotnet build Lodestar.slnx` does **not** reach `samples/` — they are outside the
solution. Duplication and coverage are visible only to SonarCloud, so a green
local build is not a green quality gate.

## Three gates that constrain how code is written

- **The packaging gate.** `samples/Lodestar.Sample` consumes the packages from
  `./artifacts` through `samples/NuGet.config`, and every new public type must be
  reachable from it by a member reference. Adding public API means adding a use of
  it in `Lot*.cs`. Both sample builds need a fresh `pack` **and** an isolated
  `NUGET_PACKAGES`, or they judge the published packages instead of the working
  tree (ADR 0009).
- **The doc-snippets gate.** Every ` ```csharp ` fence in `README.md`,
  `docs/guides/` and `docs/reference/*/*.md` is compiled against the packed
  packages, so a renamed method fails CI. The reference fences are **executed**
  on top of that, and a trailing `// =>` on one is an assertion on the value the
  page promises — CONTRIBUTING.md's [*Definition of
  done*](CONTRIBUTING.md#definition-of-done), items 5 and 6, have the two
  opt-out markers.
- **The reference gate.** A new public type or method in a namespace listed in
  `docs/wiki-map.json`'s `covered` table needs an entry in its package's
  reference page under `docs/reference/`, checked against both target
  frameworks' assemblies — a signature that drifts from its documentation fails
  CI rather than a reader. Only the namespaces `covered` names are enforced; the
  rest of the surface waits on the reference page that has not been written yet.

## Provenance — two hard rules

- **Never transcribe GPL-licensed code** — CONTRIBUTING.md's
  [*Licensing and provenance*](CONTRIBUTING.md#licensing-and-provenance) has the
  rule, and ADR 0003 the reasoning. Reading a reference implementation to
  diagnose one failing case is diagnosis and is fine; deriving the
  implementation from it is not.
- **Never commit model weights.** Test fixtures are small and synthetic; vocabularies
  are fetched against a pinned SHA-256 by `tools/fetch_*.py`.

## Workflow

GitHub flow, one concern per branch, `<type>/<issue>-<kebab-summary>`
(`feat/`, `fix/`, `perf/`, `docs/`, `chore/`). Reference the issue with
`Closes #n`. Everything written in English — code, comments, ADRs, commit
messages, PR bodies. Comments are held to four rules — say why not what, carry
what would check the claim, two lines inline or eight of prose in XML
documentation, and a marker with its reason past that. `CONTRIBUTING.md`'s
*Claims in comments* is the statement;
`tools/check_comment_length.py` counts the lines and
`.github/instructions/comment_claims.instructions.md` carries what a review
asks about one. Commit messages carry no `feat:`/`fix:` prefix.

`main` is protected by four required checks with no bypass list. "Require
approvals" is off because a single maintainer cannot approve their own PR. Do not
commit, merge or tag unless asked. A `perf/` PR carries before/after numbers and
names the machine.

**Clear Sonar findings before committing, not after.** A green build is not a
clean Sonar, and a finding introduced by a pull request blocks its merge.

Design specs and implementation plans live in `docs/superpowers/specs/` and
`docs/superpowers/plans/`, named `<date>_<issue id padded to 4>_<slug>.md` — the repository's
naming, which overrides the skills' own default. The skills that write them are vendored under
[`.claude/skills/`](.claude/skills/README.md) rather than installed, so they are reachable from a
hosted session; `writing-plans` and `brainstorming` are the two to read first. **Do not reproduce
the format by copying a neighbouring file.** Two plans written that way failed `writing-plans`'
own self-review ([#454](https://github.com/CyrilB1531/lodestar/issues/454)), most visibly against
its **No Placeholders** rule: a step that says what to do without showing how is a plan failure,
and code steps carry code.

**A spec may be written after the fact; a plan may not.** A spec records measured facts and
rejected options, and is still a record when written late — issues #202 to #446 were backfilled
that way, from the commits that closed them. A plan is an instrument for work that has not
started, with checkbox steps and a `Branch:` line, so one written for merged work is checkboxes
nobody may tick and a branch that no longer exists. **Date a backfilled spec by the work, not by
the day it was written**, or the directory loses its ordering; its status line says it is
retrospective.

### SonarQube MCP server

`.github/instructions/sonarqube_mcp.instructions.md` applies to this repository:
disable automatic analysis with `toggle_automatic_analysis` when starting a task,
call `analyze_file_list` on the files you created or modified at the end, then
re-enable it. Look project keys up with `search_my_sonarqube_projects` rather than
guessing, and do not try to confirm a fix through `search_sonar_issues_in_projects`
— the server will not reflect the change yet.
