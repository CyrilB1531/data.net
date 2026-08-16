# Changelog

All notable changes to this project are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and
this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

The four packages (`Lodestar.Text`, `Lodestar.Embeddings`, `Lodestar.Fuzzy`,
`Lodestar.Metrics`, published as `DataNet.*` up to 2026-08-15) version and release
**independently**, each from its own
`src/<Package>/Version.props`, so entries are grouped per package. Releases up to
and including `0.2.0` predate the split and covered all three at once — see
[`docs/decisions/0012`](docs/decisions/0012-per-package-versioning.md). From the
2026-08-14 release the heading carries the date alone, because the four packages
no longer share a number: `DataNet.Metrics` shipped its first `0.1.0` while the
other three shipped `0.3.0`. Each entry
is one sentence, the issue and the commit; see
[`CONTRIBUTING.md`](CONTRIBUTING.md#releasing) for the shape and why.

## [Unreleased]

### Lodestar.Text

#### Added

- `docs/reference/text/distances.md` documents every type of `Lodestar.Text.Distances` in the layout of the .NET API reference, and a test checks each declaration, parameter list and `Applies to` against the assembly. ([#181](https://github.com/CyrilB1531/data.net/issues/181))

#### Changed

- The package is `Lodestar.Text`, and its namespaces are `Lodestar.Text.*`. `DataNet.Text 0.3.0` and `Lodestar.Text 0.3.1` hold the same code: the id changed, nothing else did. ([#194](https://github.com/CyrilB1531/data.net/issues/194))
- The toolkit is `Lodestar`: the tags no longer say `datanet`, and every package carries an embedded icon rather than none. ([#194](https://github.com/CyrilB1531/data.net/issues/194))
- `DamerauLevenshtein`'s documented summary no longer says "Not a proper metric": unit-cost unrestricted Damerau-Levenshtein satisfies the triangle inequality and is a true metric; `Osa` is the one that does not. ([#181](https://github.com/CyrilB1531/data.net/issues/181))
- The reference is one page per member, with a type page and a namespace index above it: `docs/reference/text/distances.md` becomes 9 type pages and 22 member pages, and the index a reader lands on is 64 lines rather than 1034. ([#189](https://github.com/CyrilB1531/data.net/issues/189))

### Lodestar.Embeddings

#### Changed

- The package is `Lodestar.Embeddings`, and its namespaces are `Lodestar.Embeddings.*`. `Lodestar.Embeddings 0.3.1` holds the same code as `DataNet.Embeddings 0.3.0`. ([#194](https://github.com/CyrilB1531/data.net/issues/194))

### Lodestar.Fuzzy

#### Changed

- The package is `Lodestar.Fuzzy`, and its namespaces are `Lodestar.Fuzzy.*`. `Lodestar.Fuzzy 0.3.1` holds the same code as `DataNet.Fuzzy 0.3.0`, and its floor names `Lodestar.Text 0.3.1`. ([#194](https://github.com/CyrilB1531/data.net/issues/194))

### Lodestar.Metrics

#### Added

- `docs/reference/metrics/classification.md` and `docs/reference/metrics/regression.md` document every type of `Lodestar.Metrics` in the layout of the .NET API reference, and the same test checks each declaration, parameter list and `Applies to` against the assembly. ([#181](https://github.com/CyrilB1531/data.net/issues/181))

#### Changed

- The reference is one page per member, with a type page and a namespace index above it: the two documents above become 31 type pages and 42 member pages, and the index a reader lands on is 102 lines rather than 1646. ([#189](https://github.com/CyrilB1531/data.net/issues/189))
- The package is `Lodestar.Metrics`, and its namespaces are `Lodestar.Metrics.*`. ([#194](https://github.com/CyrilB1531/data.net/issues/194))

#### Added — clustering

- `AdjustedRand`, `NormalizedMutualInformation`, `Homogeneity`, `Completeness` and `VMeasure` score a clustering against a reference partition at scikit-learn parity, degenerate cases included: an empty input and a single sample both score `1`, and two independent partitions score `-0.5` on adjusted Rand. ([#172](https://github.com/CyrilB1531/data.net/issues/172))
- `Silhouette` scores a clustering with no reference partition, from the samples with the euclidean distance or from a distance matrix already computed, per sample or as their mean. ([#172](https://github.com/CyrilB1531/data.net/issues/172))

#### Added — ranking

- `Dcg`, `Ndcg` and `TopKAccuracy` score an ordered list of documents at scikit-learn parity, tie handling included: equal scores have their discounted gain averaged over the permutations of the tie by default, which on a row whose four scores are equal is `0.8069…` against `0.6138…` for `ignoreTies: true`. ([#173](https://github.com/CyrilB1531/lodestar/issues/173))
- `ReciprocalRank` scores rankings by the position of their first relevant document — the one member of this package **not verified against a reference**, because `sklearn.metrics` has no counterpart to freeze; its definition is pinned by tests under [`docs/decisions/0036`](docs/decisions/0036-a-member-may-ship-without-an-oracle-if-it-says-so.md), which also says what would retire the exception. ([#173](https://github.com/CyrilB1531/lodestar/issues/173))
- `CoverageError`, `LabelRankingLoss` and `LabelRankingAveragePrecision` score a boolean label matrix at scikit-learn parity, the two places the reference disagrees with itself included: a single label column is accepted by the average precision and refused by the other two, and a weight vector summing to zero gives `NaN` there where the other two raise. ([#201](https://github.com/CyrilB1531/lodestar/issues/201))
- A sample with no relevant label contributes `0` to `CoverageError` rather than the label count, so its mean can sit below `1` — measured, `0.5` on two samples one of which is empty; a tie between a relevant and an irrelevant label counts as an error in `LabelRankingLoss`, so a sample whose scores are all equal scores `1`. ([#201](https://github.com/CyrilB1531/lodestar/issues/201))

#### Added — ranking

- `Dcg.Score`, `Ndcg.Score` and `TopKAccuracy.Score` take a `sampleWeight`, which the reference has always had and these three did not — three rows of `docs/equivalence.md` called them identical anyway. With weights `TopKAccuracy`'s `normalize: false` returns the **sum of the weights** of the hits rather than how many there are, measured `7.0` against the unweighted `3.0`, and because that path never divides it returns `0` for a zero-sum vector where the fraction raises. ([#216](https://github.com/CyrilB1531/lodestar/issues/216))

#### Fixed — ranking

- `Dcg.Score` refuses a `logBase` outside `(0, ∞)` instead of returning a silent `NaN`: zero, a negative, `NaN` and infinity now raise `ArgumentOutOfRangeException`, which is where `dcg_score` raises too. A base below `1` is still accepted, and still takes the score negative. ([#215](https://github.com/CyrilB1531/lodestar/issues/215))

## Released — 2026-08-14

### DataNet.Text — 0.3.0

#### Added

- Stop-word lists for French, German, Italian, Portuguese and Spanish join the existing English list, one per language with a Snowball stemmer. ([#13](https://github.com/CyrilB1531/data.net/issues/13), [`58c5ed5`](https://github.com/CyrilB1531/data.net/commit/58c5ed5))
- `TfidfVectorizer`, `CountVectorizer` and `HashingVectorizer` gain `Save`/`Load` so a fitted model survives the process. ([#58](https://github.com/CyrilB1531/data.net/issues/58), [`d147abd`](https://github.com/CyrilB1531/data.net/commit/d147abd))
- `ArtifactLoadOptions` bounds what a loaded artifact may declare, so a malformed or hostile file raises `InvalidDataException` instead of `OutOfMemoryException`. ([#58](https://github.com/CyrilB1531/data.net/issues/58), [`d147abd`](https://github.com/CyrilB1531/data.net/commit/d147abd))

#### Changed

- The idf vector is stored as base64 raw IEEE-754 bits instead of JSON numbers. ([#58](https://github.com/CyrilB1531/data.net/issues/58), [`d147abd`](https://github.com/CyrilB1531/data.net/commit/d147abd))
- Artifacts are written with the relaxed JSON encoder instead of escaping every non-ASCII character. ([#58](https://github.com/CyrilB1531/data.net/issues/58), [`d147abd`](https://github.com/CyrilB1531/data.net/commit/d147abd))
- Single doubles use the shortest round-trippable form on `net8.0` and later, keeping `"G17"` on `netstandard2.0`. ([#58](https://github.com/CyrilB1531/data.net/issues/58), [`d147abd`](https://github.com/CyrilB1531/data.net/commit/d147abd))
- Measured against scikit-learn with `pickle`, `Save` is now 2.09× faster and `Load` matches it on elapsed time. ([#58](https://github.com/CyrilB1531/data.net/issues/58), [`d147abd`](https://github.com/CyrilB1531/data.net/commit/d147abd))
- Loading an artifact stopped copying the payload around: the read path sizes one buffer from the stream's length and decodes straight into the destination array. ([#100](https://github.com/CyrilB1531/data.net/issues/100), [`114245f`](https://github.com/CyrilB1531/data.net/commit/114245f))
- `CsrMatrix`'s public constructor now validates its arrays — `RowPointers` non-decreasing and in range, every column index in range. ([#58](https://github.com/CyrilB1531/data.net/issues/58), [`d147abd`](https://github.com/CyrilB1531/data.net/commit/d147abd))
- Stop-word removal no longer allocates the tokens it discards, since a dropped token is checked as a span rather than materialised. ([#80](https://github.com/CyrilB1531/data.net/issues/80), [`74f741b`](https://github.com/CyrilB1531/data.net/commit/74f741b))
- `DataNet.Text` declares `System.Text.Json` on `netstandard2.0`, where it is not in-box until `net8.0`. ([#58](https://github.com/CyrilB1531/data.net/issues/58), [`d147abd`](https://github.com/CyrilB1531/data.net/commit/d147abd))

### DataNet.Embeddings — 0.3.0

#### Added

- Vocabulary loaders cover the three formats a pretrained tokenizer ships in: `vocab.txt`, `tokenizer.json` and `spiece.model`. ([#58](https://github.com/CyrilB1531/data.net/issues/58), [`d147abd`](https://github.com/CyrilB1531/data.net/commit/d147abd))
- `WordPieceVocabulary` and `SentencePieceVocabulary` carry the settings that change tokenization: the unknown token, the continuation prefix, lowercasing, and piece type. ([#58](https://github.com/CyrilB1531/data.net/issues/58), [`d147abd`](https://github.com/CyrilB1531/data.net/commit/d147abd))
- `SentencePieceTokenizer(SentencePieceVocabulary)` decides what may match text from each piece's declared type. ([#58](https://github.com/CyrilB1531/data.net/issues/58), [`d147abd`](https://github.com/CyrilB1531/data.net/commit/d147abd))
- The loaders refuse a file whose pipeline they do not reproduce — an `NFKC` or precompiled normalizer, a `BertPreTokenizer`, a `post_processor` inserting `[CLS]`/`[SEP]` — naming what they found. ([#58](https://github.com/CyrilB1531/data.net/issues/58), [`d147abd`](https://github.com/CyrilB1531/data.net/commit/d147abd))
- `added_tokens` are read rather than dropped, reaching both tokenizers instead of tokenizing to the unknown token. ([#58](https://github.com/CyrilB1531/data.net/issues/58), [`d147abd`](https://github.com/CyrilB1531/data.net/commit/d147abd))
- The four `added_tokens` matching flags that decide where an entry matches now apply on both tokenizers. ([#104](https://github.com/CyrilB1531/data.net/issues/104), [`21f808b`](https://github.com/CyrilB1531/data.net/commit/21f808b))
- WordPiece added tokens are matched as text, not folded into the vocabulary, changing tokenization for any `tokenizer.json` carrying a non-empty `added_tokens` table. ([#104](https://github.com/CyrilB1531/data.net/issues/104), [`96b1b6b`](https://github.com/CyrilB1531/data.net/commit/96b1b6b))
- `BpeTokenizer`, `BpeVocabulary`, `BpeFilesLoader` and `TokenizerJsonLoader.LoadBpe` add a third sub-word tokenizer, matching `tokenizers.models.BPE` in both its classic and byte-level lineages, with byte-level `Encode`/`Decode` round-tripping any well-formed string exactly. ([`b46c474`](https://github.com/CyrilB1531/data.net/commit/b46c474))
- `continuing_subword_prefix` loads instead of being refused, applied to every symbol after the first of each pre-tokenized piece on the classic, non-byte-level lineage. ([#120](https://github.com/CyrilB1531/data.net/issues/120), [`dfa7639`](https://github.com/CyrilB1531/data.net/commit/dfa7639))
- `fuse_unk` loads instead of being refused: a run of consecutive uncovered characters becomes one unknown token rather than one each. ([#119](https://github.com/CyrilB1531/data.net/issues/119), [`c91f3ef`](https://github.com/CyrilB1531/data.net/commit/c91f3ef))
- The merge loop threads symbols on a doubly-linked list and a hand-rolled priority queue, replacing a rescan-and-shift loop that was quadratic on a token with no split point. ([`b46c474`](https://github.com/CyrilB1531/data.net/commit/b46c474))
- A batch encoding pipeline — `BatchEncoder`, `EncodingOptions`, `SpecialTokenTemplate`, `EncodedBatch`, `ISubwordTokenizer` — now owns matching a model's special-token wrapping instead of leaving it to the caller. ([#60](https://github.com/CyrilB1531/data.net/issues/60), [`c67b6c5`](https://github.com/CyrilB1531/data.net/commit/c67b6c5))
- `OnnxTextEmbedder.EmbedBatch` takes text in and returns one normalized vector per text out, in input order, mirroring `SentenceTransformer.encode`. ([#60](https://github.com/CyrilB1531/data.net/issues/60), [`c67b6c5`](https://github.com/CyrilB1531/data.net/commit/c67b6c5))
- `CancellationToken` is now accepted on every batch entry point. ([#60](https://github.com/CyrilB1531/data.net/issues/60), [`c67b6c5`](https://github.com/CyrilB1531/data.net/commit/c67b6c5))
- `Pooler.MeanPoolBatch` and `MeanPoolAndNormalizeBatch` pool a `[batch, seq, dim]` tensor with each row against its own mask slice. ([#60](https://github.com/CyrilB1531/data.net/issues/60), [`c67b6c5`](https://github.com/CyrilB1531/data.net/commit/c67b6c5))
- `EmbeddingIndex.Save`/`Load`, with `SaveAsync`/`LoadAsync` counterparts, round-trip a built index so embedding a corpus is not lost with the process. ([#62](https://github.com/CyrilB1531/data.net/issues/62), [`7e093c9`](https://github.com/CyrilB1531/data.net/commit/7e093c9))
- `EmbeddingIndex.Add(vector, id)`, `GetId` and `HasIds` attach an opaque id to each vector, kept off `SearchResult`. ([#62](https://github.com/CyrilB1531/data.net/issues/62), [`c06b472`](https://github.com/CyrilB1531/data.net/commit/c06b472))

#### Changed

- A `Sequence`'s `Split` step whose `pattern` declares both `Regex` and `String` is now refused, where it loaded by silently reading the first. ([#167](https://github.com/CyrilB1531/data.net/issues/167), [`01c0de1`](https://github.com/CyrilB1531/data.net/commit/01c0de1))
- `EmbeddingIndex.Load` now moves a vector block in three passes instead of five. ([#100](https://github.com/CyrilB1531/data.net/issues/100), [`114245f`](https://github.com/CyrilB1531/data.net/commit/114245f))
- `OnnxTextEmbedder.Embed` takes `ReadOnlySpan<long>` where it took `IReadOnlyList<long>`, a source break that removes two defensive copies per call. ([#60](https://github.com/CyrilB1531/data.net/issues/60), [`c67b6c5`](https://github.com/CyrilB1531/data.net/commit/c67b6c5))
- The default output is chosen deterministically instead of by dictionary key order. ([#60](https://github.com/CyrilB1531/data.net/issues/60), [`c67b6c5`](https://github.com/CyrilB1531/data.net/commit/c67b6c5))
- An output of unexpected rank now throws instead of producing an out-of-range access or a silently wrong result. ([#60](https://github.com/CyrilB1531/data.net/issues/60), [`c67b6c5`](https://github.com/CyrilB1531/data.net/commit/c67b6c5))
- The zero `token_type_ids` buffer is thread-static and never written to, instead of being allocated per call. ([#60](https://github.com/CyrilB1531/data.net/issues/60), [`c67b6c5`](https://github.com/CyrilB1531/data.net/commit/c67b6c5))
- An added token is a token, not a vocabulary entry: a single-character added token `model.vocab` does not declare no longer makes that character look covered. ([#130](https://github.com/CyrilB1531/data.net/issues/130), [`d785b86`](https://github.com/CyrilB1531/data.net/commit/d785b86))
- `BpeVocabulary.PreSplitPattern` becomes `PreSplit`, a `BpeSplitStep` carrying the pattern, the `behavior` and the `invert` flag together. ([#145](https://github.com/CyrilB1531/data.net/issues/145), [`9546b1c`](https://github.com/CyrilB1531/data.net/commit/9546b1c))
- A `BpeVocabulary` has to say how its text is split, and is refused when it declares none of `PreSplit`, `PreTokenizerPattern` or `NoPreTokenizer`. ([#122](https://github.com/CyrilB1531/data.net/issues/122), [`545c51e`](https://github.com/CyrilB1531/data.net/commit/545c51e))

#### Deprecated

- `SentencePieceTokenizer(IReadOnlyList<SentencePiece>, int)`, the id-based constructor, is deprecated in favor of building a `SentencePieceVocabulary` with a loader. ([#58](https://github.com/CyrilB1531/data.net/issues/58), [`d147abd`](https://github.com/CyrilB1531/data.net/commit/d147abd))

#### Fixed

- A merge pair listed twice in `model.merges` now keeps its last occurrence instead of its first, changing the tokens produced for a file that repeats one. ([#160](https://github.com/CyrilB1531/data.net/issues/160), [`708982f`](https://github.com/CyrilB1531/data.net/commit/708982f))
- A `Sequence` of `Split` then `ByteLevel` now applies both patterns instead of only the `Split` step's, changing the tokens produced for Llama-3 and Qwen2 on ordinary text. ([#143](https://github.com/CyrilB1531/data.net/issues/143), [`9a8d15c`](https://github.com/CyrilB1531/data.net/commit/9a8d15c))
- A `Sequence`'s `Split` step now honours its `behavior` and `invert` fields instead of always acting as `Removed` with `invert: true`. ([#145](https://github.com/CyrilB1531/data.net/issues/145), [`9546b1c`](https://github.com/CyrilB1531/data.net/commit/9546b1c))
- A `tokenizer.json` declaring no `pre_tokenizer`, or a bare `ByteLevel` step with `use_regex` off, now loads as `BpeVocabulary.NoPreTokenizer` instead of the `Whitespace` split. ([#122](https://github.com/CyrilB1531/data.net/issues/122), [`545c51e`](https://github.com/CyrilB1531/data.net/commit/545c51e))
- With a `Sequence` pre-tokenizer and `add_prefix_space` on, the space now goes on every piece the `Split` step produces instead of once per added-token segment, so `"a|b|c|d"` decodes to `" a | b | c | d"` where it decoded to `" a|b|c|d"`. ([#122](https://github.com/CyrilB1531/data.net/issues/122), [`26481a9`](https://github.com/CyrilB1531/data.net/commit/26481a9))
- A `Sequence`'s `Split` step whose pattern is spelled `{"String": …}` now loads, the literal escaped into the regex matching exactly it, instead of being refused for declaring no `pattern.Regex`. ([#167](https://github.com/CyrilB1531/data.net/issues/167), [`01c0de1`](https://github.com/CyrilB1531/data.net/commit/01c0de1))

### DataNet.Fuzzy — 0.3.0

#### Changed

- `DataNet.Fuzzy` depends on `DataNet.Text` as a published NuGet package rather than a project reference, so a package can ship without dragging the other two with it. ([#64](https://github.com/CyrilB1531/data.net/issues/64), [`96286ac`](https://github.com/CyrilB1531/data.net/commit/96286ac))

### DataNet.Metrics — 0.1.0

First release of a fourth package.

#### Added

- Classification metrics at scikit-learn parity: `ConfusionMatrix`, `Accuracy`, `Precision`, `Recall`, `F1`, `FBeta`, `ClassificationReport` and `RocAuc`. ([`3355f94`](https://github.com/CyrilB1531/data.net/commit/3355f94))
- All four averaging modes — `Averaging.Binary`, `Micro`, `Macro` and `Weighted` — are an enum instead of a string, with `average=None` becoming a separate `PerClass` method. ([`3355f94`](https://github.com/CyrilB1531/data.net/commit/3355f94))
- `ClassificationReport` comes in both shapes: structured rows a program can read, and `ToText(digits)` reproducing `classification_report`'s printed output character for character. ([`3355f94`](https://github.com/CyrilB1531/data.net/commit/3355f94))
- `RocAuc.Score` mirrors `_binary_clf_curve`'s sort-and-accumulate, and `RocAuc.MultiClass` covers both `ovr` and Hand & Till's `ovo`. ([`3355f94`](https://github.com/CyrilB1531/data.net/commit/3355f94))
- `ZeroDivision.Zero`, `One`, `NaN` or `Throw` give an explicit, caller-chosen answer for the 0/0 case scikit-learn silently defaults and warns on. ([`3355f94`](https://github.com/CyrilB1531/data.net/commit/3355f94))
- `sampleWeight` is threaded throughout, which is why matrix cells and support figures are `double` rather than `int`. ([`3355f94`](https://github.com/CyrilB1531/data.net/commit/3355f94))
- All 29 operations are measured at or above 1× scikit-learn's processor time rather than merely asserted, narrowest margin 2.74×. ([`3355f94`](https://github.com/CyrilB1531/data.net/commit/3355f94))
- Opt-in parallelism for multiclass ROC-AUC: `RocAuc.MultiClass(…, new MultiClassRocOptions { MaxDegreeOfParallelism = … })`, sequential by default and bit-identical either way. ([#86](https://github.com/CyrilB1531/data.net/issues/86), [`a2cae2b`](https://github.com/CyrilB1531/data.net/commit/a2cae2b))
- At n=100 000, k=10, on four physical cores, one-vs-rest drops from 76 ms sequential to 27 ms at eight workers, and one-vs-one from 127 ms to 37 ms at four. ([#86](https://github.com/CyrilB1531/data.net/issues/86), [`a2cae2b`](https://github.com/CyrilB1531/data.net/commit/a2cae2b))
- Balanced accuracy, Matthews correlation and Cohen's kappa — `BalancedAccuracy.Score`, `MatthewsCorrelation.Score` and `CohenKappa.Score` — each from labels or from an already-built `ConfusionMatrix`. ([`d00294a`](https://github.com/CyrilB1531/data.net/commit/d00294a))
- `confusion_matrix(…, normalize=…)` is a projection: `ConfusionMatrix.ToArray(Normalization.None/True/Pred/All)` returns scaled cells without the matrix itself remembering it was normalized. ([`d00294a`](https://github.com/CyrilB1531/data.net/commit/d00294a))
- `ZeroDivision` keeps a faithful default per metric rather than one across the package — `Zero` for precision, recall, F1, F-beta, the report and Matthews correlation; `NaN` for Cohen's kappa. ([`d00294a`](https://github.com/CyrilB1531/data.net/commit/d00294a))
- 18 new cross-language rows — three operations over six shapes — are at or above 1× scikit-learn's processor time, narrowest margin 16.59× on `balanced_accuracy` at n=1 000 000. ([`d00294a`](https://github.com/CyrilB1531/data.net/commit/d00294a))
- Regression metrics at scikit-learn parity: `MeanSquaredError`, `RootMeanSquaredError`, `MeanAbsoluteError`, `MedianAbsoluteError`, `MeanAbsolutePercentageError`, `MeanSquaredLogError`, `RootMeanSquaredLogError`, `MaxError`, `R2`, `ExplainedVariance` and `PinballLoss`. ([#92](https://github.com/CyrilB1531/data.net/issues/92), [`641f098`](https://github.com/CyrilB1531/data.net/commit/641f098))
- `multioutput=` is spelled by choosing a method: `Score(…)` is `uniform_average`, `PerOutput(…)` is `raw_values`, and `VarianceWeighted(…)` is `variance_weighted` on `R2` and `ExplainedVariance`. ([#92](https://github.com/CyrilB1531/data.net/issues/92), [`641f098`](https://github.com/CyrilB1531/data.net/commit/641f098))
- The undefined cases are two knobs, not one: `forceFinite` answers zero variance over two or more samples, and `R2`'s `ZeroDivision` separately answers fewer than two samples. ([#92](https://github.com/CyrilB1531/data.net/issues/92), [`641f098`](https://github.com/CyrilB1531/data.net/commit/641f098))
- The weighted median averages within one machine epsilon rather than exactly, matching scikit-learn's own overshoot test against `np.finfo(float64).eps`. ([`859da5c`](https://github.com/CyrilB1531/data.net/commit/859da5c))
- Two refusals taken from `check_array` and from `numpy.average`: a `sampleWeight` that is zero throughout, and `outputWeights` that sum to zero. ([`2216d5b`](https://github.com/CyrilB1531/data.net/commit/2216d5b))
- `log(1 + x)` is computed as `log1p`, using Kahan's identity, in `MeanSquaredLogError` and `RootMeanSquaredLogError`. ([`2216d5b`](https://github.com/CyrilB1531/data.net/commit/2216d5b))
- `R2`'s two passes, `ExplainedVariance`'s five accumulations, and `Outputs.WeightedMean` now sum with Neumaier compensation rather than a running total. ([#127](https://github.com/CyrilB1531/data.net/issues/127), [`fcb705b`](https://github.com/CyrilB1531/data.net/commit/fcb705b))
- `mse`, `mae`, `median_ae` and `r2` were benchmarked against scikit-learn over six shapes; `median_ae` is the one operation below the 1× processor-time gate, at 0.80–0.90×. ([#92](https://github.com/CyrilB1531/data.net/issues/92), [`641f098`](https://github.com/CyrilB1531/data.net/commit/641f098))

#### Changed

- `DataNet.Metrics`'s long comment blocks became ten decision records, so the reasoning lives where it can be cited instead of duplicated at each call site. ([#151](https://github.com/CyrilB1531/data.net/issues/151), [`d4d9326`](https://github.com/CyrilB1531/data.net/commit/d4d9326))
- The Neumaier-versus-Kahan argument for `CompensatedSum` moved into a record of its own instead of living only as comments in the source. ([#151](https://github.com/CyrilB1531/data.net/issues/151), [`4abb609`](https://github.com/CyrilB1531/data.net/commit/4abb609))
- `MultiClassRocOptions`'s doc comments no longer restate `docs/decisions/0018`, and `Normalization`'s comment points at `0020` instead of repeating it. ([#151](https://github.com/CyrilB1531/data.net/issues/151), [`4abb609`](https://github.com/CyrilB1531/data.net/commit/4abb609))
- The rest of the package's remaining long comments were trimmed to their reason, with no behaviour changed. ([#151](https://github.com/CyrilB1531/data.net/issues/151), [`4abb609`](https://github.com/CyrilB1531/data.net/commit/4abb609))

## [0.2.0] — 2026-08-05

Reach, correctness and honesty about performance. Nothing in the public API was
removed or renamed, so upgrading from `0.1.0` is a version bump.

### Added

- `netstandard2.0` becomes a second target framework, reaching .NET Framework 4.6.1+, Mono, Xamarin and Unity through conditional compilation rather than a reduced API. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))
- Four Snowball stemmers join English and French: `SpanishSnowballStemmer`, `PortugueseSnowballStemmer`, `ItalianSnowballStemmer` and `GermanSnowballStemmer`. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))
- Blocked (multi-word) Myers removes the 64-character cap on `Levenshtein.Distance`'s bit-parallel path. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))
- A benchmark suite compares the `net10.0` and `netstandard2.0` builds of the same library. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))
- Mirror test projects replay the entire suite against the `netstandard2.0` assemblies, 339 tests across both builds. ([#17](https://github.com/CyrilB1531/data.net/issues/17), [`48b7d05`](https://github.com/CyrilB1531/data.net/commit/48b7d05))
- A sample under `samples/DataNet.Sample` consumes the packages by `PackageReference` from a locally packed feed, and runs in CI. ([#50](https://github.com/CyrilB1531/data.net/issues/50), [`391a71c`](https://github.com/CyrilB1531/data.net/commit/391a71c))
- `CONTRIBUTING.md` and this changelog are added. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))
- SonarQube Cloud analysis, a `lint` CI job (markdownlint and `dotnet format`), and Dependabot for GitHub Actions are added. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))

### Changed

- Long-string `Levenshtein.Distance` is 20–33× faster: 684 µs to 21 µs at 512 characters. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))
- Regular expressions are bounded by a match timeout: a pathological pattern now raises `RegexMatchTimeoutException` instead of hanging the calling thread. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))
- Warnings are errors across the whole repository, covering `src`, `tests` and `bench` rather than the libraries alone. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))

### Fixed

- Static-analysis defects fixed and verified against the oracle corpora: an `int` division widened to `double` in `Jaro`, nested classes shadowing their outer type in the Snowball stemmers, unread step-method return values, and nested ternaries in three files. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))
- Code coverage was never collected: CI referenced `coverlet.collector` without depending on it, so the collection step silently did nothing. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))

### Security

- A `workflow_dispatch` input was interpolated directly into a shell command in a job holding `id-token: write`, letting it mint a nuget.org publishing key; values now reach the shell through the environment. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))
- GitHub Actions are pinned to full commit SHAs, so a moved tag cannot change what runs in CI. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))
- CI dependency installation is hardened: markdownlint pinned with lifecycle scripts disabled, and `pip install --require-hashes` against a generated lock file pinning all 29 packages. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))

### Documentation

- Package metadata now attributes the project to Cyril BRUNET (`Authors`, `Company`, `Copyright`). ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`7523f34`](https://github.com/CyrilB1531/data.net/commit/7523f34))
- `THIRD-PARTY-NOTICES.md` now records the shipped dependencies instead of saying "None yet". ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`7523f34`](https://github.com/CyrilB1531/data.net/commit/7523f34))

### Notes

- Deliberate analyzer suppressions live in the source as `#pragma warning disable` with their justification, since SonarLint reads neither `.editorconfig` nor workspace settings. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))
- The `netstandard2.0` build is behavior-verified: the whole suite is replayed against those assemblies, not only compiled. ([#17](https://github.com/CyrilB1531/data.net/issues/17), [`48b7d05`](https://github.com/CyrilB1531/data.net/commit/48b7d05))

> Entries below predate the per-lot issue convention and this shape: this
> repository had not yet adopted filing one issue per change, so several point
> at the same issue rather than one each. A missing link is a date, not an
> oversight.

## [0.1.0] — 2026-08-01

First release. All four lots of the project brief are delivered, and every
building block is validated by replaying frozen reference outputs captured from
the canonical Python libraries — see [`docs/equivalence.md`](docs/equivalence.md).

### Added

- Lot 1 — string distances and similarity (`DataNet.Text`): Levenshtein (with a Myers bit-parallel fast path), OSA, Damerau-Levenshtein, Hamming, Jaro, Jaro-Winkler, Indel, LCS, Ratcliff-Obershelp, Jaccard, Dice, Overlap, Tversky, Cosine, Soundex, Metaphone, NYSIIS. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))
- Lot 2 — tokenization and sparse vectorization (`DataNet.Text`): CSR matrix, word/char/char_wb tokenizers, `CountVectorizer`, `TfidfVectorizer`, `HashingVectorizer` (MurmurHash3-32), Porter and Snowball EN/FR stemmers, English stop words. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))
- Lot 3 — embeddings and semantic search (`DataNet.Embeddings`): WordPiece and SentencePiece (unigram Viterbi) tokenizers, pooling, SIMD kNN, ONNX inference, with ONNX Runtime isolated to this package. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))
- Lot 4 — applied fuzzy matching (`DataNet.Fuzzy`): `fuzz.*` (ratio / partial / token_sort / token_set / WRatio), `process.extract` and `extractOne`, blocking deduplication. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))
- Migration guides for NumPy, pandas, scikit-learn, statsmodels, PyTorch, matplotlib and seaborn, plus a three-column inventory mapping each need to use / build / decide. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))
- A decision log records the deliberate divergences from the Python references. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))
- Publishing to nuget.org via Trusted Publishing (keyless, OIDC) and to GitHub Packages. ([#8](https://github.com/CyrilB1531/data.net/issues/8), [`0a321f1`](https://github.com/CyrilB1531/data.net/commit/0a321f1))

[Unreleased]: https://github.com/CyrilB1531/data.net/compare/DataNet.Text/v0.3.0...HEAD
[0.2.0]: https://github.com/CyrilB1531/data.net/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/CyrilB1531/data.net/releases/tag/v0.1.0
