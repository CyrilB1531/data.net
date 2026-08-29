# Contributing

## Branching model — GitHub flow

`main` is always releasable. All work happens on a short-lived branch off `main`
and comes back through a pull request. Nothing is committed straight to `main`.

```bash
git switch main && git pull
git switch -c feat/spanish-snowball-stemmer
# … work, commit …
git push -u origin feat/spanish-snowball-stemmer
gh pr create --fill
```

Branch naming — `<type>/<short-kebab-summary>`, optionally prefixed with the
issue number (`feat/2-spanish-snowball-stemmer`):

| Prefix | For |
| --- | --- |
| `feat/` | a new algorithm or public capability |
| `fix/` | a correctness fix |
| `perf/` | a measured optimization (attach before/after numbers) |
| `docs/` | documentation only |
| `chore/` | build, CI, tooling, dependencies |

Keep a branch to one concern. "One algorithm at a time, complete" applies to
branches too: a new stemmer branch carries its implementation, its oracle corpus
and its tests — and nothing else. If you find an unrelated problem while working,
open an issue rather than widening the branch.

Reference the issue from the pull request (`Closes #12`) so it closes on merge.

### Review, with a single maintainer

The project currently has one maintainer, who reviews and merges every pull
request. That constrains how `main` can be protected. **GitHub does not let you
approve your own pull request**, so a rule requiring an approving review would
block every PR here — there would be nobody able to give it.

Protection is therefore built on checks rather than approvals. A repository
ruleset named **`main protected by checks`** targets the default branch, requires
a pull request, and requires these four checks to pass before the merge button
becomes available:

| Job | What it guards |
| --- | --- |
| `Lint (markdown + C# format)` | markdownlint, `dotnet format --verify-no-changes`, the `tools/tests` suite, that no tracked file holds a machine path, and that the Sonar `.globalconfig` is current |
| `Build, test, pack` | the build, the full test suite, and that the packages still pack |
| `Oracles are reproducible` | that the committed corpora match a fresh generation |
| `Build and analyze` | publishes analysis and coverage to SonarQube Cloud, **and fails the job when the quality gate fails** — a finding in the code a pull request introduces blocks its merge |

The ruleset has **no bypass list, and it binds the administrator**. That is
deliberate: a guard rail the sole maintainer can step over on a tired evening is
a suggestion. Getting past it means disabling the rule in Settings → Rules, which
is a visible act with a record, rather than a merge nobody would have noticed.

`Build and analyze` is skipped on Dependabot and fork pull requests, where
`SONAR_TOKEN` is unreachable. GitHub counts a skipped check as satisfied, so
those pull requests are not stuck. This is also why the required check is this
repository's own job and not SonarQube Cloud's `SonarCloud Code Analysis`. That
one is never posted at all on such a pull request, and a required check that
never arrives stays pending forever.

"Require approvals" stays off until a second maintainer joins. Self-merging after
green checks is the expected flow here, not a shortcut — the pull request still
earns its keep as the place CI runs against the merge result, and as the record
of why a change was made.

## Definition of done

A change is not finished until all of these hold:

1. **`dotnet build` is clean.** Warnings are errors repository-wide
   (`TreatWarningsAsErrors` in the root `Directory.Build.props`), covering `src`,
   `tests`, `bench` and `samples` alike — so a warning fails the build.
2. **`dotnet test` passes**, and any new algorithm replays a frozen oracle
   corpus. Conformance is *proven*, never assumed — see below.
3. **Lint is clean**: `dotnet format --verify-no-changes` and markdownlint.
4. **Public API carries XML documentation**, naming the Python function whose
   behavior it matches.
5. **The C# in the documentation still compiles.** Every ```` ```csharp ````
   fence in `README.md`, `docs/guides/` and `docs/reference/<package>/` is
   extracted from the Markdown and built against the packed packages — there is
   no second copy, so a snippet cannot drift from the API. The reference pages'
   fences are then **executed**, so a result a page promises is checked rather
   than trusted; item 6 has the `// =>` marker that states one. A fence that
   genuinely cannot compile opts out with
   `<!-- docs-compile: skip - reason -->` on the line above it, and one that
   compiles but cannot be run with `<!-- docs-run: skip - reason -->`; the
   reason has to be one a reviewer can disagree with.
