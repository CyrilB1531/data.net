# DataNet

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=CyrilB1531_data.net&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=CyrilB1531_data.net)

A **data-science toolkit for C#/.NET**, built on an honest premise:

> Don't rewrite Python. Use the .NET ecosystem where it's strong, and write native
> code only where .NET has a real gap: **text** (similarity, vectorization,
> semantic search). All of it **with no Python at runtime**.

## Why

Python dominates data analysis through its ecosystem and its exploratory notebook
workflow, not through the language itself. Its performance comes from C/Fortran
kernels.

C# brings static typing, real parallelism without a global interpreter
lock, safe refactoring, and simple deployment. The only objective reason to stay
on Python for this domain was the lack of an equivalent .NET library. DataNet
removes that reason.

## Two deliverables

1. **Native code** where there's a gap → the packages below (string distances,
   vectorization, embeddings, fuzzy matching) — allocation-lean, `Span`-based,
   SIMD, zero external dependencies in the core.
2. **Migration guides** for people coming from Python → [`docs/migration/`](docs/migration/README.md),
   which, for each need (NumPy, pandas, scikit-learn, statsmodels, PyTorch,
   matplotlib, seaborn), points to the right .NET building block and the pitfalls.

See the [**four-column migration inventory**](docs/migration/README.md): it's the
project map (use / build / decide).

> Targets: **.NET 10** (`net10.0`, all fast paths) and **.NET Standard 2.0**
> (broad reach — also .NET Framework 4.6.1+, Mono, Xamarin, Unity). A single
> package carries both. See [`docs/decisions/0001`](docs/decisions/0001-target-framework.md).

## Status — every lot of the project brief is delivered

| Lot | Contents | Status |
| --- | --- | --- |
| 1 | String distances & similarity | ✅ **complete** — Levenshtein (+ Myers), OSA, Damerau-Levenshtein, Hamming, Jaro, Jaro-Winkler, Indel, LCS, Ratcliff-Obershelp, Jaccard, Dice, Overlap, Tversky, Cosine, Soundex, Metaphone, NYSIIS |
| 2 | Tokenization & sparse vectorization | ✅ **complete** — CSR, tokenizers (word/char/char_wb), CountVectorizer, TfidfVectorizer, HashingVectorizer, Porter, Snowball EN/FR/DE/ES/IT/PT, stop words in six languages |
| 3 | Embeddings & semantic search | ✅ **complete** — WordPiece, SentencePiece, BPE and byte-level BPE (GPT-2, Llama-3, Qwen2), pooling, SIMD kNN, ONNX inference |
| 4 | Applied fuzzy matching | ✅ **complete** — `fuzz.*` (ratio/partial/token_sort/token_set/WRatio), `process.extract`/`extractOne`, blocking deduplication |
| 5 | Classification metrics | ✅ **complete** — confusion matrix, accuracy, precision/recall/F1/F-beta in all four averaging modes, `classification_report` character for character, ROC-AUC binary and multiclass (`ovr`/`ovo`) |

Every building block is oracle-validated against rapidfuzz / jellyfish /
textdistance / difflib / scikit-learn / nltk / HuggingFace tokenizers /
sentencepiece / numpy / ONNX Runtime (see [`docs/equivalence.md`](docs/equivalence.md)).

## Getting started

```bash
dotnet add package DataNet.Text
```

```csharp
using DataNet.Text.Distances;

Levenshtein.Distance("kitten", "sitting");             // 3
Levenshtein.NormalizedSimilarity("kitten", "sitting"); // 0.5714…
```

A runnable version of the above, consuming the packages exactly as you would:

```bash
for p in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy src/DataNet.Metrics; do
  dotnet pack "$p" -c Release -o ./artifacts
done
dotnet run --project samples/DataNet.Sample -c Release
```

Full guide: [`docs/guides/quickstart.md`](docs/guides/quickstart.md). See also the
[vectorization](docs/guides/vectorization.md), [embeddings](docs/guides/embeddings.md)
and [fuzzy-matching](docs/guides/migrating-from-rapidfuzz.md) guides.

