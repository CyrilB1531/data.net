# BPE and byte-level BPE tokenizers — design

**Issue:** [#59](https://github.com/CyrilB1531/data.net/issues/59) ·
**Date:** 2026-08-06 · **Package:** `DataNet.Embeddings`

## Problem

`DataNet.Embeddings` ships WordPiece and SentencePiece unigram. Between them they
cover the BERT family, T5, ALBERT, camemBERT and XLM-R — every one of them an
encoder. A caller who reaches for DataNet to tokenize for a decoder model finds
nothing, because GPT-2, Llama, Qwen, Phi and Gemma all tokenize with byte-pair
encoding.

This work adds the merge algorithm and its byte-level variant.

## What is actually being claimed

The three model families the issue title names do not share one pipeline, and the
difference decides what this design can honestly promise.

| Family | Pipeline | Covered here |
| --- | --- | --- |
| GPT-2, and anything reusing its pipeline | byte-level BPE, GPT-2 split pattern | **Yes**, end-to-end, oracle-proven against the real 50 257-entry vocabulary |
| Llama-3, Qwen2 | byte-level BPE, own split pattern, `ignore_merges` | **Pattern and merge engine**, oracle-proven at the split level; bring your own vocabulary |
| Llama-2, Mistral v0.1 | SentencePiece BPE with `byte_fallback` and `Metaspace` | **No** — refused with a message naming the reason |
| `subword-nmt` lineage | character-level BPE with `end_of_word_suffix` | **Yes**, oracle-proven against a small self-trained model |

The Llama-3 / Qwen2 row is a deliberate trade. Proving those two end-to-end means
committing a second and third real vocabulary — Qwen2 alone is 151 000 entries,
roughly 4 MB of fixtures — to prove a merge engine that the GPT-2 corpus already
proves. What is *not* shared with GPT-2 is the split pattern, and that is proven
directly: the oracle generator builds a HuggingFace byte-level BPE over the GPT-2
vocabulary with the Llama-3 and then the Qwen2 `Split` pre-tokenizer, and the C#
side replays both byte for byte.

The Llama-2 / Mistral row is the same kind of limit ADR 0013 recorded for
SentencePiece, and it gets the same treatment: refused explicitly, named in the
guide, and written down in an ADR rather than rediscovered.

## Scope decisions taken before design

Recorded here because each of them closed an alternative that a reader will
otherwise reopen.

- **One pull request** for the whole issue, per the repository's one-issue-one-PR
  convention.
- **Two fixture sets**, mirroring what SentencePiece already does with
  `tiny_sp.model` and `xlmr_fairseq.model`: a small self-trained model isolates
  the merge machinery, a real vocabulary proves parity.
- **One tokenizer class**, not two. HuggingFace has a single `BPE` model and
  composes a pre-tokenizer and a decoder around it; `tokenizer.json` has the same
  shape, so a loader that had to choose between two C# classes would be inventing
  a distinction its input does not make.
- **`Decode` on `BpeTokenizer` only.** `netstandard2.0` has no default interface
  members, so adding it to `ISubwordTokenizer` is a binary break, and it would
  force a `Decode` onto WordPiece — whose decode is lossy — and onto
  SentencePiece, neither of which is in scope. An `IDetokenizer` interface with
  one implementer buys nothing today and can be added later without a break.
- **No shared merge cache.** HuggingFace keeps a bounded per-tokenizer cache, and
  it is the single largest lever available. It is also mutable shared state, which
  turns "thread-safe after construction" from a consequence of immutability into
  a claim needing proof. `CONTRIBUTING.md` asks for measured numbers rather than
  asserted ones; the benchmark lands in this PR, and a cache is added only if it
  shows the target is missed.

## Public API

Five new public types.

```csharp
/// One line of the merge table. Its index in BpeVocabulary.Merges is its rank.
public readonly record struct MergePair(string Left, string Right);

public sealed class BpeVocabulary
{
    public IReadOnlyDictionary<string, int> Vocab { get; }
    public IReadOnlyList<MergePair> Merges { get; }          // index == rank
    public IReadOnlyDictionary<string, int> AddedTokens { get; }
    public bool ByteLevel { get; }
    public bool AddPrefixSpace { get; }
    public bool IgnoreMerges { get; }
    public int SkippedMerges { get; }
    public string? EndOfWordSuffix { get; }
    public string? ContinuingSubwordPrefix { get; }
    public string? UnkToken { get; }
    public string? PreTokenizerPattern { get; }
}

public sealed class BpeTokenizer : ISubwordTokenizer
{
    public BpeTokenizer(BpeVocabulary vocabulary);
    public TokenizationResult Encode(string text);
    public bool TryGetId(string token, out int id);
    public string Decode(IReadOnlyList<int> ids, bool skipSpecialTokens = false);
    public string Decode(ReadOnlySpan<int> ids, bool skipSpecialTokens = false);
}

public static class BpePatterns
{
    // static readonly, not const: see the packaging-gate note below.
    public static string Gpt2 { get; }   = @"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+";
    public static string Llama3 { get; } // transcribed from the model's tokenizer.json
    public static string Qwen2 { get; }  // transcribed from the model's tokenizer.json
}

public static class BpeFilesLoader
{
    public static BpeVocabulary Load(Stream vocabJson, Stream merges, ArtifactLoadOptions? options = null, bool byteLevel = true);
    public static BpeVocabulary Load(string vocabJsonPath, string mergesPath, ArtifactLoadOptions? options = null, bool byteLevel = true);
    public static Task<BpeVocabulary> LoadAsync(Stream vocabJson, Stream merges, ArtifactLoadOptions? options = null, bool byteLevel = true, CancellationToken cancellationToken = default);
}
```

`BpePatterns` exposes **properties, not `const` fields**. A `const string` is a
compile-time constant, so a consumer referencing it emits no
`MemberReference` — the exact reason `PackagingGate` documents enums as its
blind spot. A `const` here would be a public member the gate could never see.

`BpeVocabulary` is what the loaders produce and what the tokenizer consumes,
exactly as `WordPieceVocabulary` and `SentencePieceVocabulary` are. It decides
nothing: it restates what the file declared.

The GPT-2 pattern above is the one the issue quotes. The Llama-3 and Qwen2
patterns are transcribed from each model's own `tokenizer.json` at implementation
time, read from the published file rather than recalled — they differ from GPT-2
in newline handling and in the case-insensitivity of the contraction group, and
from each other only in a quantifier on `\p{N}`. Each is compiled once with
`RegexDefaults.MatchTimeout`, so a caller-supplied pattern fails rather than
hanging the calling thread — the ReDoS hole this repository closed everywhere
else.

`SkippedMerges` exists because a merge pair naming a token absent from the
vocabulary is a real condition that HuggingFace tolerates. Throwing would be
wrong; dropping it silently would be worse. It is counted at load time, which is
where it is decided, and exposed on the vocabulary rather than on the tokenizer.

## Internal design

### `ByteLevelAlphabet`

The 256-entry byte↔unicode table, and its inverse. **Constructed from the
published rule** — the three printable ranges, then the remaining bytes mapped to
`256 + i` in order — rather than transcribed from a source listing. That satisfies
the provenance rule in `CONTRIBUTING.md`, and it makes a transcription slip
impossible; the oracle proves the resulting table is the right one.

### The merge engine

The issue prescribes `FrozenDictionary` with
`GetAlternateLookup<ReadOnlySpan<char>>()`, to avoid allocating a `string` per
candidate lookup. This design goes further: **no string lookup happens inside the
merge loop at all.**

At load time every merge pair is resolved to a pair of vocabulary ids, and the
table becomes `Dictionary<long, MergeEntry>` keyed by
`((long)left << 32) | (uint)right`. The merge loop then manipulates nothing but
`int`s in a rented buffer. This removes the allocation rather than optimising it,
behaves identically on `netstandard2.0` — `FrozenDictionary` needs .NET 8 — and is
the representation the reference implementation uses.

String lookups survive in two cold places: resolving the initial symbols (a single
character, or a character plus `end_of_word_suffix`) and `TryGetId`. Those are
where `FrozenDictionary` plus `GetAlternateLookup` earns its keep, under
`#if NET9_0_OR_GREATER`, following `StopWordSet`.

**Merge order.** Direct scan over the symbol buffer: find the adjacent pair of
lowest rank, merge it, repeat. That is `O(n²)` in the worst case, but `n` is the
length of one pre-tokenized piece — the split pattern cuts at every Unicode class
boundary, so it is a handful of symbols in practice, and on a reused buffer the
constant is tiny. A heap would cost an allocation or a reset per word, which is
the very thing the issue says not to repeat. A long-word arm using a priority
queue is added **only if the benchmark, including a deliberately pathological long
token, shows the direct scan misses the target**.

### Buffers

`stackalloc` below a threshold, `ArrayPool<T>.Shared` above it — the pattern
already in `Myers` and `SetSimilarity`, and one that works on both targets.
Nothing is retained between calls, so the tokenizer stays immutable after
construction and therefore thread-safe without a lock.

## Data flow

### `Encode(text)`

1. Prepend a space if `AddPrefixSpace`.
2. Split with the model's pattern. Each match is one piece, tokenized
   independently; this is what bounds word length and makes the direct scan
   viable.
3. If `IgnoreMerges`, look the whole piece up in the vocabulary first and emit it
   when found. This is what Llama-3 declares.
4. Build the initial symbols in a rented buffer:
   - **byte-level** — encode the piece as UTF-8, map each byte through
     `ByteLevelAlphabet` to one character. One byte, one symbol: a four-byte emoji
     enters the merge loop as four symbols, and that is where reversibility comes
     from.
   - **classic** — one character per symbol, `end_of_word_suffix` appended to the
     last.
5. Resolve each symbol to an id. An absent symbol becomes `UnkToken`. In
   byte-level this cannot happen — all 256 alphabet characters are vocabulary
   entries — and if it does, the vocabulary is broken and the exception says so.
6. Merge to fixpoint.
7. Emit. Ids come out of the buffer; token strings come from a `string[]` reverse
   table indexed by id.

### `Decode(ids, skipSpecialTokens = false)`

Ids become strings through the reverse table and are concatenated. In byte-level,
each character goes back through the inverse alphabet to a byte and the byte
sequence is decoded as UTF-8. In classic, `end_of_word_suffix` becomes a space.

The round trip is exact by default. HuggingFace decodes with
`skip_special_tokens=True`; the default is inverted here deliberately, because a
`Decode` that silently drops tokens makes `Decode(Encode(x)) == x` a lie in
exactly the case a caller would use to check it. Both modes are frozen in the
oracle.

## Loading

### `TokenizerJsonLoader.LoadBpe`

Alongside `LoadWordPiece` and `LoadUnigram`, with the same three overloads each
of those has — `Stream`, `string path`, and an `Async` counterpart. Reads
`model.type == "BPE"`, the
vocabulary, and the merge table in both encodings found in the wild — `"a b"`
lines and `["a", "b"]` pairs. Three flags decide behaviour:

- **`byte_fallback: true` → refused**, with a message naming the pipeline it
  belongs to (Llama-2 / Mistral v0.1) and the fact that neither tokenizer in this
  package reproduces it. Same shape as the existing `EnsureByteFallbackIsOff`.
- **`ignore_merges: true` → implemented**, not refused. It costs five lines and
  refusing it would put Llama-3 out of reach for no defensible reason.
- **`pre_tokenizer`** — two shapes accepted: GPT-2's `ByteLevel` (with
  `add_prefix_space` and `use_regex`), and Llama-3 / Qwen2's
  `Sequence[Split(pattern, "Isolated"), ByteLevel(add_prefix_space: false)]`.
  Anything else is refused naming what was found, as the loader already does.

