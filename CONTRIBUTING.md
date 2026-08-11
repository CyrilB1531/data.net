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
request. That constrains how `main` can be protected: **GitHub does not let you
approve your own pull request**, so a rule requiring an approving review would
block every PR here — there would be nobody able to give it.

Protection is therefore built on checks rather than approvals. A repository
ruleset named **`main protected by checks`** targets the default branch, requires
a pull request, and requires these four checks to pass before the merge button
becomes available:

| Job | What it guards |
| --- | --- |
| `Lint (markdown + C# format)` | markdownlint, and `dotnet format --verify-no-changes` |
| `Build, test, pack` | the build, the full test suite, and that the packages still pack |
| `Oracles are reproducible` | that the committed corpora match a fresh generation |
| `Build and analyze` | publishes analysis and coverage to SonarQube Cloud, **and fails the job when the quality gate fails** — a finding in the code a pull request introduces blocks its merge |

The ruleset has **no bypass list, and it binds the administrator**. That is
deliberate: a guard rail the sole maintainer can step over on a tired evening is
a suggestion. Getting past it means disabling the rule in Settings → Rules, which
is a visible act with a record, rather than a merge nobody would have noticed.

`Build and analyze` is skipped on Dependabot and fork pull requests, where
`SONAR_TOKEN` is unreachable. GitHub counts a skipped check as satisfied, so
those pull requests are not stuck — which is also why the required check is this
repository's own job and not SonarQube Cloud's `SonarCloud Code Analysis`: that
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
5. **The C# in the guides still compiles.** Every ```` ```csharp ```` fence in
   `README.md` and `docs/guides/` is extracted from the Markdown and built
   against the packed packages — there is no second copy, so a snippet cannot
   drift from the API. A fence that genuinely cannot compile opts out with
   `<!-- docs-compile: skip - reason -->` on the line above it, and the reason
   has to be one a reviewer can disagree with.

```bash
dotnet build DataNet.slnx -c Release
dotnet test DataNet.slnx -c Release
dotnet format DataNet.slnx --verify-no-changes
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
python3 tools/check_version_floor.py
python3 tools/extract_doc_snippets.py && dotnet build samples/DataNet.DocSnippets -c Release
```

`check_version_floor.py` is offline and instant; it catches the version numbers
that must agree drifting apart, which MSBuild is perfectly happy to let happen. CI runs it
with `--check-feed`, which additionally proves the dependency floor is published
— see [`tools/README.md`](tools/README.md). If you touched packaging, packing and
running `python3 tools/check_nuspec_dependencies.py ./artifacts --require-all`
closes the loop.

## Before pushing: the half the build cannot see

