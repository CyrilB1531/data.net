# DataNet

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=CyrilB1531_data.net&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=CyrilB1531_data.net)

A **data-science toolkit for C#/.NET**, built on an honest premise:

> Don't rewrite Python. Use the .NET ecosystem where it's strong, and write native
> code only where .NET has a real gap: **text** (similarity, vectorization,
> semantic search). All of it **with no Python at runtime**.

## Why

Python dominates data analysis through its ecosystem and its exploratory notebook
workflow — not through the language itself; its performance comes from C/Fortran
kernels. C# brings static typing, real parallelism without a global interpreter
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

See the [**three-column migration inventory**](docs/migration/README.md): it's the
project map (use / build / decide).

> Targets: **.NET 10** (`net10.0`, all fast paths) and **.NET Standard 2.0**
> (broad reach — also .NET Framework 4.6.1+, Mono, Xamarin, Unity). A single
> package carries both. See [`docs/decisions/0001`](docs/decisions/0001-target-framework.md).

## Status — every lot of the project brief is delivered

| Lot | Contents | Status |
| --- | --- | --- |
| 1 | String distances & similarity | ✅ **complete** — Levenshtein (+ Myers), OSA, Damerau-Levenshtein, Hamming, Jaro, Jaro-Winkler, Indel, LCS, Ratcliff-Obershelp, Jaccard, Dice, Overlap, Tversky, Cosine, Soundex, Metaphone, NYSIIS |
| 2 | Tokenization & sparse vectorization | ✅ **complete** — CSR, tokenizers (word/char/char_wb), CountVectorizer, TfidfVectorizer, HashingVectorizer, Porter, Snowball EN/FR/DE/ES/IT/PT, stop words in six languages |
| 3 | Embeddings & semantic search | ✅ **complete** — WordPiece, SentencePiece, pooling, SIMD kNN, ONNX inference |
| 4 | Applied fuzzy matching | ✅ **complete** — `fuzz.*` (ratio/partial/token_sort/token_set/WRatio), `process.extract`/`extractOne`, blocking deduplication |

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
for p in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy; do
  dotnet pack "$p" -c Release -o ./artifacts
done
dotnet run --project samples/DataNet.Sample -c Release
```

Full guide: [`docs/guides/quickstart.md`](docs/guides/quickstart.md). See also the
[vectorization](docs/guides/vectorization.md), [embeddings](docs/guides/embeddings.md)
and [fuzzy-matching](docs/guides/migrating-from-rapidfuzz.md) guides.

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

Conformance to Python behavior is **proven**, not assumed (§4 of the brief):
`tools/generate_oracles.py` freezes a few thousand reference cases from
rapidfuzz/jellyfish/etc. into `tests/oracles/*.json` (versioned); the C# suite
replays them with a `1e-9` tolerance. Python is a development-only dependency. See
[`tools/README.md`](tools/README.md).

## Structure

```text
DataNet.slnx
├── src/DataNet.Text/            distances, metrics, tokenizers, vectorizers, stemmers (no dependencies)
├── src/DataNet.Embeddings/      sub-word tokenizers, pooling, SIMD kNN, ONNX inference (ONNX Runtime isolated here)
├── src/DataNet.Fuzzy/           fuzz.*, process.extract, deduplication
├── tests/                       xUnit: oracles + properties (one project per module)
├── tests/oracles/               frozen JSON corpora (generated from Python) + a synthetic ONNX model
├── bench/DataNet.Text.Benchmarks/  BenchmarkDotNet
├── tools/generate_oracles.py    reference generation
├── Directory.Build.props        (root); src|tests/Directory.Packages.props (central package management)
└── docs/                        guides, equivalence table, decision log
```

## Publishing

Three NuGet packages are produced: `DataNet.Text`, `DataNet.Embeddings`,
`DataNet.Fuzzy`. Package metadata (version, license, README, repository) is shared
in `Directory.Build.props`.

**GitHub Packages** (no nuget.org account needed — uses GitHub's automatic token).
Tag a version and push; the [`release`](.github/workflows/release.yml) workflow
packs and publishes:

```bash
git tag v0.1.0
git push origin v0.1.0
```

To consume them, add a source pointing at the owner's feed (with a GitHub token
that has `read:packages`):

```bash
dotnet nuget add source "https://nuget.pkg.github.com/CyrilB1531/index.json" \
  --name github --username CyrilB1531 --password <GITHUB_TOKEN>
dotnet add package DataNet.Text
```

**nuget.org** (optional, needs a free account + API key). Once you have a key:

```bash
dotnet pack src/DataNet.Text -c Release -o artifacts
dotnet nuget push "artifacts/*.nupkg" --source https://api.nuget.org/v3/index.json --api-key <KEY>
```

## License

[Apache-2.0](LICENSE). See [`NOTICE`](NOTICE) and
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) for attributions. The license
choice and the code-provenance rule are documented in
[`docs/decisions/0003-provenance-and-licensing.md`](docs/decisions/0003-provenance-and-licensing.md).

_This repository is not legal advice._