The `decoder` declaration is checked for consistency with the model rather than
ignored: a `ByteLevel` model with a `BPEDecoder` is a file that will not round
trip, and saying so at load time beats corrupt text at decode time.

### `BpeFilesLoader`

The `vocab.json` + `merges.txt` pair, the pre-`tokenizer.json` GPT-2 layout. The
leading `#version:` comment line is skipped. Bounded by `ArtifactLoadOptions`
like every other loader.

## Errors

| Condition | Behaviour |
| --- | --- |
| Merge pair names a token absent from the vocabulary | Skipped, counted in `SkippedMerges` |
| `UnkToken` declared but absent from the vocabulary | `ArgumentException`, as `WordPieceTokenizer` |
| `byte_fallback: true` | `InvalidDataException` naming the pipeline |
| Unrecognized pre-tokenizer, normalizer or decoder | `InvalidDataException` naming what was found |
| Caller-supplied pattern that backtracks | `RegexMatchTimeoutException` after `RegexDefaults.MatchTimeout` |
| Byte-level vocabulary missing an alphabet character | `ArgumentException` — the vocabulary is not byte-level |

## Oracle validation

### Committed fixtures

- `tests/oracles/gpt2_vocab.json` and `tests/oracles/gpt2_merges.txt`, about
  1.5 MB, fetched by a new `tools/fetch_gpt2_bpe.py` against a pinned SHA-256,
  with a `--check` mode wired into the CI job that already verifies the stop-word
  lists. GPT-2 is MIT; attribution goes in `NOTICE` and
  `THIRD-PARTY-NOTICES.md`. The vocabulary only — never the weights, per ADR 0003.
