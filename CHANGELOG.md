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
- **Fitted models survive the process.** `TfidfVectorizer`, `CountVectorizer` and
  `HashingVectorizer` gain `Save`/`Load` over a stream or a path, plus native
  async counterparts. Training on a corpus and scoring later — the normal split
  in any real pipeline — no longer requires reimplementing serialization over
  `GetFeatureNames()` and `Idf`. The round trip is bit-exact: idf weights are
  written as raw IEEE-754 bits, so a reloaded model produces a
  `CsrMatrix` identical element by element, not "within a tolerance".
  `HashingVectorizer` has no vocabulary to learn but its **options** round-trip
  too — a pipeline reloaded with a different `NumFeatures` produces different
  columns for the same document, and nothing downstream would notice.
- **`ArtifactLoadOptions`** bounds what a loaded file may declare — vocabulary
  size, token length, JSON depth, total bytes, array length. A malformed or
  hostile artifact raises `InvalidDataException` naming the limit and the value,
  never `OutOfMemoryException`. Unlike `pickle.load`, the format reads data and
  never code.

#### Changed

- **The idf vector is stored as base64, not as JSON numbers.** It was the most
  expensive thing in the file — parsing it cost four times what materialising the
  whole vocabulary cost, and it made the artifact a quarter larger. It is now one
  base64 string of raw little-endian IEEE-754 bits. The vocabulary, the options
  and the header stay plain readable JSON, because those are the parts anyone
  actually reads; nobody inspects thirty thousand floats by eye. Exactness
  *improves*: raw bits round-trip by construction, with no decimal formatter
  involved. On a 30 000-feature model, saving went from 8.7 ms to 2.0 ms, loading
  from 12.6 ms to 5.0 ms, and the file from 782 KB to 589 KB.
- **Artifacts are written with the relaxed JSON encoder.** The default one
  escapes every non-ASCII character as `\uXXXX` — six bytes where UTF-8 needs
  two — which matters because this library ships Snowball stop-word lists for
  five languages, 258 of whose entries are accented. On an accented vocabulary
  that removed 9 201 escape sequences, shrank the artifact 18%, and made saving
  2.09× and loading 1.84× faster. The escaping JSON requires is still applied;
  what is dropped is the HTML-injection hardening an artifact never needed.
- **Single doubles use the shortest round-trippable form** on `net8.0` and later,
  keeping `"G17"` under `netstandard2.0` where .NET Framework does not guarantee
  it. Both read back identically and each build stays byte-reproducible against
  itself.
- Measured against scikit-learn with `pickle`, `Save` is now 2.09× faster and
  `Load` matches it on elapsed time. See [`bench/README.md`](bench/README.md) §4
  for the numbers, including processor time — which is the honest column, and
  where `Load` still costs 22% more than `pickle` because of background garbage
  collection.
- **`CsrMatrix`'s public constructor now validates its arrays.** `RowPointers`
  must be non-decreasing, start at 0 and end at `Values.Length`, and every column
  index must be in range. This was caller discipline while the arrays could only
  come from the vectorizers; deserialization makes an out-of-range column index
  an out-of-bounds read. The vectorizers build their own arrays and keep an
  internal unchecked path, so the validation costs them nothing.
- **`DataNet.Text` declares `System.Text.Json` on `netstandard2.0`.** It is
  in-box from `net8.0` onwards, so the `net10.0` package is still dependency-free
  and consumers on the modern target gain nothing new. This is the one place the
  "no external dependencies" rule is knowingly bent, and it is bent rather than
  hand-rolling a JSON reader for untrusted input. See
  [`docs/decisions/0011`](docs/decisions/0011-persistence-format.md).

### DataNet.Embeddings — 0.3.0

#### Added

- **Vocabulary loaders for the three formats a pretrained tokenizer ships in**:
  `VocabTxtLoader` (`vocab.txt`), `TokenizerJsonLoader` (`tokenizer.json`, both
  WordPiece and Unigram) and `SentencePieceModelLoader` (`spiece.model`). The
  guides previously told readers to parse a 30 000-entry vocabulary — or a
  protobuf — by hand. `SentencePieceModelLoader` carries a minimal hand-written
  protobuf reader: four wire types against a frozen format, rather than a runtime
  dependency in a package whose selling point is not having one.
- **`WordPieceVocabulary` and `SentencePieceVocabulary`**, carrying the settings
  that change tokenization and that a caller building the table by hand would
  have to guess — the unknown token, the continuation prefix, the lowercasing
  flag, and for SentencePiece the *type* of every piece.
- **`SentencePieceTokenizer(SentencePieceVocabulary)`**, which decides what may
  match text from each piece's declared type.
- The loaders **refuse** a file whose pipeline they do not reproduce — an `NFKC`
  or precompiled normalizer, a `BertPreTokenizer`, a `post_processor` that
  inserts `[CLS]`/`[SEP]` — naming what they found. A vocabulary that loads
  cleanly and produces embeddings for a model nobody trained is the worse outcome.
  The refusal covers what the file was *trained* as, not only its pipeline
  sections: a `spiece.model` built with `BPE`, `WORD` or `CHAR` rather than
  unigram, `byte_fallback` in either format, and a `Metaspace` whose
  `prepend_scheme` (or the older `add_prefix_space`) or `split` is away from the
  default. Each of those changes tokenization while leaving the vocabulary
  looking perfectly valid.
  A `spiece.model` carrying no `normalizer_spec`, and any special-token id
  (`unk_id`, `bos_id`, `eos_id`, `pad_id`) pointing outside the vocabulary, are
  refused for the same reason — the tokenizer never indexes by the sentence
  markers, but a caller naming them does, and would meet the bad id far from the
  file that carried it.
- **`added_tokens` are read rather than dropped.** `Tokenizer.add_tokens` assigns
  ids after the model's own vocabulary, so those entries appear nowhere in
  `model.vocab`; they are now folded into the loaded WordPiece vocabulary instead
  of tokenizing to the unknown token. An entry that contradicts `model.vocab`, or
  that asks for `lstrip`/`rstrip`/`single_word` matching, is refused.

#### Deprecated

- **`SentencePieceTokenizer(IReadOnlyList<SentencePiece>, int)`** — the id-based
  constructor. It inferred which pieces were control markers from "ids 0, 1 and 2
  starting with `<`", which is right for the models that happen to lay out that
  way and silently wrong for the rest. It still ships unchanged and will be
  removed in `2.0.0`; a test proves it agrees with the type-based constructor on
  a model where the guess held. Migration is one line: build a
  `SentencePieceVocabulary` with a loader and pass that instead.

### DataNet.Fuzzy — 0.3.0

#### Changed

- **Depends on `DataNet.Text` as a published NuGet package** rather than as a
  project reference. Nothing changes for consumers: a project reference between
  two packable projects already produced exactly this `<dependency>`, and
  `Fuzz.Ratio` is still `Indel.NormalizedSimilarity × 100`. What changes is that
  the build graph now matches the release graph, so a package can now ship
  without dragging the other two with it. All three happen to move to `0.3.0`
  here, each for its own reasons — that they agree is a coincidence of timing,
  not a constraint any longer. The dependency floor is
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
