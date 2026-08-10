# Persisting the `EmbeddingIndex` — design

**Issue:** [#62](https://github.com/CyrilB1531/data.net/issues/62) ·
**Date:** 2026-08-06 · **Package:** `DataNet.Embeddings`

## Problem

`EmbeddingIndex` is the last stage of the advertised chain — tokenize → infer →
pool → index → query — and the stage where the expensive work accumulates.
Building it runs an ONNX encoder over every document in the corpus: seconds for a
demo, hours for anything real. That work dies with the process, because the index
has no I/O.

The state is small and fully defined: a contiguous `float[]` of `count ×
dimension`, the dimension, the normalization flag, and the count. Persisting it
is what this work does.

## Format: the artifact of ADR 0011, not a second one

The issue text argues for a dedicated binary format, and it was written before
[ADR 0011](../../decisions/0011-persistence-format.md) landed with #58. The ADR
answers the question already, in two places:

- *Why not a binary format as well* — the benchmark #58 demanded was run, it did
  find a cost, and the answer was to move the numeric vector to base64 inside the
  JSON rather than to introduce a second format. "Two formats to keep bit-exact
  remains the cost this avoids."
- The last line of its *Consequences* — "A second artifact type is cheap to add:
  `EmbeddingIndex` persistence is a body writer and a body reader on top of this
  header."

The idf vector is **already** a base64 block of raw little-endian IEEE-754 bits
inside a JSON document. That is precisely the shape the vector block needs, and
it satisfies every constraint the issue sets: exact by construction, no
hand-written parser on the untrusted-input path, zero new dependencies, no
`BinaryFormatter`.

What a dedicated binary format would still buy, and what this design gives up
with its eyes open: base64 costs 33% in size, and the reader buffers the whole
payload before decoding, so peak memory is roughly 2.3× the index rather than 1×.
Section *Benchmark* below measures both against `numpy.save`, which — being a raw
memory dump with a header — **is** the binary floor. No new ADR: this work
executes 0011's recorded decision, it does not amend it.

## Public API

`EmbeddingIndex` becomes `partial`; the new members live in
`src/DataNet.Embeddings/Search/EmbeddingIndex.Persistence.cs`, the split
`TfidfVectorizer` already uses.

```csharp
public void Add(ReadOnlySpan<float> vector, string? id);
public string? GetId(int index);
public bool HasIds { get; }

public void Save(Stream destination);
public void Save(string path);
public Task SaveAsync(Stream destination, CancellationToken cancellationToken = default);
public static EmbeddingIndex Load(Stream source, ArtifactLoadOptions? options = null);
public static EmbeddingIndex Load(string path, ArtifactLoadOptions? options = null);
public static Task<EmbeddingIndex> LoadAsync(Stream source, ArtifactLoadOptions? options = null, CancellationToken cancellationToken = default);
```

The overload set is the one ADR 0011 records under *API shape*, so a user who has
learned one artifact has learned this one. `Load` is static, a stream passed in is
never disposed, and the `path` overloads own the `FileStream` they open.

### Why `Add` gains an overload rather than an optional parameter

Adding `string? id = null` to the existing `Add(ReadOnlySpan<float>)` is source
compatible and **binary breaking**: the signature changes, and every compiled
caller breaks. A second method costs one line and breaks nothing.

`Add(vector, null)` is exactly equivalent to `Add(vector)` — a null id does not
materialize the id array and does not set `HasIds`.

### Why the id is not on `SearchResult`

The obvious shape is `SearchResult(int Index, float Score, string? Id)`, so a hit
carries its own id. It is refused for a measured reason: `Search` allocates a
`SearchResult[Count]`, scores into it and sorts it. `SearchResult` is 8 bytes
today, a struct the GC never has to look inside. Adding a reference doubles it
and turns the hot scoring array into one the collector must scan and the sort must
move references through — on the one code path an exhaustive index exists to make
fast. `GetId(int)` on the index costs the caller one call and the library nothing.

`GetId` throws `ArgumentOutOfRangeException` outside `[0, Count)` and returns
`null` when the index carries no id at that position.

There is no `Ids` collection property. Rebuilding a caller-side map is a
three-line loop over `Count`, and exposing a list would mean wrapping an array
that is either absent or oversized by the growth policy.

## Internal moves

Two internal relocations, without which this work would duplicate reviewed code
that guards a security boundary. Both types are `internal`; the published surface
does not change.

| File | Today | After |
| --- | --- | --- |
| `ArtifactIo.cs` | `src/DataNet.Text/Persistence/`, namespace `DataNet.Text.Persistence` | `src/Shared/Persistence/`, namespace `DataNet.Internal.Persistence`, compiled into both packages that ship artifacts |
| base64 block | the bound-before-decode logic inlined in `FeatureVocabularyJson.WriteIdf`/`ReadIdf` | `src/Shared/Persistence/Base64Numbers.cs`, `double` and `float` variants; `FeatureVocabularyJson` delegates |

`ArtifactIo` is added to the `DataNetIncludesPersistence` item group in
`src/Directory.Build.props`, beside `ArtifactHeader`, `ArtifactLimits` and
`JsonArtifact`. Three call sites in `DataNet.Text` update their `using`; all three
are covered by existing tests.

This is deliberately *not* the same call as `ArtifactLoadOptions`, which ADR 0011
duplicates per package on purpose. That type is public, and one public type
compiled into two assemblies is an ambiguous reference for anyone consuming both.
`ArtifactIo` and `Base64Numbers` are internal, so sharing them has none of that
cost.

## The artifact

```json
{"$schema":"datanet/embedding-index","version":1,
 "dimension":384,"normalize":true,"count":3,
 "ids":["doc-1","",null],
 "vectors":"<base64: count × dimension little-endian float32>"}
```

`count` is written **before** `vectors`, the way `featureCount` precedes
`vocabulary`, so a reader sizes its buffer from a value it has already bounded.
The reader still accepts reordered properties — a hand-edited file is a supported
input — and checks the declared count against what actually arrived, at the end.

`ids` is **absent** when no id was ever supplied. The presence of the property
*is* the metadata-presence flag the issue asks for. When present it holds exactly
`count` entries, each a string or JSON `null`.

Only `_length` floats are written, never the capacity `_data` has grown to.

### Load semantics

Vectors are restored **verbatim** into the internal buffer, not replayed through
`Add`. That is the condition of bit-exactness: they were already normalized on
insertion, and normalizing them a second time would change their bits.

`normalize` comes from the file and is never supplied by the caller. It drives
query-side normalization on the reloaded index, so a file written with
`normalize: false` cannot silently start normalizing, and one written with
`normalize: true` cannot silently stop.

### What the reader refuses

| Case | Exception |
| --- | --- |
| `$schema` missing, or naming another artifact | `InvalidDataException` from the existing `ArtifactHeader` |
| `version` missing, `< 1`, or `> 1` | `InvalidDataException`, naming the supported range |
| `dimension` missing, `≤ 0` | `JsonArtifact.Inconsistent`, value named |
| `count` missing, `< 0`, or over `MaxArrayLength` | `Inconsistent` / the limit, named with the offending value |
| `vectors` base64 run too long | bounded **before** decoding, as `ReadIdf` does — checking only the decoded length would let the limit be satisfied by the allocation it exists to prevent |
| `vectors` not valid base64 | `Inconsistent` |
| decoded byte count not a multiple of 4 | `Inconsistent`, byte count named |
| decoded float count ≠ `count × dimension` | `Inconsistent`, both numbers named |
| `ids` length ≠ `count` | `Inconsistent`, both numbers named |
| an id longer than `MaxTokenLength` | the limit, named |
| a non-finite value in `vectors` | `Inconsistent`, item and component named |
| unknown property | `JsonArtifact.UnknownProperty` |
| truncated input, or content after the closing brace | `ArtifactIo.Malformed` / `EnsureEndOfDocument` |

Every count read from the file sizes a buffer, and every one is checked against
`ArtifactLoadOptions` before it is used. Exceeding a limit is
`InvalidDataException`, never `OutOfMemoryException`.

### Non-finite values

`Save` and `Load` both refuse `NaN` and infinity, naming the item and the
component. This is the rule `WriteIdf`/`ReadIdf` already apply, and one doctrine
in one format is worth more than a per-artifact exception.

The consequence is stated rather than hidden: **`Save` refuses data that `Add`
accepted**. `Add` stays permissive, because an index is not necessarily saved and
changing the behaviour of a published method is not this issue's business. The
XML documentation on `Save` says so.

The argument is weaker here than it is for idf — a `NaN` idf weight poisons every
column of every document, where a `NaN` row poisons only its own score, and .NET's
sort places it last deterministically — which is exactly why it is written down
here and in the code rather than left for a reviewer to rediscover.

## Tests

Two files under `tests/DataNet.Embeddings.Tests/Persistence/`, modelled on what
`DataNet.Text` already has there.

**`EmbeddingIndexPersistenceTests.cs`** — the round trip and its guarantees:

- `add → save → load → search` returns identical `Index` values and **bitwise
  equal** `Score` values (`BitConverter.SingleToInt32Bits`, no tolerance).
- `Count`, `Dimension` and the normalization flag survive and are asserted.
- An empty index (`Count == 0`) round-trips.
- Ids round-trip: non-ASCII, empty string, `null`, and a mix of ids and nulls in
  one index. An index with no ids at all loads with `HasIds == false`.
- A `normalize: false` index built from deliberately un-normalized vectors comes
  back un-normalized — the assertion that no silent renormalization happens — and
  the same corpus saved under each flag produces two files that load differently.
- All six overloads, including that a caller's stream is left open and positioned
  as the caller expects.
- `GetId` bounds, and `Save` refusing a non-finite component.

**`EmbeddingIndexHardeningTests.cs`** — one test per row of the refusal table
above, building the malformed JSON by hand, the way `ArtifactHardeningTests.cs`
does.

## Documentation

- `docs/guides/embeddings.md` — a save/load snippet in *Index a corpus and query
  it*. `tools/extract_doc_snippets.py` compiles every C# fence in the guide into
  `samples/DataNet.DocSnippets/Generated/`, so the snippet has to compile against
  `SnippetContext`, not merely read well.
- `docs/equivalence.md` — rows for `numpy.save`/`np.load` and
  `faiss.write_index`/`read_index` against `EmbeddingIndex.Save`/`Load`.
- `CHANGELOG.md` under the unreleased `DataNet.Embeddings — 0.3.0`. No version
  bump: 0.3.0 is unreleased and already a minor for added public API, which is
  what this is.
- `samples/DataNet.Sample/Lot3Embeddings.cs` — a `MemoryStream` round trip after
  the index it already builds. No new public type, so `PackagingGate` does not
  demand it; three lines put the path under the CI job that runs against the
  *packaged* assemblies.
- No ADR. 0011 already records this decision and predicted this artifact.

## Benchmark

The question the benchmark answers is the one this design consciously left open:
what does JSON + base64 cost against a raw binary dump, in time, in allocations
and in bytes on disk.

**Intra-C#** — `bench/DataNet.Text.Benchmarks/PersistenceBenchmarks.cs` gains
`EmbeddingIndexSave` and `EmbeddingIndexLoad` under the existing
`[MemoryDiagnoser]`, on an index of a realistic shape (10 000 × 384). Vectors are
generated deterministically from a seeded LCG rather than committed or read from
disk: I/O timing depends on the size of the block, not its contents, and both
language sides can reproduce the same bytes from the same seed without a corpus
file. Run on both targets, `net10.0` and `netstandard2.0`, as §4 does.

**Cross-language** — `bench/python/bench_persistence.py` gains
`embedding_index_save` and `embedding_index_load` over `numpy.save`/`np.load` on
the same 10 000 × 384 array, and the C# `compare-persistence` mode gains the
matching pair. A `.npy` file is a small header followed by the raw little-endian
block: it is what a dedicated binary format would produce, which makes the ratio a
direct measurement of the format choice rather than an analogy.

`faiss` is not added. On a flat index it writes the same raw block, so it would
cost a pinned development dependency to measure the same floor twice.

Results go into a new §5 of `bench/README.md`, following §4's conventions: both
sides run back to back on an idle machine from a single run, processor time
reported beside elapsed time, and the corpus generator named by any harness that
cannot find its inputs.

## Out of scope

**HNSW, explicitly.** The decision in `docs/guides/embeddings.md` stands: the
search is an exhaustive SIMD-vectorized cosine, the right default up to a few
hundred thousand vectors, and an approximate index is worth adding only once a
real need is demonstrated. Persisting an exhaustive index and changing the index
structure are different concerns; mixing them would break the one-branch-one-
concern rule in `CONTRIBUTING.md`. If HNSW is ever justified it gets its own issue
and its own `docs/decisions/` entry.
