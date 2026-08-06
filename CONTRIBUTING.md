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

Protection is therefore built on checks rather than approvals. A pull request may
merge once CI is green:

| Job | What it guards |
| --- | --- |
| `Lint (markdown + C# format)` | markdownlint, and `dotnet format --verify-no-changes` |
| `Build, test, pack` | the build, the full test suite, and that the packages still pack |
| `Oracles are reproducible` | that the committed corpora match a fresh generation |
| `Build and analyze` | publishes analysis and coverage to SonarQube Cloud |

"Require approvals" stays off until a second maintainer joins. Self-merging after
green checks is the expected flow here, not a shortcut — the pull request still
earns its keep as the place CI runs against the merge result, and as the record
of why a change was made.

## Definition of done

A change is not finished until all of these hold:

1. **`dotnet build` is clean.** Warnings are errors repository-wide
   (`TreatWarningsAsErrors` in the root `Directory.Build.props`), covering `src`,
   `tests` and `bench` alike — so a warning fails the build.
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

## Analyzer suppressions

Deliberate suppressions live in the source, as a `#pragma warning disable` with a
comment giving the reason:

```csharp
// SonarLint S3776: cognitive complexity: faithful port of a published
// rule-engine; decomposing it would break the 1:1 mapping with the reference.
#pragma warning disable S3776
```

Do not reach for `.editorconfig` or `.vscode/settings.json` for SonarLint rules.
SonarLint reads neither: it ignores `.editorconfig` entirely, and `sonarlint.rules`
is declared application-scope in the extension manifest, so VS Code silently drops
it from a workspace file. The pragma works because SonarLint's C# analysis is
SonarAnalyzer running through Roslyn.

A suppression needs a justification a reviewer can disagree with. "Too noisy" is
not one.

**When suppressed code moves, the suppression does not follow it.** Extracting a
method into a new file leaves the `#pragma` behind in the file the code left, and
the rule reappears against the new one. This has already happened twice while
extracting the shared Snowball framework — `CA1845`, then `S3267`. Nothing
enforces it, so check the analyser after any extraction, not just the build.

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
