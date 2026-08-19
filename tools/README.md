# Development tools — oracle generation, vendored data, packaging checks

Scripts under `tools/`, each responsible for one input the test suite treats as
given:

- `generate_oracles.py` produces the reference values the test suite replays.
- `fetch_stopwords.py` produces source that is *shipped*, which is why it
  verifies what it downloaded before writing anything.
- `fetch_xlmr_vocab.py` and `build_normalizer_fixtures.py` produce fixtures
  `generate_oracles.py` reads.
- `build_tiny_models.py` builds fixtures too small for that pipeline to bother
  with — two ONNX graphs, a trained BPE, and a hand-constructed BPE — and
  commits them directly.
- `check_nuspec_dependencies.py` verifies what the packages *declare*.
- `check_version_floor.py` verifies that the version numbers the source tree
  keeps in three places still agree.
- `check_machine_paths.py` refuses a tracked file that holds a path under
  someone's home directory.
- `check_comment_length.py` refuses a comment block that runs past its budget
  without saying why.
- `check_sample_culture.py` refuses a sample that can print a number in the
  contributor's culture rather than the same way everywhere.
- `check_sample_coverage.py` refuses a public class with no `<Class>Sample.cs`,
  package by package as each is split (decision 0041). The packaging gate already
  asks that a type be *referenced*; this asks which file references it, so an
  example stays where its name says it is.
