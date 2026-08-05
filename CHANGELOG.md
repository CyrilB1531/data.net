# Changelog

All notable changes to this project are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and
this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

The three packages (`DataNet.Text`, `DataNet.Embeddings`, `DataNet.Fuzzy`) version
and release **independently**, each from its own `src/<Package>/Version.props`, so
entries are grouped per package. Releases up to and including `0.2.0` predate the
split and covered all three at once — see
[`docs/decisions/0012`](docs/decisions/0012-per-package-versioning.md).

## [Unreleased]

### DataNet.Text — 0.3.0

#### Added

- **Stop-word lists for French, German, Italian, Portuguese and Spanish** —
  `StopWords.French` and friends, one per language that already has a Snowball
  stemmer. They are Snowball's lists (BSD-3-Clause), vendored by
  `tools/fetch_stopwords.py` against a pinned SHA-256, and attributed in `NOTICE`.
  The nltk corpus is deliberately not used: `nltk_data` classifies it as having
  no stated licence, so it cannot be redistributed. That makes these lists the
  one place where the library knowingly diverges from nltk — the gap is measured
  per language in [`docs/equivalence.md`](docs/equivalence.md) and the reasoning
  is in [`docs/decisions/0010`](docs/decisions/0010-stop-word-list-provenance.md).
  `StopWords.English` is unchanged, still scikit-learn's 318-word list.

### DataNet.Fuzzy — 0.3.0

#### Changed

