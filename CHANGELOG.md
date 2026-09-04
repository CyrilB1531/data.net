# Changelog

All notable changes to this project are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and
this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

The eight packages (`Lodestar.Text`, `Lodestar.Embeddings`, `Lodestar.Fuzzy`,
`Lodestar.Metrics` — published as `DataNet.*` up to 2026-08-15 — plus
`Lodestar.Conformal`, `Lodestar.Abstractions`, `Lodestar.Decomposition` and
`Lodestar.Onnx`, all newer than that rename)
version and release **independently**, each from its own
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

- **`BkTree`** is a metric index over the integer distances, worth building only at a radius of 1 — [`docs/guides/dictionary-lookup.md`](docs/guides/dictionary-lookup.md) has the measurement. ([#526](https://github.com/CyrilB1531/lodestar/issues/526))

- **`Lodestar.Text.Keywords` adds two unsupervised keyword extractors, `Rake` and `TextRank`.** Neither trains or downloads a model: `Rake` scores the runs between stop words by their word co-occurrence, and `TextRank` ranks a co-occurrence graph of the document's own stems the way PageRank ranks a link graph. Each replays a frozen oracle against its Python reference — `rake-nltk` 1.0.6 in `tests/oracles/keywords_rake.json`, `summa` 1.2.0 in `tests/oracles/keywords_textrank.json` — and [decision 0077](docs/decisions/0077-the-keyword-extractors-take-their-oracles-lists-and-not-their-own.md) records where each is measured to diverge from it. [`docs/guides/keyword-extraction.md`](docs/guides/keyword-extraction.md) has both, plus the KeyBERT-style composition with `Mmr.Select`. ([#525](https://github.com/CyrilB1531/lodestar/issues/525))

#### Changed

- **`CsrMatrix` and `SparseNorm` moved to `Lodestar.Abstractions`.** Consuming code adds `using Lodestar.Abstractions;`; the vectorizers still return the same type, and the seven reference pages moved with it. ([#440](https://github.com/CyrilB1531/lodestar/issues/440))

- `Lcs.SubsequenceLength` takes the bit-parallel route from a pattern of 2 characters and `Levenshtein.Distance` from 5, while a pattern holding a character above U+00FF is refused below 6 and 10 instead. ([#411](https://github.com/CyrilB1531/lodestar/issues/411), [`a5c0d52`](https://github.com/CyrilB1531/lodestar/commit/a5c0d52))

#### Fixed

- The blocked bit-parallel equality table is sized from the pattern's characters above U+00FF rather than from its length, and a pattern too long to tabulate takes the dynamic program instead of wrapping the table's length in `int`. ([#413](https://github.com/CyrilB1531/lodestar/issues/413), [`52d68cc`](https://github.com/CyrilB1531/lodestar/commit/52d68cc))

### Lodestar.Embeddings

#### Added

- **`Mmr.Select` (`Lodestar.Embeddings.Search`) picks a diverse, relevance-weighted subset of candidate vectors — Maximal Marginal Relevance**, knowing nothing about text: the candidates are vectors and the result is their indices, in selection order. It replays `keybert` 0.9.0's own selection step, `keybert._mmr.mmr`, compared as a set rather than a sequence (`tests/oracles/mmr.json`) — [decision 0077](docs/decisions/0077-the-keyword-extractors-take-their-oracles-lists-and-not-their-own.md) has the three divergences, and [decision 0078](docs/decisions/0078-keybert-is-declared-nodeps-not-compiled-into-the-lock.md) why `keybert` itself stays out of the oracle lock file. Composes with `Rake` and `OnnxTextEmbedder` into a KeyBERT-style pipeline, walked through in [`docs/guides/keyword-extraction.md`](docs/guides/keyword-extraction.md). ([#525](https://github.com/CyrilB1531/lodestar/issues/525))

- **`BatchEncoder.EncodeAll` and `BatchEncoder.Pad` are public**, so a caller that groups rows itself no longer needs a second copy of the padding. `EncodeAll` returns one unpadded row per text, template applied and truncation done; `Pad` lays a **window** of those rows out as one rectangle, widened to the longest row in that window rather than in the corpus — which is what makes grouping by length worth anything. `EncodeBatch` is unchanged, and is still the two of them over the whole corpus at once. ([#533](https://github.com/CyrilB1531/lodestar/issues/533))

- `EmbeddingIndex.FromBlock` and `EmbeddingIndex.FromOwnedBlock` build an index from a contiguous block of vectors in one copy or none, where replaying the block through `Add` cost three times the read that produced it — the adopting factory keeps the caller's array for the life of the index, an invariant the caller keeps and [decision 0056](docs/decisions/0056-a-block-may-be-adopted-and-the-invariant-is-the-callers-to-keep.md) argues for. ([#474](https://github.com/CyrilB1531/lodestar/issues/474), [`13bdacc`](https://github.com/CyrilB1531/lodestar/commit/13bdacc))

- `bench/Lodestar.Text.Benchmarks -- sidecar` prices a binary sidecar against the JSON artifact in bytes and in time, and [decision 0055](docs/decisions/0055-the-artifact-gets-a-binary-sidecar-once-a-block-can-be-ingested-whole.md) takes one — conditional on a bulk ingest into `EmbeddingIndex`, without which the sidecar route is slower than what it replaces. No shipped behaviour changes yet. ([#436](https://github.com/CyrilB1531/lodestar/issues/436), [`7ab80d1`](https://github.com/CyrilB1531/lodestar/commit/7ab80d1))

- **numpy's `.npy` reads and writes, for the vector block only.** `NpyFile.Read` and `NpyFile.Write` carry a contiguous `float32` block in numpy's own format, returning an `NpyBlock` of the values and the shape; the header is parsed against a fixed grammar and never evaluated, so `descr: '|O'` — numpy's pickle-backed dtype — is refused by name before the payload is touched. It is interop and not a second artifact format: a `.npy` carries no ids, no normalize flag and no schema, so `EmbeddingIndex.Save` is untouched and [decision 0011](docs/decisions/0011-persistence-format.md) is not reopened. ([#450](https://github.com/CyrilB1531/lodestar/issues/450), [`0f05972`](https://github.com/CyrilB1531/lodestar/commit/0f05972))

- **`byte_fallback` resolves an uncovered symbol into `<0xXX>` byte pieces instead of the unknown token, so Llama-2 and Mistral v0.1 both load.** `BpeVocabulary.ByteFallback` and `TokenizerJsonLoader.LoadBpe` require the vocabulary to carry all 256 pieces, refusing by name a file that does not rather than reproduce the silent degradation — or, with no unknown token declared, the dropped symbol — `tokenizers` 0.23.1 falls back to; the expansion runs before the merges, on the decorated symbol, so a `continuing_subword_prefix` or `end_of_word_suffix` on it is itself encoded as bytes. `BpeTokenizer.Decode` now reproduces such a file's `decoder` block too, a bare `ByteFallback` or Llama-2's own `Sequence[Replace, ByteFallback, Fuse, Strip]`, round-tripping the byte pieces and the whitespace escape together — [decision 0063](docs/decisions/0063-byte-fallback-requires-the-whole-alphabet-and-its-decoder-is-read-strictly-too.md) has the measurements against the reference, including an upstream ordering bug found and not reproduced. ([#317](https://github.com/CyrilB1531/lodestar/issues/317), [`6b4f2b6`](https://github.com/CyrilB1531/lodestar/commit/6b4f2b6))

#### Removed

- **`OnnxTextEmbedder` moved to the new `Lodestar.Onnx` package, and this one now carries no external dependency at all.** `Microsoft.ML.OnnxRuntime` 1.28.0 was the repository's only external dependency and was reached by one file of 407 lines, while the four sub-word tokenizers, the batch encoder, the pooling, the `.npy` reader and the SIMD kNN index could not be had without it — `dotnet add package Lodestar.Embeddings` restored a native runtime for a caller who only tokenizes. Migration is one `using`: the type is `Lodestar.Onnx.OnnxTextEmbedder`, with the same members and the same behaviour, in a package that depends on this one. [Decision 0076](docs/decisions/0076-a-core-package-carries-no-external-dependency.md) states the rule it settles — a core package carries no external dependency, an external dependency earns its own satellite package — supersedes [0069](docs/decisions/0069-the-package-layout-as-built-and-what-enforces-it.md), and records what was refused. ([#533](https://github.com/CyrilB1531/lodestar/issues/533))

#### Fixed

- `NpyFile.Read` bounds a block by `ArtifactLoadOptions.MaxTotalBytes` rather than by `MaxArrayLength`, which that option documents as not applying to a vector block: a 2 605 × 384 block — small for embeddings — was refused at the default options while the same vectors loaded from an index artifact. ([#468](https://github.com/CyrilB1531/lodestar/issues/468), [`c480c1f`](https://github.com/CyrilB1531/lodestar/commit/c480c1f))

#### Changed

- **The `.npy` read copies the block once, and a second entry point copies it none.** `NpyFile.Read(Stream)` reads the payload straight into the `float[]` the returned block keeps, where it used to stage the same bytes through two buffers first, and names that array as `NpyBlock.OwnedArray` so `EmbeddingIndex.FromOwnedBlock` can adopt it rather than copy the block once more; `NpyFile.Read(ReadOnlyMemory<byte>)` serves a caller already holding the file by **aliasing** those bytes, which must not change while the block lives, and leaves `OwnedArray` null because a borrowed block has no array to hand on. Reading the same 15 360 128 bytes against `np.load` measured 0.21–0.23× of numpy's wall time with three copies between the stream and the block, and a fourth into the index that held it; on the adopting route it now measures **1.00–1.13× cpu and 1.21–1.25× wall** — parity in the first round and slightly ahead in the other two on cpu, the column this project trusts, where it was four to five times behind. The stream read is one copy on `net10.0` and two on `netstandard2.0`, which has no `Stream.Read(Span<byte>)` to read into a caller's array — one API and one behaviour at two speeds, as [decision 0057](docs/decisions/0057-the-npy-read-serves-a-stream-and-a-buffer-differently.md) records with the view on every path it refused. ([#466](https://github.com/CyrilB1531/lodestar/issues/466), [`a3d3145`](https://github.com/CyrilB1531/lodestar/commit/a3d3145))
- **The payload buffer is rented, not allocated.** `EmbeddingIndex.Load(Stream)` takes its artifact buffer from `ArrayPool<byte>.Shared` and returns it once parsing is done, which removes 20.5 MB of allocation and three of the four collections a load provoked: renting is **42× the allocation and 1.74 ms a load**, about a tenth of one, because what cost was never the allocation but the large-object collection it triggered. The pool holds 33.5 MB for the life of the process in exchange — see [decision 0054](docs/decisions/0054-the-payload-buffer-is-pooled-after-all-because-the-collection-is-the-cost.md), which amends [0053](docs/decisions/0053-the-payload-buffer-is-not-pooled-because-residency-outlives-the-load.md) for refusing that trade without ever timing it. ([#470](https://github.com/CyrilB1531/lodestar/issues/470), [`f8de2ba`](https://github.com/CyrilB1531/lodestar/commit/f8de2ba))
- **Half the allocation, same bytes on disk.** `EmbeddingIndex.Save` and `SaveAsync` write the vector block a slice at a time instead of handing `Utf8JsonWriter.WriteBase64String` the whole thing, so the writer's buffer no longer doubles its way up to the 20.48 MB the encoding occupies: `EmbeddingIndexSave` allocates **19.87 MB against 39.64**, with a third fewer collections in every generation, and the row against `numpy.save` moves **0.29× to 0.39×**. Slices are 245 760 bytes — a multiple of 12, so a whole number of base64 groups and of floats — which is what makes the artifact byte-for-byte what it was; `SaveAsync` loses its intermediate `MemoryStream` with it. The load pays part of it back, having been subsidised by the buffer the save used to leave behind — see [decision 0051](docs/decisions/0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md), which also records why parallelising the base64 was refused: it runs at `memcpy` speed already. ([#430](https://github.com/CyrilB1531/lodestar/issues/430), [`2a50cc1`](https://github.com/CyrilB1531/lodestar/commit/2a50cc1))

### Lodestar.Onnx

#### Added

- **First release, 0.1.0: ONNX inference, and the satellite tier's first member.** One type, `OnnxTextEmbedder`, moved verbatim from `Lodestar.Embeddings` into namespace `Lodestar.Onnx` — every package sets `RootNamespace` equal to its `PackageId`, and the rename is also what let the split land without colliding with the copy published in `Lodestar.Embeddings` 0.4.0 and 0.5.0. It depends on `Lodestar.Embeddings` 0.5.0 for the tokenizers, the encoding options and the pooling it feeds a session with, and on `Microsoft.ML.OnnxRuntime` 1.28.0, which no other package in the repository now references. Ships `net10.0;netstandard2.0` like the rest. ([#533](https://github.com/CyrilB1531/lodestar/issues/533))

### Lodestar.Decomposition

#### Changed

- **`Nmf.Fit(matrix, k)` accepts `k == min(rows, columns)`**, scikit-learn's own bound, where it refused any `k` at or above the column count — a bound inherited from the validation `TruncatedSvd` needs rather than from anything NMF does, so a square matrix at full rank was a fit there and an `ArgumentOutOfRangeException` here. The oracle corpus now freezes a `24 × 8` fit at `k = 8` against `NMF` itself, and `TruncatedSvd`'s own bound is untouched: `n_components >= n_features` is what scikit-learn refuses there too. ([#519](https://github.com/CyrilB1531/lodestar/issues/519))

## Released — 2026-09-01

Four tags on one day, and none of them had a section here: the three packages below
kept their entries under *Unreleased* while their releases were already on the feed.
Each entry is filed under the tag its own commit is an ancestor of, which is how the
2026-08-16 wave was reconstructed too. `Nmf.Fit`'s component bound stays unreleased —
it landed after `Lodestar.Decomposition/v0.1.0` was cut.

### Lodestar.Abstractions — 0.1.0

#### Added

- **The sparse primitive the packages share.** `CsrMatrix` and `SparseNorm` ship in a package of their own, with two new products — `Multiply(block, columnCount)` and `TransposeMultiply(block, columnCount)` — that read the matrix once per non-zero rather than once per block column. `Lodestar.Text` still declares its own copy until its next release; [decision 0071](docs/decisions/0071-csrmatrix-moves-to-an-abstractions-package.md) amends [0069](docs/decisions/0069-the-package-layout-as-built-and-what-enforces-it.md) and records the sequence. ([#440](https://github.com/CyrilB1531/lodestar/issues/440))

### Lodestar.Conformal — 0.1.0

#### Added

- **Split conformal prediction, at MAPIE 1.5.0 parity.** `SplitConformal` turns a point prediction into an interval or a class into a prediction set, with a finite-sample coverage guarantee: `AbsoluteResiduals` and `LeastAmbiguousScores` score a calibration set, `Quantile` reduces the scores to the one number that carries the guarantee, and `Interval` and `PredictionSet` apply it. Static and dependency-free, the fifth package under [decision 0069](docs/decisions/0069-the-package-layout-as-built-and-what-enforces-it.md)'s first rule. The empty LAC prediction set is reproduced rather than repaired, and `k > n` returns an infinite interval instead of MAPIE's clamp to the widest score — [decision 0070](docs/decisions/0070-k-greater-than-n-returns-an-infinite-interval.md). The guarantee assumes exchangeability, which the guide leads with. ([#441](https://github.com/CyrilB1531/lodestar/issues/441))

### Lodestar.Decomposition — 0.1.0

#### Added

- **`TruncatedSvd` — `sklearn.decomposition.TruncatedSVD(algorithm="randomized")` at parity, over a `CsrMatrix` and without centring it.** Fit, transform, components, singular values, explained variance and its ratio; all three power-iteration normalizers, including `Auto`'s rule. Ω is an input rather than a seed, which is what makes a randomized algorithm an ordinary parity target — [decision 0072](docs/decisions/0072-omega-is-an-input-not-a-seed.md) has the measurement and what it refuses. ([#440](https://github.com/CyrilB1531/lodestar/issues/440))
- **`Nmf` — `sklearn.decomposition.NMF(solver="mu")` at parity, on both β losses, from the NNDSVD family.** The dense kernels it needs — thin Householder QR, LU with partial pivoting, one-sided Jacobi SVD — are written here, so the package's only dependency is `Lodestar.Abstractions`. ([#440](https://github.com/CyrilB1531/lodestar/issues/440))

### Lodestar.Abstractions — 0.1.1

#### Fixed

- **The shared internal helpers are no longer compiled into this package.** `src/Shared/Guard.cs` and its siblings are compiled into every library, and this one grants `InternalsVisibleTo` to `Lodestar.Text`, which compiles them too — one internal type in both assemblies is CS0436 at every call site on the consuming side, 96 of them across the two target frameworks. `CsrMatrix` carries the two argument guards it needs instead; behaviour and exception types are unchanged. ([#440](https://github.com/CyrilB1531/lodestar/issues/440))

## Released — 2026-08-21

Four deliverables, cut in two steps on the same day. `Lodestar.Text`,
`Lodestar.Embeddings` and `Lodestar.Metrics` went first; `Lodestar.Fuzzy` followed once
`Lodestar.Text 0.4.0` was served by nuget.org, which is what
`src/Directory.Packages.props` requires before its floor may move — and moving that
floor is the whole of what Fuzzy publishes here, its source being untouched since
`0.3.1`.

### Lodestar.Text — 0.4.0

#### Changed

- **CJK and emoji take the bit-parallel path.** A pattern holding a character above U+00FF sent `Levenshtein.Distance`, `Lcs.SubsequenceLength` and therefore `Indel` and `fuzz.ratio` back to the dynamic program in the UTF-16 mode, because the equality table was indexed by the character. A side table now carries those symbols on both the single-word and the blocked route, so the kernels no longer refuse an alphabet — see [decision 0043](docs/decisions/0043-the-equality-table-is-sized-to-the-pattern.md). ([#302](https://github.com/CyrilB1531/lodestar/issues/302), [`649b8e6`](https://github.com/CyrilB1531/lodestar/commit/649b8e6))
- **Faster, same answers.** The blocked bit-parallel LCS kernel no longer threads a borrow between its 64-bit words. It never needed one: the subtrahend is `v & peq`, a bit-subset of `v`, and subtracting a subset cannot borrow — the chain had been carrying a provably zero value since #273. Measured **1.56×** at length 512 and **1.43×** at 128 on the pair corpus, interleaved over four replications with `Levenshtein` as an untouched control, which moves the two long buckets from roughly 2× behind rapidfuzz to 1.38× and 1.53×. ([#357](https://github.com/CyrilB1531/lodestar/issues/357), [`5a448a9`](https://github.com/CyrilB1531/lodestar/commit/5a448a9))
- **Faster, same answers.** The blocked bit-parallel LCS kernel — which `Indel` and therefore `fuzz.ratio` reach on long inputs — no longer calls a helper once per text character, and no longer re-tests inside its inner loop whether that character is one the equality table can hold. Measured **1.10×** on the pair corpus's length-512 bucket, interleaved over four replications with `Levenshtein` as an untouched control; the length-128 bucket is unchanged, its patterns spanning two machine words against eight. ([#320](https://github.com/CyrilB1531/lodestar/issues/320), [`1fa65f3`](https://github.com/CyrilB1531/lodestar/commit/1fa65f3))
- **Faster, same answers.** `Levenshtein.Distance`, `Lcs.SubsequenceLength` and therefore `Indel` and `fuzz.ratio` take the bit-parallel route from a pattern of 8 characters rather than 16, and neither kernel clears an equality table that `stackalloc` has already zeroed: on the pair corpus's length-32 bucket that is **2.09×** for Levenshtein (427.6 → 204.8 ns/pair) and **2.19×** for Indel (318.7 → 145.6 ns/pair), with every other bucket within noise and the 3 905-test suite unchanged. ([#208](https://github.com/CyrilB1531/lodestar/issues/208), [`cae6236`](https://github.com/CyrilB1531/lodestar/commit/cae6236))
- **Breaking.** `Soundex.Encode(string)`, `Nysiis.Encode(string)` and `Metaphone.Encode(string)` now throw `ArgumentNullException` on a `null` word instead of silently returning `""`, matching the stemmers next door — see [decision 0042](docs/decisions/0042-phonetic-encoders-refuse-a-null-word.md). ([#342](https://github.com/CyrilB1531/lodestar/issues/342), [`4000a05`](https://github.com/CyrilB1531/lodestar/commit/4000a05))

### Lodestar.Embeddings — 0.4.0

#### Added

- The embeddings guide documents how to compress an artifact, and what it costs: the caller wraps the stream on both sides, which works today and needed no library change. Deflate takes back the format's 1.33× base64 expansion almost exactly, at **26.67× the save and 7.19× the load**, and 76.8× and 14.8× on the benchmark corpus's larger index — so the library declines to do it by default, and [decision 0044](docs/decisions/0044-compression-belongs-to-the-caller.md) records why. `compare-persistence` now reports the bytes each row wrote or read, next to its time. ([#378](https://github.com/CyrilB1531/lodestar/issues/378), [`01642c9`](https://github.com/CyrilB1531/lodestar/commit/01642c9))
- `EmbeddingIndex.Load(ReadOnlyMemory<byte>)` reads an index from bytes the caller already holds — a blob, a cache entry, an embedded resource — where handing them to the `Stream` overload made the loader copy them back out first. Measured **1.40×** on processor time against that overload, both rows in the same run, which is the read phase [#324](https://github.com/CyrilB1531/lodestar/issues/324) profiled at about a third of the load. It checks `MaxTotalBytes` before parsing rather than while reading, the length being known up front, and has no `Async` counterpart because nothing is waited on. It is the only loader to gain one: the saving scales with the artifact and no other is large enough. ([#336](https://github.com/CyrilB1531/lodestar/issues/336), [`27fa908`](https://github.com/CyrilB1531/lodestar/commit/27fa908))

#### Changed

- **`EmbeddingIndex.LoadAsync` no longer refuses an index that `Load` accepts.** The segmented read #377 gave the synchronous path stopped there, so the same artifact past the CLR's array ceiling loaded one way and threw the other — a disagreement between two overloads of the same method rather than a missing feature. Both now take the same decision on the same threshold, and the chain they build is one implementation so they cannot drift apart again; a cancelled read throws instead of parsing a partial chain. ([#396](https://github.com/CyrilB1531/lodestar/issues/396), [`3a89dde`](https://github.com/CyrilB1531/lodestar/commit/3a89dde))
- **An index is no longer capped by the text encoding of its vectors.** An artifact past the CLR's array ceiling was read into one `byte[]` and could not be, so the format's 1.34× expansion came straight off the largest index that could exist — about 1.04 million vectors at 384 dimensions where the raw block allowed 1.40 million. `EmbeddingIndex.Load` now reads such an artifact in segments and hands the parser a `ReadOnlySequence<byte>`, which `Utf8JsonReader` reads natively. **The bytes on disk do not change**, so an artifact written by any earlier version still loads. ([#377](https://github.com/CyrilB1531/lodestar/issues/377), [`cfc1945`](https://github.com/CyrilB1531/lodestar/commit/cfc1945))
- **Faster, same answers.** Loading an artifact no longer has the runtime zero the two large buffers it overwrites in full — the payload the stream fills and the vector block the base64 decoder fills. Measured **1.18×** on `embedding_index_load`, with a write-only operation re-run as an untouched control; a small artifact such as a fitted vectorizer sees nothing, its buffers never reaching the large-object heap. ([#324](https://github.com/CyrilB1531/lodestar/issues/324), [`359d889`](https://github.com/CyrilB1531/lodestar/commit/359d889))
- **Faster, same bytes.** `EmbeddingIndex.Save` no longer allocates and copies the whole vector block before encoding it: on a little-endian machine the bytes to base64 are the ones already in the span, and the copy existed only to carry an endianness swap that is a no-op there. Measured **1.46×** on processor time at the benchmark's size, with the load direction re-run as an untouched control, and the encoding pinned byte for byte by a new test. ([#323](https://github.com/CyrilB1531/lodestar/issues/323), [`4359d32`](https://github.com/CyrilB1531/lodestar/commit/4359d32))

### Lodestar.Metrics — 0.3.0

#### Added

- `docs/guides/metrics.md` answers which metric to reach for, which the per-member reference pages deliberately cannot: a router across the four families, and the four things true of all of them — row-major input with a count, `sampleWeight` as a weighted mean, `ZeroDivision` as an argument rather than a warning, and the answers that look like bugs and are scikit-learn's. ([#203](https://github.com/CyrilB1531/lodestar/issues/203), [`8aaa19a`](https://github.com/CyrilB1531/lodestar/commit/8aaa19a))

#### Added — ranking

- `Dcg`, `Ndcg` and `TopKAccuracy` score an ordered list of documents at scikit-learn parity, tie handling included: equal scores have their discounted gain averaged over the permutations of the tie by default, which on a row whose four scores are equal is `0.8069…` against `0.6138…` for `ignoreTies: true`. ([#173](https://github.com/CyrilB1531/lodestar/issues/173), [`8f3fda1`](https://github.com/CyrilB1531/lodestar/commit/8f3fda1))
- `ReciprocalRank` scores rankings by the position of their first relevant document — the one member of this package **not verified against a reference**, because `sklearn.metrics` has no counterpart to freeze; its definition is pinned by tests under [`docs/decisions/0036`](docs/decisions/0036-a-member-may-ship-without-an-oracle-if-it-says-so.md), which also says what would retire the exception. ([#173](https://github.com/CyrilB1531/lodestar/issues/173), [`8f3fda1`](https://github.com/CyrilB1531/lodestar/commit/8f3fda1))
- `CoverageError`, `LabelRankingLoss` and `LabelRankingAveragePrecision` score a boolean label matrix at scikit-learn parity, the two places the reference disagrees with itself included: a single label column is accepted by the average precision and refused by the other two, and a weight vector summing to zero gives `NaN` there where the other two raise. ([#201](https://github.com/CyrilB1531/lodestar/issues/201), [`eec79dd`](https://github.com/CyrilB1531/lodestar/commit/eec79dd))
- A sample with no relevant label contributes `0` to `CoverageError` rather than the label count, so its mean can sit below `1` — measured, `0.5` on two samples one of which is empty; a tie between a relevant and an irrelevant label counts as an error in `LabelRankingLoss`, so a sample whose scores are all equal scores `1`. ([#201](https://github.com/CyrilB1531/lodestar/issues/201), [`eec79dd`](https://github.com/CyrilB1531/lodestar/commit/eec79dd))
- `Dcg.Score`, `Ndcg.Score` and `TopKAccuracy.Score` take a `sampleWeight`, which the reference has always had and these three did not — three rows of `docs/equivalence.md` called them identical anyway. With weights `TopKAccuracy`'s `normalize: false` returns the **sum of the weights** of the hits rather than how many there are, measured `7.0` against the unweighted `3.0`, and because that path never divides it does not refuse a zero-sum vector at all, where the fraction does — what it returns there is the weighted sum of the hits, `3.0` on weights `[1, 1, 1, -3]` whose total is zero. ([#216](https://github.com/CyrilB1531/lodestar/issues/216), [`e2b62e3`](https://github.com/CyrilB1531/lodestar/commit/e2b62e3))

#### Changed

- **Faster, same answers.** `MeanSquaredError`, `MeanAbsoluteError` and `RootMeanSquaredError` accumulate through `Vector<double>` on `net10.0` when there is a single output, which is the rule [decision 0027](docs/decisions/0027-r2-and-explainedvariance-vectorize-only-a-single-output.md) already set for `R2` and `ExplainedVariance`: **1.65×** on `mse` and **1.60×** on `mae` at a million rows, with `r2` re-run as an untouched control. The lanes reduce in a different order from a scalar loop, so the values can differ in their last bits; the frozen scikit-learn corpora pass unchanged at their `1e-9` comparison. ([#321](https://github.com/CyrilB1531/lodestar/issues/321), [`36ec36e`](https://github.com/CyrilB1531/lodestar/commit/36ec36e))
- **Numerical change, under `1e-14`.** `NormalizedMutualInformation`, `Homogeneity`, `Completeness` and `VMeasure` return slightly different values on inputs where one labelling is a single cluster: the shared mutual-information term now zeroes each contribution below the machine epsilon before summing and returns `0.0` outright when either side has one label, both of which the reference does. The old values were up to `5.13e-15` from scikit-learn's and the new ones are exact, so anything comparing at the corpus tolerance of `1e-9` is unaffected — this is recorded because the values moved, not because a caller should have to react. ([#191](https://github.com/CyrilB1531/lodestar/issues/191), [`43b4368`](https://github.com/CyrilB1531/lodestar/commit/43b4368))

#### Fixed — ranking

- `Dcg.Score` refuses a `logBase` outside `(0, ∞)` instead of returning a silent `NaN`: zero, a negative, `NaN` and infinity now raise `ArgumentOutOfRangeException`, which is where `dcg_score` raises too. A base below `1` is still accepted, and still takes the score negative. ([#215](https://github.com/CyrilB1531/lodestar/issues/215), [`1eff5e5`](https://github.com/CyrilB1531/lodestar/commit/1eff5e5))

### Lodestar.Fuzzy — 0.4.0

#### Changed

- **`fuzz.ratio` and `process.extract` now require the kernels they were made faster by.** The floor on `Lodestar.Text` moves from `0.3.1` to `0.4.0`, so a caller who references only `Lodestar.Fuzzy` stops resolving a `Lodestar.Text` that predates #208, #320, #357 and #302. No source file changes; `Lodestar.Text 0.4.0` also refuses a `null` word in the phonetic encoders, which a consumer of both packages meets here. ([#415](https://github.com/CyrilB1531/lodestar/issues/415), [`8a1573c`](https://github.com/CyrilB1531/lodestar/commit/8a1573c))

## Released — 2026-08-16

The rename from `DataNet.*` to `Lodestar.*`, and the reference pages that went out
with it. Five tags were cut and none of them had a section here until the 0.4.0
release went looking for one; each entry below is filed under the tag its own commit
is an ancestor of.

### Lodestar.Text — 0.3.1

#### Added

- `docs/reference/text/distances.md` documents every type of `Lodestar.Text.Distances` in the layout of the .NET API reference, and a test checks each declaration, parameter list and `Applies to` against the assembly. ([#181](https://github.com/CyrilB1531/data.net/issues/181), [`754a61d`](https://github.com/CyrilB1531/lodestar/commit/754a61d))

#### Changed

- The package is `Lodestar.Text`, and its namespaces are `Lodestar.Text.*`. `DataNet.Text 0.3.0` and `Lodestar.Text 0.3.1` hold the same code: the id changed, nothing else did. ([#194](https://github.com/CyrilB1531/data.net/issues/194), [`3a9931a`](https://github.com/CyrilB1531/lodestar/commit/3a9931a))
- `DamerauLevenshtein`'s documented summary no longer says "Not a proper metric": unit-cost unrestricted Damerau-Levenshtein satisfies the triangle inequality and is a true metric; `Osa` is the one that does not. ([#181](https://github.com/CyrilB1531/data.net/issues/181), [`754a61d`](https://github.com/CyrilB1531/lodestar/commit/754a61d))
- The reference is one page per member, with a type page and a namespace index above it: `docs/reference/text/distances.md` becomes 9 type pages and 22 member pages, and the index a reader lands on is 64 lines rather than 1034. ([#189](https://github.com/CyrilB1531/data.net/issues/189), [`754a61d`](https://github.com/CyrilB1531/lodestar/commit/754a61d))

### Lodestar.Text — 0.3.2

#### Changed

- The toolkit is `Lodestar`: the tags no longer say `datanet`, and every package carries an embedded icon rather than none. ([#194](https://github.com/CyrilB1531/data.net/issues/194), [`ec421f6`](https://github.com/CyrilB1531/lodestar/commit/ec421f6))

### Lodestar.Embeddings — 0.3.1

#### Changed

- The package is `Lodestar.Embeddings`, and its namespaces are `Lodestar.Embeddings.*`. `Lodestar.Embeddings 0.3.1` holds the same code as `DataNet.Embeddings 0.3.0`. ([#194](https://github.com/CyrilB1531/data.net/issues/194), [`b2911a5`](https://github.com/CyrilB1531/lodestar/commit/b2911a5))

### Lodestar.Fuzzy — 0.3.1

#### Changed

- The package is `Lodestar.Fuzzy`, and its namespaces are `Lodestar.Fuzzy.*`. `Lodestar.Fuzzy 0.3.1` holds the same code as `DataNet.Fuzzy 0.3.0`, and its floor names `Lodestar.Text 0.3.1`. ([#194](https://github.com/CyrilB1531/data.net/issues/194), [`b2911a5`](https://github.com/CyrilB1531/lodestar/commit/b2911a5))

### Lodestar.Metrics — 0.2.0

#### Added

- `docs/reference/metrics/classification.md` and `docs/reference/metrics/regression.md` document every type of `Lodestar.Metrics` in the layout of the .NET API reference, and the same test checks each declaration, parameter list and `Applies to` against the assembly. ([#181](https://github.com/CyrilB1531/data.net/issues/181), [`754a61d`](https://github.com/CyrilB1531/lodestar/commit/754a61d))

#### Added — clustering

- `AdjustedRand`, `NormalizedMutualInformation`, `Homogeneity`, `Completeness` and `VMeasure` score a clustering against a reference partition at scikit-learn parity, degenerate cases included: an empty input and a single sample both score `1`, and two independent partitions score `-0.5` on adjusted Rand. ([#172](https://github.com/CyrilB1531/data.net/issues/172), [`3d10674`](https://github.com/CyrilB1531/lodestar/commit/3d10674))
- `Silhouette` scores a clustering with no reference partition, from the samples with the euclidean distance or from a distance matrix already computed, per sample or as their mean. ([#172](https://github.com/CyrilB1531/data.net/issues/172), [`714dd80`](https://github.com/CyrilB1531/lodestar/commit/714dd80))

#### Changed

- The reference is one page per member, with a type page and a namespace index above it: the two documents above become 31 type pages and 42 member pages, and the index a reader lands on is 102 lines rather than 1646. ([#189](https://github.com/CyrilB1531/data.net/issues/189), [`754a61d`](https://github.com/CyrilB1531/lodestar/commit/754a61d))
- The package is `Lodestar.Metrics`, and its namespaces are `Lodestar.Metrics.*`. ([#194](https://github.com/CyrilB1531/data.net/issues/194), [`b2911a5`](https://github.com/CyrilB1531/lodestar/commit/b2911a5))

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