- All of those but `check_nuspec_dependencies.py`, which needs a packed
  `./artifacts`, also run before a commit for whoever installs
  `.githooks/pre-commit` with `git config core.hooksPath .githooks` —
  CONTRIBUTING.md's [*Before
  committing*](../CONTRIBUTING.md#before-committing-the-guards-one-command-earlier)
  has what it does and does not cover.
- `generate_sonar_globalconfig.py` writes the `.globalconfig` that raises the
  Sonar rules `SonarAnalyzer.CSharp` ships disabled, from the SonarCloud
  quality profile that gates the pull request.
- `extract_doc_snippets.py` turns the ` ```csharp ` fences in `README.md` and
  `docs/` into a project the compiler — and, for the reference pages, the
  runtime — can judge.
- `build_wiki.py` produces what the GitHub wiki publishes: `docs/` turned into
  a flat page per package channel and per released version.
- `sonarqube-local/` holds the compose file for a disposable local SonarQube
  server, covering the Python rules, duplication and coverage that no local
  `dotnet build` reaches — see
  [`../CONTRIBUTING.md`](../CONTRIBUTING.md#before-pushing-the-half-the-build-cannot-see).

## `generate_oracles.py`

`generate_oracles.py` produces the frozen reference corpora under
`tests/oracles/`, from the canonical Python libraries. **These libraries are
development dependencies only** — never runtime ones: the C# deliverable depends
only on the committed JSON.

## Regenerate

```bash
python -m venv .venv-oracles
. .venv-oracles/bin/activate          # Windows: .venv-oracles\Scripts\activate
pip install -r tools/requirements.txt
python tools/generate_oracles.py
```

The script is **deterministic** (fixed seed, no timestamps): regenerating on
another machine produces an identical file — diffs stay readable and reviewable.
Committing the regenerated JSON is part of the change.

## `draw_icon.py`

Draws `assets/icon.png`, the icon every published package embeds. Run by hand and
rarely — the output is committed, so a fresh clone needs nothing:

```bash
python tools/draw_icon.py
```

**Pillow is its only dependency, and it is deliberately not in
`requirements.txt`.** That file feeds a hash-pinned lock every CI job installs,
and no job draws an icon; adding it there would make three workflows carry an
image library for nothing. Install it ad hoc when you need to redraw:
`pip install Pillow`.

The star is drawn at eight times the final size and downsampled, because its
points are thin and a 128-pixel canvas has no room for a jagged edge. Check the
result at **32 pixels** as well as at 128 — that is the size the package list
shows, and a mark that dissolves there is the wrong mark.

## `fetch_stopwords.py`

Regenerates `src/Lodestar.Text/Vectorization/StopWords.Snowball.cs` from the
Snowball stop-word lists (BSD-3-Clause). No third-party package needed — the
standard library is enough:

```bash
python tools/fetch_stopwords.py            # regenerate
python tools/fetch_stopwords.py --check    # verify the checked-in file is current
```

Each file is checked against a pinned SHA-256 before use. A mismatch means
Snowball edited the list upstream: read the diff, update the pin, adjust the
counts in `StopWordsTests`, and record it — do not regenerate quietly. The nltk
stop-word corpus is **not** a permitted source here, whatever its convenience:
see [`../docs/decisions/0010-stop-word-list-provenance.md`](../docs/decisions/0010-stop-word-list-provenance.md).

## `fetch_xlmr_vocab.py`

Rebuilds `tests/oracles/xlmr_fairseq.model`, the fixture behind the XLM-R oracle:

```bash
python tools/fetch_xlmr_vocab.py            # rebuild
python tools/fetch_xlmr_vocab.py --check    # verify the checked-in fixture
```

It downloads `xlm-roberta-base`'s SentencePiece vocabulary (MIT — vocabulary
only, never weights), checks it against a pinned SHA-256, and **re-emits** it:
same 250 000 pieces, scores and types, at the ids HuggingFace gives them
(`<s>`=0, `<pad>`=1, `</s>`=2, `<unk>`=3, `<mask>`=250001). The relabelling is
the point — the stock file is laid out `<unk>`=0/`<s>`=1/`</s>`=2, which is the
one layout the old id-based control filter got right. The `normalizer_spec` is
copied across untouched, `nmt_nfkc` and its character map included; it was
overwritten with `identity` until #75 made the map readable. See
[`../docs/decisions/0014-precompiled-normalizer.md`](../docs/decisions/0014-precompiled-normalizer.md).

Like `tiny_sp.model`, the result is an *input* to `generate_oracles.py`, not one
of its outputs: it is committed, and the `Oracles are reproducible` job replays
it without touching the network. Rebuilding it is a deliberate act, and a
changed pin means the ids in `xlmr_fairseq.json` move with it.

## `build_normalizer_fixtures.py`

Trains the two small models the normalizer oracle needs beyond the XLM-R one:

```bash
python tools/build_normalizer_fixtures.py            # rebuild
python tools/build_normalizer_fixtures.py --check    # verify the checked-in fixtures
```

`nmt_nfkc_cf.model` carries the case-folding map; `custom_norm.model` carries a
map compiled from three hand-written rules and calls its normalizer nothing more
than `user_defined`. The second is the one that keeps the claim honest: what
`PrecompiledNormalizer` interprets is *a* character map, not the one map every
stock model happens to share. Both are committed inputs to
`generate_oracles.py`, like `tiny_sp.model` — CI never retrains them, and
training is not guaranteed reproducible across `sentencepiece` versions.

## `build_tiny_models.py`

Builds fixtures too small to fit the pipeline the other scripts share: two
ONNX graphs (`tiny_encoder.onnx`, `tiny_embedder.onnx`), a trained
character-level BPE (`tiny_bpe.json`), a hand-constructed BPE holding one
orphaned vocabulary entry (`orphan_bpe_model.json`), and a hand-constructed
BPE carrying `roberta-base`'s own `added_tokens` table
(`roberta_shaped_model.json`) — five fixtures, all committed rather than
rebuilt by CI. See the module docstring for what each one proves.

`tiny_bpe.json`'s trainer is not byte-reproducible across runs: tokens and
merges that tie in frequency land at different ids each time, because the
Rust `HashMap` behind the frequency counts seeds its hash randomly per
process. So, like `tiny_sp.model`, the committed file is authoritative and
rebuilding it is a deliberate act — a diff there is expected, and must not be
committed without regenerating `bpe.json` in the same commit.

`orphan_bpe_model.json` has no such caveat: its vocabulary and merge table are
stated directly rather than learned, so rebuilding it is byte-reproducible —
running it again and diffing is a legitimate way to confirm nothing changed.

## `check_nuspec_dependencies.py`

Asserts that the `<dependencies>` of every packed `.nupkg` match an expected
table, exactly — an unexpected dependency fails as loudly as a missing one:

```bash
python tools/check_nuspec_dependencies.py artifacts
```

A package's dependency graph is a *build output*, derived from whatever restore
resolved, so nobody writes it down and nothing notices when it drifts. This
script is where it is written down. It matters more since the four packages
version independently: `Lodestar.Fuzzy` reaches `Lodestar.Text` through a
`PackageReference`, and that edge is now the one thing holding the two together.
See [`../docs/decisions/0012-per-package-versioning.md`](../docs/decisions/0012-per-package-versioning.md).

Dependency **ids and version ranges** are both asserted. The range matters as
much as the id here: a `PackageReference` emits the floor from
`src/Directory.Packages.props`, while the `LodestarUseProjectRefs` developer loop
emits `Lodestar.Text`'s own current version. Same id, different number — which is
what lets this check catch a package accidentally built with the escape hatch
left on.

## `check_version_floor.py`

Checks the three places a `Lodestar.Text` version number lives, each for a
different reason, none of which MSBuild relates to the others:

```bash
python tools/check_version_floor.py               # offline: the two rules below
python tools/check_version_floor.py --check-feed  # also: the floor is published
```

- `src/Lodestar.Text/Version.props` — what `Lodestar.Text` *is*.
- `src/Directory.Packages.props` — the *floor* `Lodestar.Fuzzy` requires of it.
- `check_nuspec_dependencies.py` — the floor that check asserts actually shipped.

The floor must not exceed the declared version, and must already be on nuget.org
— that is what makes `git clone && dotnet build` work with no pack step. A floor
naming an unpublished version still builds for whoever raised it, whose cache is
warm, and fails for everyone else. `--check-feed` is what turns that into a CI
failure rather than a contributor's bug report.

## `generate_sonar_globalconfig.py`

Writes the root `.globalconfig`: the rules SonarCloud's quality profile activates
for this project and that `SonarAnalyzer.CSharp` ships disabled, raised to
`warning` so `dotnet build` enforces them instead of the quality gate three
minutes after a push. This is the file [`../CONTRIBUTING.md`](../CONTRIBUTING.md#analyzers)
points at, and where a contributor lands after CI prints a diff at them on the
**`Sonar globalconfig is current`** job.

It needs two inputs: one anonymous SonarCloud call it makes itself, and a SARIF
v2 error log from a local build, which only the build can produce. Both the
error log's path and the `.globalconfig` it writes are fixed constants in the
script, not command-line arguments — the tool takes no path or URL of any kind
(issue #131), so there is nothing to pass beyond `--check`. Run from the
repository root, so `$(pwd)` is `ROOT`:

```bash
mkdir -p obj
dotnet build src/Lodestar.Fuzzy -c Release --no-incremental -f net10.0 \
  -p:ErrorLog=$(pwd)/obj/sonar-rules.sarif%2Cversion=2
python tools/generate_sonar_globalconfig.py
```

The path handed to `-p:ErrorLog` must be absolute. MSBuild resolves a relative one
against the project directory (`src/Lodestar.Fuzzy`), not the shell's current
directory, so a relative `obj/sonar-rules.sarif` here would write to
`src/Lodestar.Fuzzy/obj/` and the script — which always looks under the
repository's own `obj/` — would report the input missing.

The `%2C` is load-bearing, not decorative — it is the URL-encoded comma MSBuild
needs between the SARIF path and `version=2`. A bare comma there is parsed as two
separate properties, and `ErrorLog` falls back to its default SARIF **v1**, which
carries no `defaultConfiguration.enabled` flag at all. `disabled_rules()` would
then read an empty rule table and the generated file would enforce nothing, with
no error to say why. `obj/sonar-rules.sarif` is where the script always looks —
`obj/` is already git-ignored, so the error log never becomes something to commit
or clean up by hand.

`--check` compares the regenerated file against the committed one instead of
writing it — no `.globalconfig` in the tree is touched. This is what the `Lint`
job runs on every pull request:

```bash
python tools/generate_sonar_globalconfig.py --check
```

A regenerated file that differs means the SonarCloud profile moved; an
unreachable API is reported separately and never as drift, so a network hiccup
cannot make the check pass on a stale file.

## `extract_doc_snippets.py`

Turns every ` ```csharp ` fence in `README.md`, `docs/guides/*.md` and
`docs/reference/*/*.md` into one method in a generated, git-ignored project
under `samples/Lodestar.DocSnippets/Generated/` — the guides stay the single
source of truth, so a snippet and its compiled counterpart cannot drift:

```bash
python3 tools/extract_doc_snippets.py            # regenerate
python3 tools/extract_doc_snippets.py --check    # report without writing
```

A fence that cannot compile opts out with `<!-- docs-compile: skip - reason
a reviewer can disagree with -->` on the line above it — CONTRIBUTING.md's
[*Definition of done*](../CONTRIBUTING.md#definition-of-done), item 5, has
the exact syntax. A marker with no fence after it is an error, not silence:
an opt-out that stopped applying must not go unnoticed.

Pages under `docs/reference/` carry three more markers, and land in the
`Lodestar.DocSnippets.Reference` namespace instead of `Lodestar.DocSnippets`:

- `<!-- docs-declaration -->` marks a signature shown above a fence — the
  declaration itself, excluded from compilation entirely.
- `<!-- docs-run: skip - reason -->` compiles the fence but never runs it,
  under the same "a reviewer can disagree with the reason" rule as
  `docs-compile: skip`.
- a trailing `// =>` comment on a local-variable declaration becomes an
  assertion on the value the declaration promises, so a reference example is
  not only compiled but *executed* — a promised result that stops being true
  fails CI instead of a reader.

## `build_wiki.py`

Turns a checkout into the tree the GitHub wiki publishes: `docs/` becomes a
flat page per package channel and, once a package is tagged, per released
version — flat because a wiki addresses a page by file name alone, with no
directory context of its own, and two pages sharing a base name would
otherwise collide silently.

```bash
python3 tools/build_wiki.py --repo <dir> --out <dir> \
  --released Lodestar.Text=0.3.0 [--released ...]     # every live channel
python3 tools/build_wiki.py --repo <dir> --out <dir> \
  --archive Lodestar.Text=0.4.0                       # one frozen version
```

`docs/wiki-map.json` is the only place that says which page belongs to which
package; a page it declares that the tree does not hold is an error, not
silence — that is how a renamed guide stops being published without anyone
noticing. Without `--archive` the run refreshes every live channel plus the
root pages (`Home`, `_Sidebar`, and the pages whose subject is the project
rather than a package); with it, the run freezes that one package's snapshot
first and then refreshes the live channels on top of it — only that package is
frozen, which is what lets a per-package release tag publish one version, but
the sidebar and the live pages' banner are read off the tree, so they are
rewritten too rather than left naming the previous release.
`.github/workflows/wiki.yml` is what calls it, on pushes to `main` and on
per-package release tags.

Each live channel also gets a generated entry page, `<channel>.md` — a link
per namespace it covers and a link per guide it ships, read off
`docs/wiki-map.json`'s `covered` map so it cannot go stale — and `Home` links
each package to that page rather than to a bare channel name that used to
resolve to nothing. A package that covers no namespace and ships no guide,
`Lodestar.Metrics` today, gets no entry page: one linking nothing would not be
navigation, so `Home` names it "no pages yet" instead, the same text it
already used before this page existed. The entry page is not archived — it
carries no content of its own to freeze, and the reasoning is in the module
docstring.

## `check_comment_length.py`

Refuses a comment block that runs past its budget without saying why —
**two lines** for an inline comment, **eight** for XML documentation, counted
over prose. `CONTRIBUTING.md`'s
[*Claims in comments*](../CONTRIBUTING.md#claims-in-comments) has why the two
budgets differ.

```bash
python3 tools/check_comment_length.py           # findings, exit 1 if any
python3 tools/check_comment_length.py --report  # counts only, always exit 0
python3 tools/check_comment_length.py --help
```

CI runs it in the `Lint` job and on Windows, so a finding fails the build. The `--report` line prints beside it, which is how the marker count stays visible without failing on its own growth.

Longer stays possible where it is necessary. A block past its budget carries
`long-comment:` and a reason as its first line; an empty marker is refused. The
guard sees only that a marker exists — whether the block deserved one is a
code review's call, per `CONTRIBUTING.md`'s *Claims in comments*.

A docstring is not a comment block. Python prose belongs in one, and the tools
in this directory open with thirty-line docstrings on purpose.

## `check_sample_culture.py`

Refuses a sample that can print a number in whoever ran it's culture. The sample
is the packaging gate: CI runs it on every pull request and a contributor reads
its output to see that a package works. String interpolation formats through
`CurrentCulture`, so the same commit printed `0.807` on CI and `0,807` on a
French console for two releases, and nothing failed — the gate checks that every
public type is reachable, not what the run said.

`CA1305` cannot catch it. The rule fires on an explicit `ToString(string)` and
never on an interpolated hole, at any `AnalysisMode`, so the gap is in the rule
rather than in the configuration and raising `AnalysisLevel` would not surface
one of them. [`decisions/0019`](../docs/decisions/0019-the-net-analysers-run-in-the-build-too.md)
recorded that and left it open; [#205](https://github.com/CyrilB1531/lodestar/issues/205)
closed it.

```bash
python3 tools/check_sample_culture.py
```

Two checks, because neither covers the other:

1. **No interpolated hole carries a standard format specifier.** `{value:F3}` and
   friends are the rewritable ones, and `Inv.F3(value)` —
   `samples/Lodestar.Sample/Inv.cs` — is what they become. An aligned hole
   (`{value,10:F3}`) is reported as such, because a rewrite has to keep the
   alignment rather than swallow it.

   A hole is found by its closing `:F3}` and then walked *backwards* to its
   opening brace, because the expression can hold braces of its own — an object
   initializer in an argument list does, and three such holes survived the first
   sweep of this issue precisely because a single regular expression cannot span
   them. The specifier must be a letter followed by digits: the sample embeds
   vocabularies as JSON, where `:10}` is data rather than a format. A *custom*
   format (`{value:0.###}`) is therefore not matched, and is left to check 2.
2. **`Program.cs` still pins the thread culture.** That covers what no syntactic
   scan can: a bare `{value}` hole whose expression is a `double` reads exactly
   like a bare `{count}` hole whose expression is an `int`. Matched on the
   assignment rather than the comment above it, so rewording the comment does not
   fail the build.

Sources come from `git ls-files`, never a glob: `bin/` and `obj/` hold copies of
every sample source, and editing those would turn the guard green while the files
that ship still printed in the contributor's culture.

Exit codes:

- `0` — clean.
- `1` — findings printed, each naming its file, line and hole.
- `2` — bad usage, or no tracked sample sources to scan.

## `check_machine_paths.py`

Refuses a tracked file that holds a path under someone's home directory. Ten of
them reached this public repository across six documents before anything looked
for them, and both sweeps that removed them started from a reader noticing a
line rather than from a check. They arrive by being pasted from a terminal,
which is exactly when nobody is thinking about what the string contains.

```bash
python3 tools/check_machine_paths.py                # named shapes, plus this machine's $HOME
python3 tools/check_machine_paths.py --no-environment  # named shapes only
python3 tools/check_machine_paths.py --help
```

Two probe sets. **Named shapes** run everywhere: a home directory under `/home`
or `/Users`, its Windows equivalent, the root user's own home, and the session
scratch-directory prefix. **Environment-derived probes** are computed at run
time from `$HOME` — the path itself, the account name bounded by a separator or
a dash, and the dashed form a session scratch directory is named after — which
catch shapes no fixed list enumerates, on the machine where a path is actually
created.

An ordinary account name (`src`, `build`, `net` and the like) can turn a derived
probe into noise on an otherwise unrelated line. `--no-environment` drops that
set and leaves the named shapes enforcing, which have no such escape by design:
a path under a home directory is never wanted in a committed file. The report
names which probe matched each finding, and mentions `--no-environment` only
when a derived probe is the one that fired.

Exit codes:

- `0` — clean.
- `1` — findings printed (with a suggestion — `$SCRATCH`, `$(mktemp -d)`, or a
  description of what the path held).
- `2` — bad usage.

It exempts only its own source and its own test module, which have to contain
the patterns they search for to exist; nothing else is, because an exemption
list that grows is a guard being switched off one file at a time. See
[`../docs/superpowers/specs/2026-08-12_0133_machine-path-guard.md`](../docs/superpowers/specs/2026-08-12_0133_machine-path-guard.md)
for the measurement that shaped the two-probe-set design.

## Rules

- **Code-point semantics.** rapidfuzz/jellyfish iterate over code points; the C#
  suite therefore replays with `TextElement.CodePoint`. No lone surrogate is
  emitted (it would not survive the JSON round-trip).
- **Provenance.** We *run* these libraries to generate data — which creates no
  right over the outputs — but we do not **transcribe** any code. `python-
  Levenshtein` (GPL) is excluded even from generation, for hygiene. See
  [`../docs/decisions/0003-provenance-and-licensing.md`](../docs/decisions/0003-provenance-and-licensing.md).