Function by function, the reference pages under
[`docs/reference/`](docs/reference/text/distances.md) say what each member is
for, when to prefer it to its neighbour and what the trap is — start with
[the distances](docs/reference/text/distances.md). The same pages are published
to [the wiki](https://github.com/CyrilB1531/data.net/wiki), where each package's
channel follows `main` and every release is archived under its own version.

## Developing

```bash
dotnet build                                   # build the solution
dotnet test                                    # replay oracles + property tests
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*Levenshtein*'
```

The project follows **GitHub flow**: `main` is always releasable, and every change
arrives through a short-lived branch and a pull request. Branch conventions, the
definition of done, the oracle-validation procedure and the analyzer-suppression
policy are in [`CONTRIBUTING.md`](CONTRIBUTING.md); release history is in
[`CHANGELOG.md`](CHANGELOG.md).

### Oracle validation

Conformance to Python behavior is **proven**, not assumed (§4 of the brief).
`tools/generate_oracles.py` freezes a few thousand reference cases from
rapidfuzz/jellyfish/etc. into `tests/oracles/*.json` (versioned). The C# suite
replays them with a `1e-9` tolerance. Python is a development-only dependency. See
[`tools/README.md`](tools/README.md).

## Structure

```text
DataNet.slnx
├── src/DataNet.Text/            distances, similarity, tokenizers, vectorizers, stemmers (no dependencies)
├── src/DataNet.Embeddings/      sub-word tokenizers, pooling, SIMD kNN, ONNX inference (ONNX Runtime isolated here)
├── src/DataNet.Fuzzy/           fuzz.*, process.extract, deduplication
├── src/DataNet.Metrics/         confusion matrix, precision/recall/F1, report, ROC-AUC
├── tests/                       xUnit: oracles + properties (one project per module)
├── tests/oracles/               frozen JSON corpora (generated from Python) + a synthetic ONNX model
├── bench/DataNet.Text.Benchmarks/  BenchmarkDotNet
├── tools/generate_oracles.py    reference generation
├── Directory.Build.props        (root); src|tests/Directory.Packages.props (central package management)
├── src/*/Version.props          one version per publishable package (decision 0012)
├── docs/                        guides, equivalence table, decision log
├── docs/reference/<package>/    one reference entry per exported type and public method
└── docs/wiki-map.json           which page ships with which package, and which namespaces the reference gate enforces
```

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

## Publishing

Four NuGet packages are produced: `DataNet.Text`, `DataNet.Embeddings`,
`DataNet.Fuzzy`, `DataNet.Metrics`. **Each versions and releases on its own**: shared metadata
(license, README, repository) lives in `Directory.Build.props`, while the version
is declared per project in `src/<Package>/Version.props`. `DataNet.Fuzzy` depends
on `DataNet.Text` as a published package, not as a project reference — see
[`docs/decisions/0012`](docs/decisions/0012-per-package-versioning.md).

**GitHub Packages** (no nuget.org account needed — uses GitHub's automatic token).
Bump the version, then tag it with the package name. The
[`release`](.github/workflows/release.yml) workflow packs and publishes that
package alone:

```bash
# 1. edit src/DataNet.Fuzzy/Version.props, commit, merge to main
# 2. tag the released version — <PackageId>/v<Version>
git tag DataNet.Fuzzy/v0.3.0
git push origin DataNet.Fuzzy/v0.3.0
```

The tag does not set the version; it names which declared version to release. The
workflow refuses the job if the tag and `Version.props` disagree. Repository-wide
`v*` tags are retired — there is no single version left for one to designate.

**Step 1 is not optional.** Because the tag only confirms the declared version,
tagging without bumping first is a tag that agrees with `Version.props` and names
a version the feed already has. The push is then rejected rather than absorbed.
The workflows do not pass `--skip-duplicate`, which used to report that case as a
successful release that shipped nothing. Keeping a declared version off the feed
is also checked directly in CI by `tools/check_version_floor.py`.

To consume them, add a source pointing at the owner's feed (with a GitHub token
that has `read:packages`):

```bash
dotnet nuget add source "https://nuget.pkg.github.com/CyrilB1531/index.json" \
  --name github --username CyrilB1531 --password <GITHUB_TOKEN>
dotnet add package DataNet.Text
```

**nuget.org** uses Trusted Publishing (OIDC, no stored key): run the
[`Publish to nuget.org`](.github/workflows/release-nuget-org.yml) workflow from
the Actions tab, choosing the package and confirming its version. By hand, with
an API key, one package at a time:

```bash
dotnet pack src/DataNet.Text -c Release -o artifacts
dotnet nuget push "artifacts/DataNet.Text.*.nupkg" \
  --source https://api.nuget.org/v3/index.json --api-key <KEY>
```

## License

[Apache-2.0](LICENSE). See [`NOTICE`](NOTICE) and
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) for attributions. The license
choice and the code-provenance rule are documented in
[`docs/decisions/0003-provenance-and-licensing.md`](docs/decisions/0003-provenance-and-licensing.md).

_This repository is not legal advice._