- `tests/oracles/tiny_bpe.json`, a character-level BPE with
  `end_of_word_suffix="</w>"`, trained by `tools/build_tiny_models.py`.

### Generated corpora

Added as generator sections in `tools/generate_oracles.py`:

- `bpe.json` — classic BPE over the small model: tokens and ids.
- `bytelevel_bpe.json` — GPT-2: tokens, ids, and decode in both modes. Cases:
  ASCII; Latin-1 accented text; CJK; emoji; leading, trailing and repeated
  whitespace; text containing the special-token strings literally; the empty
  string; a single space.
- `bpe_pretokenize.json` — the three patterns over the GPT-2 vocabulary, proving
  the split matches HuggingFace byte for byte.
- `bpe_tokenizer_json.json` — the loader paths, `ignore_merges` included.

### The failure mode worth naming

Both `*.Tests.csproj` copy `tests/oracles/**` filtered to `*.json`, `*.onnx` and
`*.model`. **`merges.txt` is copied by neither.** The `*.txt` glob has to be added
to both projects, or the netstandard suite fails on a missing file — or worse,
skips a test that looks green.

Tests live in `tests/DataNet.Embeddings.Tests/`, so the netstandard suite picks
them up automatically through its source glob.

## Acceptance

- Byte-exact parity with HuggingFace `tokenizers` over the whole corpus, tokens
  and ids, exact comparison.
- `Decode(Encode(x)) == x` for every case, CJK and emoji included.
- Both `net10.0` and `netstandard2.0` build and pass.
- `BpeBenchmarks.cs` in `bench/DataNet.Text.Benchmarks`, `[MemoryDiagnoser]`,
  against `SentencePieceTokenizer` on the same input; before/after figures and the
  machine named in the PR.
- Rows in `docs/equivalence.md` naming the Python call each entry matches.
- A "which tokenizer for which model family" section in
  `docs/guides/embeddings.md`, stating the Llama-2 / Mistral limit plainly.
- `docs/decisions/0016-bpe-parity-scope.md`, in the shape of ADR 0013.
- The five public types exercised in `samples/DataNet.Sample/Lot3Embeddings.cs`
  with a member reference each, or the packaging gate breaks CI.
- A `CHANGELOG.md` entry under `DataNet.Embeddings — 0.3.0`.
- XML documentation naming the reference implementation across the public surface.

## Out of scope

- BPE **training**. This is an inference library.
- The existing allocations in `WordPieceTokenizer` and `SentencePieceTokenizer`,
  tracked separately.
- `Decode` on the other two tokenizers.
- WordLevel and the remaining `tokenizers` model types.