6. **A new public type or method carries a reference entry.** The pages under
   `docs/reference/<package>/` follow the layout of the .NET API reference: a `###` entry per
   exported type, a `####` entry per public method with all overloads sharing it, and inside an
   entry, in order — a one-sentence summary, the declaration under a `<!-- docs-declaration -->`
   marker, **Parameters**, **Returns**, **Exceptions**, **Example**, **Remarks**, **Applies to**,
   **See also**. Empty rubrics are left out rather than filled with "none".

   The prose a reader came for lives in **Remarks**: what the member is for, when to prefer it to
   its neighbour, and the trap. The Python counterpart is not repeated — link
   [`docs/equivalence.md`](docs/equivalence.md) under **See also**.

   In an **Example**, a `// =>` comment is an assertion the CI executes; a plain `//` stays a
   comment. The value must be bound to a local first, and a trailing `…` means prefix match. A
   fence that cannot be executed carries `<!-- docs-run: skip - reason -->` on the line above.

   A Mermaid diagram is welcome where it shows a mechanism prose cannot hand a reader in one
   glance, and is removed in review when it only restates the sentence above it.

   Which namespaces are enforced is declared in [`docs/wiki-map.json`](docs/wiki-map.json), and
   `ReferenceDocumentationTests` fails the build when a page and the assembly disagree.

   **Exceptions** is checked against the member's own `<exception cref>` tags: the two must name
   the same set of types, in either order, so a `throw` added to a member owes both edits in the
   same commit. The sentence around each type is not compared — *when* it is thrown stays a
   review question. A namespace still owing that parity is named in the map's
   `exceptionsUnchecked` list, which only ever shrinks; see
   [ADR 0038](docs/decisions/0038-the-gate-confronts-an-exception-tag-with-the-page-that-documents-it.md).

   A member that has a reference entry is linked to it wherever it is named in prose or in a
   table. Using it obliges the page as well: a member named anywhere on a page — inside a
   ```` ```csharp ```` fence included, where Markdown cannot carry a link — has to be linked to
   its entry at least once somewhere on that page, so a reader who meets it has a way to find
   out what it does. `ReferenceDocumentationTests` fails the build on either. A reference page
   is exempt for its own members: its headings are the entries.

   The page's opening table is navigation, not a summary: every exported type gets a row, its
   name linked to its own `###` entry — `` [`Levenshtein`](#levenshtein) ``. The anchor is
   GitHub's slug rule, lower-cased with dots dropped, and `ReferenceDocumentationTests` fails
   the build on a row with no link.
