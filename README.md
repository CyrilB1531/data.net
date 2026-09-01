# Lodestar

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
on Python for this domain was the lack of an equivalent .NET library. Lodestar
removes that reason.

## What has no .NET equivalent

The claim this project is judged on, strongest first.

1. **Sparse text vectorization.** `CountVectorizer`, `TfidfVectorizer` and
   `HashingVectorizer` at scikit-learn semantics, over a `CsrMatrix` written here.
   ML.NET's `FeaturizeText` produces a **dense** vector coupled to `IDataView`; for
   the same thousand documents it materializes 81 million floats where the sparse
   matrix stores 39 974 values. There is no third option in .NET.
2. **Framework-free classification metrics.** 54 metric classes against ML.NET's
   six result types — and no `IDataView`, no schema, no pipeline object between the
   caller and a `double`. ML.NET has no call that returns one metric: asking it for
   accuracy costs the whole evaluation bundle.
3. **Loading the tokenizer files people actually have.** `tokenizer.json` —
   normalizer, pre-tokenizer, model, decoder, added tokens — is what Llama-2 and
   Mistral v0.1 ship, and `Microsoft.ML.Tokenizers` has no entry point that reads
   one. Every one of its factories takes a vocabulary, a merges file or a
   `spiece.model`.
4. **Split conformal prediction.** An interval instead of a point, a set instead of
   a class, with a finite-sample coverage guarantee — MAPIE's job, and the survey
   behind [#441](https://github.com/CyrilB1531/lodestar/issues/441) found **no C#
   implementation at all**, maintained or otherwise. The guarantee assumes
   exchangeable calibration and test data, which
   [`docs/guides/conformal.md`](docs/guides/conformal.md#exchangeability) leads with
   rather than footnotes.
5. **Distances, embeddings and fuzzy matching**, bundled for pipeline coherence
   rather than because .NET is empty here — it is not, and the table below says by
   how much.

All of it **with no Python at runtime**, on **.NET 10** and **.NET Standard 2.0**
from a single package (also .NET Framework 4.6.1+, Mono, Xamarin, Unity — see
[`docs/decisions/0001`](docs/decisions/0001-target-framework.md)).

The second deliverable is the **migration guides** for people arriving from Python:
[`docs/migration/`](docs/migration/README.md) points each need (NumPy, pandas,
scikit-learn, statsmodels, PyTorch, matplotlib, seaborn) at the right .NET building
block and its pitfalls, marks the libraries that are no longer maintained with the
dates that prove it, and says when calling Python is still the right answer. Its
[four-column inventory](docs/migration/README.md) is the project map — use, build,
decide.

## Measured against the .NET incumbents

A claim nobody checked is a claim nobody believes, so every package is benchmarked
against the .NET library a reader would otherwise reach for — not only against
Python. Both sides are checked to return **the same answers** before either is
timed; `bench/README.md`'s section 15 has the harness and the agreement checks.

| package | incumbent | how it reads |
| --- | --- | --- |
| `Lodestar.Text` | ML.NET `FeaturizeText` | **Not like-for-like.** Per feature produced the two are within ~11 % — the advantage is the sparse representation, not a faster kernel |
| `Lodestar.Fuzzy` | Fastenshtein, Quickenshtein, F23.StringSimilarity, Raffinert.FuzzySharp | Ahead on Levenshtein at every length, and on all four `fuzz` ratios |
| `Lodestar.Embeddings` | `Microsoft.ML.Tokenizers`, `TensorPrimitives` | **Behind on encoding**, and the gap that justifies the package is the loader above — [decision 0068](docs/decisions/0068-the-tokenizer-gap-is-the-loader-not-the-encode-kernel.md) |
| `Lodestar.Metrics` | ML.NET metrics | Coverage, not speed: the advantage narrows with size and the shape does not |
| `Lodestar.Conformal` | — | **No incumbent exists**, which is the finding rather than a gap in the harness — `bench/README.md` section 15 says what would change that |
| `Lodestar.Decomposition` | ML.NET `ProjectToPrincipalComponents` | **Not like-for-like.** Centred dense PCA against uncentred sparse truncated SVD and a non-negative factorization — three different decompositions, so each side is checked against its own reconstruction error rather than against the other's numbers |

Numbers with the machine that produced them are in
[`docs/guides/performance.md`](docs/guides/performance.md); a shared runner's
absolutes are not comparable and are deliberately not published there.

## Why not just call Python?

[CSnakes](https://github.com/tonybaloney/CSnakes) and
[Python.NET](https://github.com/pythonnet/pythonnet) both work, both are maintained,
and for a model that only exists as a Python package they are the right answer —
[`docs/migration/`](docs/migration/README.md#when-calling-python-is-still-the-right-answer)
says so. What they cost is a Python runtime to deploy and version alongside the
application, no ahead-of-time compilation to a single artifact, and the GIL between
your threads and theirs. Where a .NET library will do, that is a poor trade, and
this project exists to make it an avoidable one.

## What is delivered

The five lots of the original brief are complete, and the repository has since
grown past that framing — [`docs/reference/`](docs/reference/text/distances.md)
documents considerably more than this table lists.

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
dotnet add package Lodestar.Text
```

```csharp
using Lodestar.Text.Distances;

Levenshtein.Distance("kitten", "sitting");             // 3
Levenshtein.NormalizedSimilarity("kitten", "sitting"); // 0.5714…
```

A runnable version of the above, consuming the packages exactly as you would:

```bash
for p in src/Lodestar.Abstractions src/Lodestar.Text src/Lodestar.Embeddings \
         src/Lodestar.Fuzzy src/Lodestar.Metrics src/Lodestar.Conformal \
         src/Lodestar.Decomposition; do
  dotnet pack "$p" -c Release -o ./artifacts
done
NUGET_PACKAGES=$(mktemp -d) dotnet run -c Release --project samples/Lodestar.Sample
```

The isolated `NUGET_PACKAGES` is not decoration: the global packages folder is consulted ahead of
any source, so a machine that has ever restored a published `Lodestar.*` at one of these versions
runs the sample against **that** rather than against what `pack` just produced — see
[`docs/decisions/0009`](docs/decisions/0009-sample-consumes-a-local-feed.md). On PowerShell the
same isolation is two lines, `$env:NUGET_PACKAGES = (New-Item -ItemType Directory -Path (Join-Path $env:TEMP (New-Guid))).FullName`
before the `dotnet run`, and `Remove-Item Env:NUGET_PACKAGES` after it.

Full guide: [`docs/guides/quickstart.md`](docs/guides/quickstart.md). See also the
[vectorization](docs/guides/vectorization.md), [embeddings](docs/guides/embeddings.md),
[fuzzy-matching](docs/guides/migrating-from-rapidfuzz.md),
[decomposition](docs/guides/decomposition.md) and
[metrics](docs/guides/metrics.md) guides — the last one answers _which_ metric to
reach for, which the per-member reference pages deliberately cannot.

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
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- --filter '*Levenshtein*'
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
Lodestar.slnx
├── src/Lodestar.Abstractions/              CsrMatrix and SparseNorm — the sparse primitive the others share (no dependencies)
├── src/Lodestar.Text/                      distances, similarity, tokenizers, vectorizers, stemmers
├── src/Lodestar.Embeddings/                sub-word tokenizers, pooling, SIMD kNN, ONNX inference (ONNX Runtime isolated here)
├── src/Lodestar.Fuzzy/                     fuzz.*, process.extract, deduplication
├── src/Lodestar.Metrics/                   confusion matrix, precision/recall/F1, report, ROC-AUC
├── src/Lodestar.Conformal/                 split conformal intervals and prediction sets (no dependencies)
├── src/Lodestar.Decomposition/             truncated SVD and non-negative matrix factorization over a CsrMatrix
├── tests/                                  xUnit: two projects per package — net10.0, and a mirror linking the same sources against netstandard2.0
├── tests/oracles/                          frozen JSON corpora (generated from Python) + a synthetic ONNX model
├── bench/Lodestar.Text.Benchmarks/         BenchmarkDotNet: every non-netstandard benchmark, whatever package it measures
├── bench/Lodestar.NetStandard.Benchmarks/  the netstandard2.0 assemblies, measured on the same host
├── tools/generate_oracles.py               reference generation
├── Directory.Build.props                   (root); src|tests/Directory.Packages.props (central package management)
├── src/*/Version.props                     one version per publishable package (decision 0012)
├── docs/                                   guides, equivalence table, decision log
├── docs/reference/<package>/               one reference entry per exported type and public method
└── docs/wiki-map.json                      which page ships with which package, and which namespaces the reference gate enforces
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

Seven NuGet packages are produced: `Lodestar.Abstractions`, `Lodestar.Text`,
`Lodestar.Embeddings`, `Lodestar.Fuzzy`, `Lodestar.Metrics`, `Lodestar.Conformal` and
`Lodestar.Decomposition`. **Each versions and releases on its own**: shared metadata
(license, README, repository) lives in `Directory.Build.props`, while the version
is declared per project in `src/<Package>/Version.props`. `Lodestar.Fuzzy` depends
on `Lodestar.Text` as a published package, not as a project reference — see
[`docs/decisions/0012`](docs/decisions/0012-per-package-versioning.md).

**GitHub Packages** (no nuget.org account needed — uses GitHub's automatic token).
Bump the version, then tag it with the package name. The
[`release`](.github/workflows/release.yml) workflow packs and publishes that
package alone:

```bash
# 1. edit src/Lodestar.Fuzzy/Version.props, commit, merge to main
# 2. tag the released version — <PackageId>/v<Version>
git tag Lodestar.Fuzzy/v0.3.0
git push origin Lodestar.Fuzzy/v0.3.0
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
dotnet add package Lodestar.Text
```

**nuget.org** uses Trusted Publishing (OIDC, no stored key): run the
[`Publish to nuget.org`](.github/workflows/release-nuget-org.yml) workflow from
the Actions tab, choosing the package and confirming its version. By hand, with
an API key, one package at a time:

```bash
dotnet pack src/Lodestar.Text -c Release -o artifacts
dotnet nuget push "artifacts/Lodestar.Text.*.nupkg" \
  --source https://api.nuget.org/v3/index.json --api-key <KEY>
```

## License

[Apache-2.0](LICENSE). See [`NOTICE`](NOTICE) and
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) for attributions. The license
choice and the code-provenance rule are documented in
[`docs/decisions/0003-provenance-and-licensing.md`](docs/decisions/0003-provenance-and-licensing.md).

_This repository is not legal advice._