- **Depends on `DataNet.Text` as a published NuGet package** rather than as a
  project reference. Nothing changes for consumers: a project reference between
  two packable projects already produced exactly this `<dependency>`, and
  `Fuzz.Ratio` is still `Indel.NormalizedSimilarity × 100`. What changes is that
  the build graph now matches the release graph, so a package can now ship
  without dragging the other two with it — as `DataNet.Embeddings` demonstrates
  by staying at `0.2.0` through this release. The dependency floor is
  pinned in `src/Directory.Packages.props`; the developer loop for editing both
  libraries at once is documented in
  [`CONTRIBUTING.md`](CONTRIBUTING.md#working-across-two-packages), and the whole
  decision in
  [`docs/decisions/0012`](docs/decisions/0012-per-package-versioning.md).

## [0.2.0] — 2026-08-05

Reach, correctness and honesty about performance. Nothing in the public API was
removed or renamed, so upgrading from `0.1.0` is a version bump.

### Added

- **`netstandard2.0` as a second target framework.** The packages now also run on
  .NET Framework 4.6.1+, Mono, Xamarin and Unity. One package carries both
  frameworks. The net10 fast paths are unchanged — netstandard2.0 reaches
  equivalent behavior through conditional compilation, never a reduced API.
  See [`docs/decisions/0001`](docs/decisions/0001-target-framework.md).
- **Four Snowball stemmers**: `SpanishSnowballStemmer`,
  `PortugueseSnowballStemmer`, `ItalianSnowballStemmer`, `GermanSnowballStemmer`.
  With the existing English and French, that is 758 frozen reference words
  replayed against `nltk`.
- **Blocked (multi-word) Myers** for `Levenshtein.Distance`, removing the
  64-character cap on the bit-parallel path.
- A benchmark suite comparing the `net10.0` and `netstandard2.0` builds of the
  same library — see [`bench/README.md`](bench/README.md).
- Mirror test projects that replay the entire suite against the `netstandard2.0`
  assemblies, so the build shipped to .NET Framework, Mono and Unity consumers is
  executed rather than only compiled. 339 tests across both builds.
- A sample under [`samples/`](samples/DataNet.Sample) that consumes the packages
  by `PackageReference` from a locally packed feed, and runs in CI. Nothing
  previously exercised the packaging, so a defect in it would have reached
  consumers before CI. See
  [`docs/decisions/0009`](docs/decisions/0009-sample-consumes-a-local-feed.md).
- [`CONTRIBUTING.md`](CONTRIBUTING.md) and this changelog.
- SonarQube Cloud analysis, a `lint` CI job (markdownlint and `dotnet format`),
  and Dependabot for GitHub Actions.

### Changed

- **Long-string `Levenshtein.Distance` is 20–33× faster.** At 512 characters,
  684 µs → 21 µs; at 128, 36 µs → 1.8 µs. Patterns over 64 characters previously
  fell back to the `O(n·m)` DP. The bit-parallel path still requires a Latin-1
  pattern, so CJK and emoji inputs continue to use the DP.
- **Regular expressions are bounded by a match timeout.** `TextAnalyzer` accepts a
  caller-supplied pattern and runs it over caller-supplied text, so catastrophic
  backtracking was reachable from the public API. A pathological pair now raises
  `RegexMatchTimeoutException` instead of hanging the calling thread. This is the
  one behavioural change in the release: input that previously hung will now throw.
- Warnings are errors across the whole repository, covering `src`, `tests` and
  `bench` rather than the libraries alone.

### Fixed

- Static-analysis defects, each verified against the oracle corpora: an `int`
  division result widened to `double` in `Jaro`; nested classes shadowing their
  outer type in the Snowball stemmers; step methods returning a value no caller
  read; nested ternaries in `Nysiis`, `EnglishSnowballStemmer` and
  `HashingVectorizer`.
- **Code coverage was never collected.** CI passed `--collect:"XPlat Code Coverage"`
  with no `coverlet.collector` package referenced, so the collector was absent and
  the step silently did nothing.

### Security

- **Script injection in the release workflows.** A `workflow_dispatch` input was
  interpolated directly into a shell command, in a job holding `id-token: write`
  that can mint a nuget.org publishing key. Values now reach the shell through the
  environment.
- GitHub Actions pinned to full commit SHAs, so a moved tag cannot change what
  runs in CI.
- CI dependency installation hardened: markdownlint pinned with lifecycle scripts
  disabled, `pip install --only-binary :all: --require-hashes` against a generated
  lock file that pins all 29 packages — the transitive graph included, since the
  oracle corpora are those libraries' output.

### Documentation

- Package metadata now attributes the project to Cyril BRUNET (`Authors`,
  `Company`, `Copyright`), and `NOTICE` and `LICENSE` no longer carry the
  project's former name.
- `THIRD-PARTY-NOTICES.md` records the shipped dependencies. It previously said
  "None yet", which stopped being true once `DataNet.Embeddings` took ONNX
  Runtime and the `netstandard2.0` target added `System.Memory` and
  `System.Numerics.Vectors`. The development-only table was likewise missing
  `nltk`, `tokenizers`, `sentencepiece` and `numpy`.

### Notes

- Deliberate analyzer suppressions live in the source as `#pragma warning disable`
  with their justification. SonarLint reads neither `.editorconfig` nor a workspace
  `.vscode/settings.json`, so those do not work.
- The `netstandard2.0` build is behavior-verified: the whole suite is replayed
  against those assemblies, not only against the `net10.0` ones.

## [0.1.0] — 2026-08-01

First release. All four lots of the project brief are delivered, and every
building block is validated by replaying frozen reference outputs captured from
the canonical Python libraries — see [`docs/equivalence.md`](docs/equivalence.md).

### Added

- **Lot 1 — string distances and similarity** (`DataNet.Text`): Levenshtein
  (with a Myers bit-parallel fast path), OSA, Damerau-Levenshtein, Hamming,
  Jaro, Jaro-Winkler, Indel, LCS, Ratcliff-Obershelp, Jaccard, Dice, Overlap,
  Tversky, Cosine, Soundex, Metaphone, NYSIIS.
- **Lot 2 — tokenization and sparse vectorization** (`DataNet.Text`): CSR
  matrix, word/char/char_wb tokenizers, `CountVectorizer`, `TfidfVectorizer`,
  `HashingVectorizer` (MurmurHash3-32), Porter and Snowball EN/FR stemmers,
  English stop words.
- **Lot 3 — embeddings and semantic search** (`DataNet.Embeddings`): WordPiece
  and SentencePiece (unigram Viterbi) tokenizers, pooling, SIMD kNN, ONNX
  inference — with ONNX Runtime isolated to this package.
- **Lot 4 — applied fuzzy matching** (`DataNet.Fuzzy`): `fuzz.*`
  (ratio / partial / token_sort / token_set / WRatio), `process.extract` and
  `extractOne`, blocking deduplication.
- Migration guides for NumPy, pandas, scikit-learn, statsmodels, PyTorch,
  matplotlib and seaborn, plus the three-column inventory that maps each need to
  use / build / decide — [`docs/migration/`](docs/migration/README.md).
- A decision log recording the deliberate divergences from the Python
  references — [`docs/decisions/`](docs/decisions/).
- Publishing to nuget.org via Trusted Publishing (keyless, OIDC) and to GitHub
  Packages.

[Unreleased]: https://github.com/CyrilB1531/data.net/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/CyrilB1531/data.net/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/CyrilB1531/data.net/releases/tag/v0.1.0