7. **A change to shipped behaviour carries its `CHANGELOG.md` entry**, under the package's
   heading in `[Unreleased]`, in the shape [Releasing](#releasing) sets: one sentence, the issue
   and the commit. New public surface, a fixed defect and a measured performance change all
   qualify; a refusal does not, because it changed nothing a caller can observe — the decision
   record is where that lives.

   This is item 7 rather than a line under *Releasing* because it was one, and four lots shipped
   without it — including [#450](https://github.com/CyrilB1531/lodestar/issues/450), a whole
   public API. Every other item here is checked by a gate that fails the build; this one is not,
   which is exactly why it needs to be read alongside them rather than at release time.

```bash
dotnet build Lodestar.slnx -c Release
dotnet test Lodestar.slnx -c Release
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
python3 tools/check_version_floor.py
python3 tools/extract_doc_snippets.py && dotnet build samples/Lodestar.DocSnippets -c Release
python3 tools/check_machine_paths.py
```

```powershell
# PowerShell — split, not chained with `&&`, which needs PowerShell 7+
python tools/check_version_floor.py
python tools/extract_doc_snippets.py
dotnet build samples/Lodestar.DocSnippets -c Release
python tools/check_machine_paths.py
```

`check_version_floor.py` is offline and instant. It catches the version numbers
that must agree drifting apart, which MSBuild is perfectly happy to let happen. CI runs it
with `--check-feed`, which additionally proves the dependency floor is published
— see [`tools/README.md`](tools/README.md). If you touched packaging, packing and
running `python3 tools/check_nuspec_dependencies.py ./artifacts --require-all` (`python …` on
Windows, per the split above) closes the loop.

`check_machine_paths.py` refuses a tracked file that holds a path under someone's
home directory. `/tmp` is deliberately allowed, other than the session
scratch-directory shape `/tmp/claude-<digits>/`, which is the one that carried
eight of the ten paths this guard exists because of. `/usr`, `/etc`, `~/.nuget`
and other system paths are allowed too. An ordinary account name can still
collide with the probes derived from `$HOME`; `--no-environment` skips those
and keeps the named shapes enforcing.

## Before committing: the guards, one command earlier

The four guards above are CI steps, so by default the first thing that tells you
a machine path reached a tracked file is a red job on a pull request. A tracked
hook removes that round trip, and installing it is one command with no
dependency:

```bash
git config core.hooksPath .githooks
```

```powershell
git config core.hooksPath .githooks
```

`.githooks/pre-commit` then runs `check_machine_paths.py`,
`check_comment_length.py`, `check_version_floor.py`, `check_sample_culture.py`,
`check_bench_map.py`, `check_sample_coverage.py` and
`check_no_console_writeline.py` before every commit, reports every one that failed
rather than the first, and refuses the commit if any did. It resolves `python3`
then `python` — neither name is safe to assume on both platforms — and, on a
machine with neither, says so and lets the commit through rather than blocking
work over a development dependency.

**It is skippable, on purpose.** `git commit --no-verify` bypasses it for one
commit, and its failure message says so. A hook that presents itself as
mandatory is a hook that gets deleted rather than skipped, and a deleted hook is
silent next time.

Three things are worth knowing before relying on it.

- **It is not a rehearsal of CI.** It runs the guards and nothing else: not the
  build, not the tests, not the packaging, doc-snippets or reference gates, not
  Sonar. A green commit is not a green pull request.
- **It runs in about a second, and that is the whole budget.** Measured on one
  machine over the whole tree: 0.97 s for `check_machine_paths.py`, 0.12 s for
  `check_comment_length.py`, 0.06 s for `check_version_floor.py`, 0.03 s for
  `check_sample_culture.py` — **1.18 s** for the four in sequence. Anything that
  reached `dotnet build` would be uninstalled within a week.
- **It reads the worktree, not the commit.** `git ls-files` reports the index,
  so a newly `git add`ed file *is* checked — which running the scripts by hand
  does not do. Their contents are then read from disk, so a file staged in one
  state and edited in another is judged in its worktree state.

Two guards CI runs stay out of it: `check_nuspec_dependencies.py` needs a packed
`./artifacts`, and `check_version_floor.py --check-feed` reaches nuget.org. The
reasoning, and the alternative of adopting `pre-commit` instead, are in
[decision 0037](docs/decisions/0037-the-guards-run-before-the-commit.md).

## Before pushing: the half the build cannot see

`dotnet build` enforces the Sonar rules that live in `.globalconfig` (see
[Analyzers](#analyzers) below), but it has no view of three things the quality
gate on the pull request still judges: the Python rules over `tools/`,
duplication, and coverage. `tools/sonarqube-local/compose.yaml` runs a disposable
SonarQube Community server that covers all three, for whoever wants that answer
before pushing rather than after.

```bash
# POSIX (bash/zsh)
cd tools/sonarqube-local && docker compose up -d
# Podman instead of Docker Engine: podman compose up -d
# Wait for the server rather than sleeping blind:
until curl -s http://localhost:9000/api/system/status | grep -q '"status":"UP"'; do sleep 5; done
```

```powershell
# PowerShell — split, not chained with `&&`, which needs PowerShell 7+
cd tools/sonarqube-local
docker compose up -d
# Podman instead of Docker Engine: podman compose up -d
# Wait for the server rather than sleeping blind. -ErrorAction SilentlyContinue does not
# stop Invoke-RestMethod's connection-refused error from aborting a do/until, so this
# catches it explicitly instead — verified against a port with nothing listening yet:
while ($true) { try { if ((Invoke-RestMethod http://localhost:9000/api/system/status).status -eq 'UP') { break } } catch { }; Start-Sleep -Seconds 5 }
```

The figures below were measured with Podman on one machine, which is what makes
them traceable rather than asserted.

SonarQube Community bundles Elasticsearch, which wants `vm.max_map_count >= 262144`.
`SONAR_ES_BOOTSTRAP_CHECKS_DISABLE=true` in the compose file suppresses the startup
check, not the underlying requirement, so a container that exits immediately on a
machine at a lower distribution default (many ship `65530`) needs that raised —
`sudo sysctl -w vm.max_map_count=262144`, or the persistent form in
`/etc/sysctl.conf` — before trying again.

Then, from the repository root, with a token created in the local server's UI
(*My Account → Security*, `local` is a fine name) exported as `SONAR_TOKEN`:

```bash
dotnet tool install --global dotnet-sonarscanner   # once, if absent
dotnet sonarscanner begin /k:"datanet-local" \
  /d:sonar.host.url="http://localhost:9000" /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.python.version="3.12" \
  /d:sonar.exclusions="tests/oracles/**,samples/Lodestar.DocSnippets/Generated/**"
dotnet build Lodestar.slnx -c Release --no-incremental
dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"
```

Measured on this machine: the image (`sonarqube:community`, pinned by digest in
the compose file) is **≈1.4 GB** and took **38 s** to pull. Bringing the
container up is near-instant (2 s to launch), but the server itself takes
roughly **56 s** from launch to answering `"status":"UP"`. The three scanner
commands — `begin`, `dotnet build --no-incremental`, `end` — took **5 s**,
**39 s** and **31 s** respectively (75 s total) against this repository's
current size (17 192 lines of code: 13 361 C#, 3 815 Python, 16 XML). The server
reported **0 findings for C# and 0 for Python** on that run — a clean tree, not
a light one. Duplication and coverage sensors both ran (2.0% duplicated lines,
28 duplicated blocks). Coverage reads 0.0% because this run's commands, matching
the ones above, do not feed it a coverage report — CI's job does.

This is not a rehearsal of `Build and analyze`, and saying otherwise would make
the document worse than not writing it:

- the Community edition has no branch or pull-request analysis, so the verdict
  is over the whole project and never over the diff — which is the axis the
  real gate judges on;
- the custom `No new issue` gate and its seven conditions are not there — a
  fresh local server starts with only the default `Sonar way` gate, and this run
  was evaluated against that one instead;
- its analyser versions move independently of the server's, so a rule firing
  (or not) here does not pin down which version fired it on SonarCloud;
- the Community edition carries no taint-analysis engine, so `PythonSecuritySensor`
  and its injection-class vulnerabilities (SSRF, path traversal, and the like) are
  invisible to it — a script that reads an argument into `Path.read_text` or hands
  one to `urlopen` looks clean here and can still fail the real gate (issue #131).

A finding it reports is real, a clean run promises nothing.

## Working across two packages

The four libraries version and release independently, and `Lodestar.Fuzzy`
reaches `Lodestar.Text` through a `PackageReference` on the published package
rather than a project reference — the reasoning is in
[`docs/decisions/0012`](docs/decisions/0012-per-package-versioning.md).

A plain clone builds with no extra step: the version `Lodestar.Fuzzy` depends on
is a floor pinned in `src/Directory.Packages.props`, and it always names a
release that is already on nuget.org.

**When a branch edits `Lodestar.Text` and `Lodestar.Fuzzy` together**, that floor
points at a `Lodestar.Text` older than the one in your working tree, so
`Lodestar.Fuzzy` would compile against the published assembly and not see your
change. Flip the reference back for the duration:

```bash
# POSIX (bash/zsh)
export LodestarUseProjectRefs=true
dotnet build Lodestar.slnx -c Release   # prints a reminder that this is on
```

```powershell
# PowerShell
$env:LodestarUseProjectRefs = 'true'
dotnet build Lodestar.slnx -c Release   # prints a reminder that this is on
```

MSBuild reads environment variables as properties, so one export covers `build`,
`test` and the IDE. Nothing to pass per command, and nothing to pack and restore
between edits.

Two things to keep straight:

- **It is a local loop, not a merge strategy.** CI never sets the property and
  asserts the default path, so a branch whose `Lodestar.Fuzzy` needs new
  `Lodestar.Text` API cannot go green. Release `Lodestar.Text` first, raise the
  floor in `src/Directory.Packages.props`, then land the `Lodestar.Fuzzy` side.
  Two packages that release independently cannot also be merged as one.
- **Unset it before measuring anything.** With the property on you are building a
  graph that will never ship; benchmark numbers and packaging checks taken there
  describe nothing real.

### Releasing

Versions are declared per package in `src/<Package>/Version.props` and nowhere
else. To release one: bump that file, land it on `main`, then tag
`<PackageId>/v<Version>` (for example `Lodestar.Fuzzy/v0.3.0`). The workflow
compares the tag against the declared version and refuses to publish if they
disagree. The tag chooses *which* release to cut; it does not set the number.
Add the entry under a per-package heading in `CHANGELOG.md`.

Each entry is one sentence, the issue and the commit — nothing else. The why
lives in the issue and the how in the commit; restating either in the
changelog is the misplacement this shape exists to avoid, so an entry carries
no rationale, no measurement and no caveat:

```markdown
- The byte-level decode substitutes U+FFFD instead of throwing. ([#149](https://github.com/CyrilB1531/data.net/issues/149), [`5948a59`](https://github.com/CyrilB1531/data.net/commit/5948a59))
```

An entry whose commit closed no issue keeps the sentence and the commit link
alone, rather than a fabricated issue link.

## Oracle validation

New algorithms are validated by replaying reference outputs captured from the
canonical Python library — not by trusting that the C# passes tests someone wrote
alongside the implementation.

1. Add a generator section to [`tools/generate_oracles.py`](tools/generate_oracles.py).
2. Regenerate, and commit the resulting `tests/oracles/*.json`:

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

   Run it from a neutral working directory. `nltk` refuses to import its own
   dependencies when they appear to live *under* the current directory, on POSIX and
   Windows alike, so the run fails — even with `PYTHONSAFEPATH` set — whenever the
   working directory is an ancestor of the virtualenv:

   ```text
   ImportError: Blocked import of regex from current working directory for security reasons
   ```

   Running from `/tmp` (POSIX) or `$env:TEMP` (PowerShell), with the virtualenv inside the
   repository, satisfies this. Running from the repository root, or from `~`, does not.

   Check the generator's own exit code, not a pipeline's. `python … | tail` reports
   `tail`'s status, so a failed generation looks successful — and the drift check
   that follows then proves nothing, because nothing was regenerated.
3. Add a test that replays the corpus, with a `1e-9` tolerance for floating-point
   results and exact comparison for strings.

Generation must be deterministic: a fixed seed, no wall-clock timestamps, no
unordered iteration. The `Oracles are reproducible` CI job regenerates and fails
on any drift, so a corpus that is not byte-reproducible will block the pull
request.

### Dependencies

`tools/requirements.txt` is the human-edited input; `tools/requirements.lock.txt`
is generated and pins the whole resolved graph with hashes. CI installs from the
lock, so a transitive bump cannot change the corpora behind your back. After
editing the input, regenerate:

```bash
pip install pip-tools
pip-compile --generate-hashes --strip-extras   --output-file tools/requirements.lock.txt tools/requirements.txt
```

Then regenerate the corpora and confirm they are unchanged. If they move, the
dependency bump changed reference output — resolve that deliberately, in the same
commit, rather than letting it land on someone else's pull request.

Where behavior deliberately diverges from the Python reference, record it in
[`docs/decisions/`](docs/decisions/README.md) rather than in a code comment alone — see
[`0005`](docs/decisions/0005-hamming-jellyfish-divergence.md) for the shape of
one.

## Analyzers

### Where the rules run

`SonarAnalyzer.CSharp` is referenced by every project under `src/`, `tests/`,
`bench/` and `samples/`, and the .NET code-quality rules are on at
`AnalysisMode=All` repository-wide, so **the rules that gate the pull request also
gate `dotnet build`**. Warnings are errors here, which means a Sonar or `CAxxxx`
finding is a compile error on your machine rather than a comment on your pull
request:

```bash
dotnet build Lodestar.slnx -c Release
```

That claim needs two mechanisms, not one. `AnalysisMode=All` covers the .NET
code-quality rules on its own. SonarAnalyzer's own rules do not: the package
ships a large share of them **disabled**, the SonarCloud quality profile enables
some of those, and nothing in `AnalysisMode` closes that gap — a finding there
used to surface only at the quality gate, three minutes after a push (issue #109).
The root **`.globalconfig`** closes it: a generated file, not a hand-written one,
that raises exactly the rules the profile activates and the package ships
disabled, to `warning`. Regenerate it with
[`tools/generate_sonar_globalconfig.py`](tools/generate_sonar_globalconfig.py) —
see [`tools/README.md`](tools/README.md#generate_sonar_globalconfigpy) for the
full command, including where it reads the SARIF error log from. `dotnet build` picks up
`.globalconfig` at the repository root with no wiring. The SDK's
`Microsoft.Managed.Core.targets` already globs every ancestor directory of every
compiled file for a file with exactly that name, so nothing declares it, and
nothing should.

CI's `Lint` job runs the same generator with `--check` on every pull request,
comparing against the committed file without writing it. A red **`Sonar
globalconfig is current`** step means the SonarCloud profile has moved since the
file was last generated: regenerate with the command above and commit the
result, the same as any other generated file here.

Regenerating is required, not optional, whenever `AnalysisLevel` is raised past
`10.0` or `$(LodestarSonarAnalyzerVersion)` is bumped. Either can change which
rules the package ships disabled, which changes the delta the file encodes. This
applies whether the bump is a deliberate edit or an automated dependency update
(a Dependabot pull request, should one ever be wired for this pin): skip the
regeneration and the `Sonar globalconfig is current` job goes red with a diff and
no explanation of why, on a pull request that touched no C#.

It is an analyzer-only reference (`PrivateAssets="all"`), so it reaches no
published package — `tools/check_nuspec_dependencies.py` asserts that. The
version is pinned once, as `$(LodestarSonarAnalyzerVersion)` in the root
`Directory.Build.props`; raising it will usually surface new rules and therefore
a cleanup, so treat it as its own change. `AnalysisLevel` is pinned to `10.0`
for the same reason. See
[`0015`](docs/decisions/0015-sonar-rules-in-the-build.md) and
[`0019`](docs/decisions/0019-the-net-analysers-run-in-the-build-too.md).

The command above does not reach `samples/`. The samples are outside
`Lodestar.slnx` and consume the packages from a local feed, so they are analysed
only when the samples themselves are built. That needs a `pack` first, and
happens in three CI jobs: `Sample consumes the packages`, `Guide snippets
compile`, and the samples build inside `Build and analyze`. Expect a finding
there from CI rather than from `dotnet build Lodestar.slnx`.

One thing still only SonarCloud sees, so a green local build is not a green
quality gate: **duplication and coverage**.

### Suppressions

Deliberate suppressions live in the source, as a `#pragma warning disable` with a
comment giving the reason:

```csharp
// SonarLint S3776: cognitive complexity: faithful port of a published
// rule-engine; decomposing it would break the 1:1 mapping with the reference.
#pragma warning disable S3776
```

Do not reach for `.editorconfig` or `.vscode/settings.json` **to change what
SonarLint reports**. SonarLint reads neither: it ignores `.editorconfig`
entirely, and `sonarlint.rules` is declared application-scope in the extension
manifest, so VS Code silently drops it from a workspace file. The pragma works
because SonarLint's C# analysis is SonarAnalyzer running through Roslyn.

The **build** is a different tool: its Roslyn pass does read analyzer
configuration files, which is how `.globalconfig` raises the rules
SonarAnalyzer ships disabled (see [Analyzers](#analyzers) above). That file is
generated from the server's profile — change the profile, or the generator,
never the file.

A rule that a whole area trips *by being that area* — xunit's underscored test
names, BenchmarkDotNet's reflection-instantiated types, a sample printing to the
console — goes in that area's `Directory.Build.props` as a `NoWarn` entry, with a
comment naming each rule and why it does not apply there. A rule that one call
site disagrees with stays a `#pragma warning disable` in the source, with its
reason above it. Never add either without the reason.

A suppression needs a justification a reviewer can disagree with. "Too noisy" is
not one.

**When suppressed code moves, the suppression does not follow it.** Extracting a
method into a new file leaves the `#pragma` behind in the file the code left, and
the rule reappears against the new one. This has already happened twice while
extracting the shared Snowball framework — `CA1845`, then `S3267`. Both times the
build stayed green, because nothing in it ran the analyzer. That is no longer
true for `src/`, `tests/` and `bench/`, where the rule now reappears as a build
error at the moment of the extraction. Nor is it true for `samples/`, where it
reappears when the samples are built.

## Licensing and provenance

The project is Apache-2.0. Two hard rules, expanded in
[`0003`](docs/decisions/0003-provenance-and-licensing.md):

- **Never transcribe GPL-licensed code.** Implement from the *published algorithm
  description*. This is why the stemmers and phonetic encoders are original
  implementations rather than ports of an existing codebase — and it is not a
  formality: an oracle proves the behavior matches without the source needing to.
- **Never commit model weights.** Test fixtures are small and synthetic.

New third-party attributions go in [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).

## Performance claims

Performance is a stated selling point, so numbers are measured, not asserted.
Benchmarks live in [`bench/`](bench/README.md). Attach before/after figures to any
`perf/` pull request, and say which machine produced them.

Verify what you are actually measuring before quoting a result. A benchmark that
silently exercises the wrong build or the wrong code path will still produce
confident-looking numbers.

## Claims in comments

A comment here often carries the reason a divergence from the Python reference exists, which is what makes
that divergence reviewable. That is what makes them load-bearing, and it is also what makes them dangerous:
nothing checks them, and they go stale when the code beside them moves. Four rules, and they bind every
tracked file — `src/`, `tests/`, `tools/`, `bench/`, `samples/`, `docs/` and `docs/superpowers/` alike. A
spec that overclaims what its corpus proves is the same defect as a comment that overclaims what the
reference does.

**A comment says why, never what.** Restating the line below it is noise, and it goes stale faster than the
code does — the code at least gets compiled.

**A claim carries what would check it.** Where it is executable — a measurement, a reference library's
output, a count — run it and cite the corpus case, the file and line, or the command. "Measured" with no
pointer is an assertion wearing a measurement's clothes.

**Two budgets, because the two kinds of prose sit in different places.** An inline comment stands between
a reader and the code, so it gets **two lines** — a sentence, not a paragraph. XML documentation is the
member's own interface, read by a caller who does not have the source and required on every public member,
so it gets **eight**, counted over prose. A `<param>` or an `<exception>` that a well-formed member must
carry does not spend the budget. **The reason above a `#pragma warning disable` is not counted at all**:
[Suppressions](#suppressions) below already demands a reason a reviewer can disagree with, which is a
stricter requirement than brevity and rarely met in two lines. Past either budget, the reasoning belongs in
[`docs/decisions/`](docs/decisions/README.md),
cited from one line — or it needs cutting. `tools/check_comment_length.py` counts them, and CI runs
it in the `Lint` job: a block past its budget with no marker fails the build.

**A longer block carries a marker naming its reason**, as its first line:

```csharp
// long-comment: <why this one needs the room>
```

Longer is allowed where it is necessary; the marker is what stops it becoming the norm. It is held to the
bar a `#pragma warning disable` is held to — a reason a reviewer can disagree with, and "it felt useful" is
not one. A code review judges whether the marker was deserved, because the guard can only see that one
exists.