`dotnet build` enforces the Sonar rules that live in `.globalconfig` (see
[Analyzers](#analyzers) below), but it has no view of three things the quality
gate on the pull request still judges: the Python rules over `tools/`,
duplication, and coverage. `tools/sonarqube-local/compose.yaml` runs a disposable
SonarQube Community server that covers all three, for whoever wants that answer
before pushing rather than after.

```bash
cd tools/sonarqube-local && docker compose up -d
# Podman instead of Docker Engine: podman compose up -d
# Wait for the server rather than sleeping blind:
until curl -s http://localhost:9000/api/system/status | grep -q '"status":"UP"'; do sleep 5; done
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
  /d:sonar.exclusions="tests/oracles/**,samples/DataNet.DocSnippets/Generated/**"
dotnet build DataNet.slnx -c Release --no-incremental
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
a light one: duplication and coverage sensors both ran (2.0% duplicated lines,
28 duplicated blocks; coverage reads 0.0% because this run's commands, matching
the ones above, do not feed it a coverage report — CI's job does).

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

The three libraries version and release independently, and `DataNet.Fuzzy`
reaches `DataNet.Text` through a `PackageReference` on the published package
rather than a project reference — the reasoning is in
[`docs/decisions/0012`](docs/decisions/0012-per-package-versioning.md).

A plain clone builds with no extra step: the version `DataNet.Fuzzy` depends on
is a floor pinned in `src/Directory.Packages.props`, and it always names a
release that is already on nuget.org.

**When a branch edits `DataNet.Text` and `DataNet.Fuzzy` together**, that floor
points at a `DataNet.Text` older than the one in your working tree, so
`DataNet.Fuzzy` would compile against the published assembly and not see your
change. Flip the reference back for the duration:

```bash
export DataNetUseProjectRefs=true
dotnet build DataNet.slnx -c Release   # prints a reminder that this is on
```

MSBuild reads environment variables as properties, so one export covers `build`,
`test` and the IDE. Nothing to pass per command, and nothing to pack and restore
between edits.

Two things to keep straight:

- **It is a local loop, not a merge strategy.** CI never sets the property and
  asserts the default path, so a branch whose `DataNet.Fuzzy` needs new
  `DataNet.Text` API cannot go green. Release `DataNet.Text` first, raise the
  floor in `src/Directory.Packages.props`, then land the `DataNet.Fuzzy` side.
  Two packages that release independently cannot also be merged as one.
- **Unset it before measuring anything.** With the property on you are building a
  graph that will never ship; benchmark numbers and packaging checks taken there
  describe nothing real.

### Releasing

Versions are declared per package in `src/<Package>/Version.props` and nowhere
else. To release one: bump that file, land it on `main`, then tag
`<PackageId>/v<Version>` (for example `DataNet.Fuzzy/v0.3.0`). The workflow
compares the tag against the declared version and refuses to publish if they
disagree — the tag chooses *which* release to cut, it does not set the number.
Add the entry under a per-package heading in `CHANGELOG.md`.

## Oracle validation

New algorithms are validated by replaying reference outputs captured from the
canonical Python library — not by trusting that the C# passes tests someone wrote
alongside the implementation.

1. Add a generator section to [`tools/generate_oracles.py`](tools/generate_oracles.py).
2. Regenerate, and commit the resulting `tests/oracles/*.json`:

   ```bash
   cd /tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py
   ```

   Run it from a neutral working directory. `nltk` refuses to import its own
   dependencies when they appear to live *under* the current directory, so the
   run fails — even with `PYTHONSAFEPATH` set — whenever the working directory is
   an ancestor of the virtualenv:

   ```text
   ImportError: Blocked import of regex from current working directory for security reasons
   ```

   Running from `/tmp` with the virtualenv inside the repository satisfies this.
   Running from the repository root, or from `~`, does not.

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
[`docs/decisions/`](docs/decisions/) rather than in a code comment alone — see
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
dotnet build DataNet.slnx -c Release
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
`.globalconfig` at the repository root with no wiring — the SDK's
`Microsoft.Managed.Core.targets` already globs every ancestor directory of every
compiled file for a file with exactly that name — so nothing declares it, and
nothing should.

CI's `Lint` job runs the same generator with `--check` on every pull request,
comparing against the committed file without writing it. A red **`Sonar
globalconfig is current`** step means the SonarCloud profile has moved since the
file was last generated: regenerate with the command above and commit the
result, the same as any other generated file here.

Regenerating is required, not optional, whenever `AnalysisLevel` is raised past
`10.0` or `$(DataNetSonarAnalyzerVersion)` is bumped — either can change which
rules the package ships disabled, which changes the delta the file encodes. This
applies whether the bump is a deliberate edit or an automated dependency update
(a Dependabot pull request, should one ever be wired for this pin): skip the
regeneration and the `Sonar globalconfig is current` job goes red with a diff and
no explanation of why, on a pull request that touched no C#.

It is an analyzer-only reference (`PrivateAssets="all"`), so it reaches no
published package — `tools/check_nuspec_dependencies.py` asserts that. The
version is pinned once, as `$(DataNetSonarAnalyzerVersion)` in the root
`Directory.Build.props`; raising it will usually surface new rules and therefore
a cleanup, so treat it as its own change. `AnalysisLevel` is pinned to `10.0`
for the same reason. See
[`0015`](docs/decisions/0015-sonar-rules-in-the-build.md) and
[`0019`](docs/decisions/0019-the-net-analysers-run-in-the-build-too.md).

The command above does not reach `samples/`: the samples are outside
`DataNet.slnx` and consume the packages from a local feed, so they are analysed
only when the samples themselves are built — which needs a `pack` first, and
happens in three CI jobs: `Sample consumes the packages`, `Guide snippets
compile`, and the samples build inside `Build and analyze`. Expect a finding
there from CI rather than from `dotnet build DataNet.slnx`.

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
build stayed green, because nothing in it ran the analyzer; that is no longer
true for `src/`, `tests/` and `bench/`, where the rule now reappears as a build
error at the moment of the extraction — nor for `samples/`, where it reappears
when the samples are built.

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
