# Semantic search with embeddings

`Lodestar.Embeddings` covers the full chain: **tokenize → infer (ONNX) → pool →
index → query**. ONNX Runtime is isolated here, so the distance and
vectorization packages take no native dependency.

```bash
dotnet add package Lodestar.Embeddings
```

## Sub-word tokenization

Three tokenizers, depending on the model family — and in every case the
vocabulary is **read from the file the model ships with**, never assembled by
hand.

**WordPiece** (BERT), from a `vocab.txt` or a `tokenizer.json`:

```csharp
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

WordPieceVocabulary vocab = VocabTxtLoader.Load("bert-base-uncased/vocab.txt", lowercase: true);
var wp = new WordPieceTokenizer(vocab);
TokenizationResult t = wp.Encode("playing");   // pieces: play ##ing
```

A stock HuggingFace BERT `tokenizer.json` — `BertPreTokenizer` plus a full
`BertNormalizer` — **is refused** by [`TokenizerJsonLoader.LoadWordPiece`](../reference/embeddings/persistence/tokenizerjsonloader-loadwordpiece.md); that is
the correct outcome, not a gap, since Lodestar does not reproduce those steps.
`VocabTxtLoader` is the route for BERT, and `LoadWordPiece` is for a `tokenizer.json`
whose pipeline already matches Lodestar's own (see
[Models that are refused](#models-that-are-refused)).

**SentencePiece** (ALBERT, T5, camemBERT, XLM-R) — unigram Viterbi segmentation,
from the trained `spiece.model`. The model's own `precompiled_charsmap` is
applied before segmentation, so a stock file — all four families ship `nmt_nfkc`
— tokenizes here as it does in Python:

```csharp
SentencePieceVocabulary vocab = SentencePieceModelLoader.Load("spiece.model");
var sp = new SentencePieceTokenizer(vocab);
TokenizationResult t = sp.Encode("the quick brown fox");
```

**BPE** (GPT-2 and its byte-level descendants) — lowest-ranked-merge-first over a
`vocab.json` + `merges.txt` pair or a `tokenizer.json`. The byte-level variant is
lossless over any well-formed `string`, valid UTF-8 or not, because every byte of
the input becomes one symbol before merging starts:

```csharp
BpeVocabulary vocab = BpeFilesLoader.Load("gpt2/vocab.json", "gpt2/merges.txt");
var bpe = new BpeTokenizer(vocab);
TokenizationResult t = bpe.Encode("Hello, world! 🎉");
string back = bpe.Decode(t.Ids);   // == "Hello, world! 🎉", byte for byte
```

A `tokenizer.json` that declares a normalizer — `NFC`, `NFKC`, `NFD`, `NFKD`, or a
`Sequence` of those — has it applied before encoding, not after decoding. `Decode`
applies no normalizer of its own, but since `Encode` already normalized the text it
saw, `Decode(Encode(x))` returns the normalized text rather than `x`, matching Python.
One case does not round-trip byte-exactly: a non-ASCII added token that is not
byte-level encodable end to end decodes to U+FFFD, matching HuggingFace rather than
throwing — [decision 0023](../decisions/0023-byte-level-decode-substitutes.md). That is
also what makes decoding one token id at a time work, the normal way to consume a
streamed model.

See [Which tokenizer for which model family](#which-tokenizer-for-which-model-family)
for the family-to-class mapping, including the one family this package refuses
outright.

The loaders are what make the second and third examples correct rather than
merely short: `spiece.model` records the *type* of every piece, so the
tokenizer knows which entries are control markers instead of inferring it from
their ids, and the BPE loaders read `ignore_merges`, the split pattern together
with its `behavior` and `invert` flag, and the byte-level flag straight from
the model rather than asking the caller to get them right. See
[loading vocabularies](#loading-vocabularies) for
`tokenizer.json`, for the limits applied to untrusted files, and for which
models are refused outright.

> The tokenization must match the model's **exactly**, otherwise the embeddings
> are wrong (§5 of the brief). All three tokenizers are validated token-for-token
> against HuggingFace `tokenizers` / the `sentencepiece` library, and so are the
> four loaders.

## Which tokenizer for which model family

| Family | Class | How to load |
| --- | --- | --- |
| BERT, DistilBERT, and the WordPiece family | `WordPieceTokenizer` | `VocabTxtLoader` or [`TokenizerJsonLoader.LoadWordPiece`](../reference/embeddings/persistence/tokenizerjsonloader-loadwordpiece.md) |
| T5, ALBERT, camemBERT, XLM-R | `SentencePieceTokenizer` | `SentencePieceModelLoader` or [`TokenizerJsonLoader.LoadUnigram`](../reference/embeddings/persistence/tokenizerjsonloader-loadunigram.md) |
| GPT-2 and its byte-level descendants | `BpeTokenizer` | `BpeFilesLoader` or [`TokenizerJsonLoader.LoadBpe`](../reference/embeddings/persistence/tokenizerjsonloader-loadbpe.md) |
| Llama-3, Qwen2 | `BpeTokenizer` with `BpePatterns.Llama3` / `BpePatterns.Qwen2` | [`TokenizerJsonLoader.LoadBpe`](../reference/embeddings/persistence/tokenizerjsonloader-loadbpe.md) |
| Llama-2, Mistral v0.1 | `BpeTokenizer` (the SentencePiece-BPE lineage) | [`TokenizerJsonLoader.LoadBpe`](../reference/embeddings/persistence/tokenizerjsonloader-loadbpe.md) |

Llama-2 and Mistral v0.1 are trained as **SentencePiece BPE with a `Metaspace`
whitespace escape and `byte_fallback`** — not a third pipeline: `model.type` still
says `BPE`, and `BpeTokenizer` reproduces the whole lineage, the whitespace escape
([decision 0050](../decisions/0050-the-sentencepiece-bpe-lineage-stays-a-bpe-model.md),
[decision 0062](../decisions/0062-the-two-metaspace-spellings-part-on-the-prepend-twice.md))
and `byte_fallback`
([decision 0063](../decisions/0063-byte-fallback-requires-the-whole-alphabet-and-its-decoder-is-read-strictly-too.md))
both included. A real Llama-2 or Mistral v0.1 `tokenizer.json` declares
`model.type == "BPE"` with `byte_fallback`, and
[`TokenizerJsonLoader.LoadBpe`](../reference/embeddings/persistence/tokenizerjsonloader-loadbpe.md)
loads it. `LoadUnigram` still **refuses it by name**, unconditionally — the
Unigram pipeline does not reproduce `byte_fallback` at all, on any file, and the
message names `byte_fallback` directly rather than stopping at "this is a `BPE`
model, not `Unigram`" (#343) — and `LoadBpe` itself refuses only a vocabulary
that declares the flag without carrying every `<0xXX>` piece it promises, naming
the first one missing.
See [decision 0017](../decisions/0017-bpe-parity-scope.md) for the parity scope
this table states — end-to-end for GPT-2 and the classic lineage, split-pattern
only for Llama-3 and Qwen2 — and for a known split divergence from HuggingFace
above the Basic Multilingual Plane.

```csharp
try
{
    SentencePieceVocabulary llama2 = TokenizerJsonLoader.LoadUnigram("llama-2-7b/tokenizer.json");
}
catch (InvalidDataException e)
{
    // "This tokenizer.json cannot be loaded because it declares a 'BPE' model
    // with byte_fallback: LoadBpe is the call for a BPE model, and it is the
    // one that reproduces byte_fallback. Loading it anyway would produce
    // embeddings that do not match the model."
    Console.WriteLine(e.Message);
}
```

## Loading vocabularies

Four formats, four loaders. Each has `Load(Stream)`, `Load(string path)` and an
async counterpart; a stream you pass in is never disposed for you.

| File | Loader | Produces |
| --- | --- | --- |
| `vocab.txt` (BERT) | [`VocabTxtLoader.Load`](../reference/embeddings/persistence/vocabtxtloader-load.md) | `WordPieceVocabulary` |
| `tokenizer.json` (HuggingFace) | [`TokenizerJsonLoader.LoadWordPiece`](../reference/embeddings/persistence/tokenizerjsonloader-loadwordpiece.md) / `.LoadUnigram` / `.LoadBpe` | `WordPieceVocabulary` / `SentencePieceVocabulary` / `BpeVocabulary` |
| `spiece.model` (SentencePiece) | [`SentencePieceModelLoader.Load`](../reference/embeddings/persistence/sentencepiecemodelloader-load.md) | `SentencePieceVocabulary` |
| `vocab.json` + `merges.txt` (GPT-2) | [`BpeFilesLoader.Load`](../reference/embeddings/persistence/bpefilesloader-load.md) | `BpeVocabulary` |

```csharp
WordPieceVocabulary wpVocab = TokenizerJsonLoader.LoadWordPiece("tokenizer.json");
SentencePieceVocabulary uniVocab = TokenizerJsonLoader.LoadUnigram("tokenizer.json");
BpeVocabulary bpeVocab = TokenizerJsonLoader.LoadBpe("tokenizer.json");
```

`vocab.txt` carries only the tokens, so the settings that are not in the file —
whether the model was trained lowercased, what marks a continuation piece — are
parameters. `tokenizer.json` and `spiece.model` carry them, and the loaders read
them rather than asking.

### Bounds on untrusted files

A vocabulary is a downloaded file, and every count it declares sizes a buffer.
`ArtifactLoadOptions` bounds that: vocabulary size, token length, JSON depth,
total bytes, array length. Exceeding one raises `InvalidDataException` naming
both the limit and the value — never an `OutOfMemoryException`.

```csharp
var strict = new ArtifactLoadOptions { MaxVocabularySize = 50_000, MaxTotalBytes = 8L * 1024 * 1024 };
WordPieceVocabulary vocab = VocabTxtLoader.Load("vocab.txt", strict);
```

The defaults are generous enough for real models — BERT ships 30 522 tokens,
XLM-R 250 002 — so raising them should be deliberate.

### Models that are refused

Lodestar's tokenizers implement one fixed pipeline each. A file describing a
different one is **rejected**, with a message naming what was found:

- a model trained with an algorithm other than **unigram** — a `spiece.model`
  whose `trainer_spec.model_type` is `BPE`, `WORD` or `CHAR` carries a piece
  table that unigram Viterbi decoding would consume and segment the wrong way;
- **`byte_fallback`, on the Unigram path** — `spiece.model`, or a `tokenizer.json`
  declaring `model.type: "Unigram"` — refused unconditionally, since Python
  resolves an uncovered character into `<0x..>` byte pieces where that pipeline
  would emit the unknown piece instead; on the BPE path it is reproduced, and
  only a vocabulary that declares the flag without carrying all 256 `<0xXX>`
  pieces it promises is refused, naming the first one missing
  ([decision 0063](../decisions/0063-byte-fallback-requires-the-whole-alphabet-and-its-decoder-is-read-strictly-too.md));
- a normalizer named in a `spiece.model` with no `precompiled_charsmap` to
  apply, or a character map that will not parse — the rules come from the
  compiled map, never from `normalizer_spec.name`;
- for `tokenizer.json`, a normalizer other than `Precompiled` on the Unigram
  path, or `Lowercase`/a plain `BertNormalizer` on the WordPiece one — `NFKC`
  asks for the runtime's Unicode tables where the model asked for a frozen map;
- a pre-tokenizer other than `Whitespace` (WordPiece) or `Metaspace` (Unigram),
  and a `Metaspace` whose `replacement`, `prepend_scheme` (or the older
  `add_prefix_space`) or `split` is away from the default;
- for BPE, a pre-tokenizer other than a bare `ByteLevel` (stock GPT-2),
  `Whitespace` (the classic, non-byte-level lineage), a `Sequence` of exactly
  `Split` then `ByteLevel` (Llama-3, Qwen2), or none at all — which is read as
  `BpeVocabulary.NoPreTokenizer` rather than refused, below — and, on the
  byte-level path, a
  `decoder` whose byte-level-ness disagrees with the model's own, which would
  not decode what it encodes;
- for BPE, a `Sequence`'s `Split` step whose `pattern` declares neither
  `Regex` nor `String` as a string, or declares **both** — `tokenizers` writes
  exactly one of the two, so a node carrying both is not something the
  reference produces and choosing a winner would invent behaviour. A `String`
  is read as a literal and escaped, not interpreted: `\d` matches those two
  characters and leaves a digit alone;
- for BPE, a `Sequence`'s `Split` step declaring no `behavior`, no `invert`, or
  a `behavior` other than the five `tokenizers` defines — `Isolated`,
  `Removed`, `MergedWithPrevious`, `MergedWithNext`, `Contiguous`, spelled in
  the file's own PascalCase, not the Python constructor's snake_case.
  `tokenizers` 0.23.1 has no default for either field and refuses the file
  identically;
- for BPE, a normalizer other than `NFC`, `NFKC`, `NFD`, `NFKD` or a `Sequence`
  of those (empty included, which normalizes nothing) — `Replace` by name,
  since its pattern may be a Rust regex whose flavour .NET does not share, and
  anything else by name too. A **non-zero** `dropout` is refused as well — it
  changes what Python produces and is not applied here. A bare `ByteLevel`
  with `use_regex` off is **not**: it says the model splits nothing, which is
  what `BpeVocabulary.NoPreTokenizer` carries, and a file declaring no
  `pre_tokenizer` at all says the same and loads the same way. A vocabulary
  built by hand has to say which it means — one declaring no `PreSplit`, no
  `PreTokenizerPattern` and no `NoPreTokenizer` is refused by `BpeTokenizer`'s
  constructor rather than given the classic word-boundary split it used to
  get, that shape being what a no-split model would look like too. Write
  `PreTokenizerPattern = BpePatterns.Whitespace` for that split, `PreSplit`
  for a `Split` step, or `NoPreTokenizer = true` for a model whose text
  reaches the merge loop unsplit.
  `use_regex` off on the `ByteLevel` step of a `Split`-then-`ByteLevel`
  `Sequence` is a different thing again — the `Split` step is still a split,
  so such a file is not the mode. On, `ByteLevel` re-splits each piece the
  `Split` step already produced, on its own GPT-2 pattern; off, that second
  split does not happen, and the `Split` step's pattern is genuinely the only
  one applied. It is how Llama-3 and Qwen2 are written. A `dropout`
  of `0.0` and an `end_of_word_suffix` of `""` are accepted, because each
  provably changes nothing — the empty suffix reads back as absent on
  `BpeVocabulary`, an empty marker marking nothing. `continuing_subword_prefix`
  is applied rather than refused on the classic (non-byte-level) lineage:
  HuggingFace prefixes every non-initial symbol of a piece with it before
  merging, and so does `BpeTokenizer`; an empty prefix reads back as absent on
  `BpeVocabulary`, the same normalisation `end_of_word_suffix` gets;
- for BPE, a **non-empty** `continuing_subword_prefix` on a byte-level model.
  The prefix is never applied to a byte-level model's symbols while a merge's
  right side still has it stripped, so the two halves of the tokenizer would
  disagree — and silently, since the byte-level alphabet spells `0x23` as `#`,
  which lets a stripped right side land on another entry that exists. The
  refusal says that Lodestar does not reproduce such a file, not anything about
  what `tokenizers` makes of one. `BpeTokenizer`'s constructor refuses the same
  pairing, since `BpeVocabulary` can be built by hand;
- for BPE, a `ByteLevel` block that declares no `add_prefix_space`, wherever it
  appears — as the pre-tokenizer, as the second step of a `Sequence`, or as the
  `decoder`. `tokenizers` has no default for that field and refuses such a file
  itself, so accepting it here would mean inventing the value that decides
  whether a leading space is added. An omitted `use_regex` is fine (the
  reference defaults it to `true`, and stock GPT-2 leaves it out) and so is an
  omitted `trim_offsets`, which nothing here reads;
- a `post_processor` — the wrapping lives in `EncodingOptions.Template`
  ([Embed a batch](#embed-a-batch)), and a `post_processor` in the file would be
  a second source of truth for it, free to disagree with the first;
- a `truncation` or `padding` section;
- an `added_tokens` entry that contradicts `model.vocab` — the same content at a
  different id, or a negative id, which is an out-of-range index in the caller's
  embedding lookup wherever it lands. The matching flags are **not** a refusal
  any more: `lstrip`, `rstrip`, `single_word`, `special` and `normalized` are all
  read and honoured
  ([decision 0022](../decisions/0022-added-token-matching-flags.md));
- a `spiece.model` with no `normalizer_spec` at all — treating "absent" as
  "identity" would make the normalizer check skippable by deleting a field;
- a special-token id (`unk_id`, `bos_id`, `eos_id`, `pad_id`) outside the
  vocabulary. `-1` is how the format spells "this model has none".

Refusing every one of these is deliberate. The alternative is a vocabulary that
loads cleanly and produces embeddings for a model nobody trained, which is the
failure this whole guide warns about — and it would be silent.

The **whole** `added_tokens` table is carried into `AddedTokens` on the loaded
vocabulary — `BpeVocabulary.AddedTokens` and `WordPieceVocabulary.AddedTokens`,
both `IReadOnlyList<AddedToken>` — and folded into neither vocabulary. The
entries `model.vocab` also declares are included, because that is where every
special token lives. `<|endoftext|>` is id 50256 in GPT-2's own `model.vocab`
*and* in its `added_tokens`, and the pre-model scan reads nothing but this list,
so subtracting the intersection would drop exactly the tokens the scan exists
for. A token added with `Tokenizer.add_tokens` gets an id after the model's own
vocabulary and appears nowhere in `model.vocab`; it stays reachable all the same.

Both tokenizers match these entries as **text**, ahead of the model — the merge
loop for BPE, the greedy longest match for WordPiece. Folding them into the
vocabulary instead would make them matchable as a whole word only, which is a
different tokenizer as soon as an entry carries `lstrip`, `rstrip` or
`single_word`, and not what `tokenizers` does even when none does. Two things
follow, and both are worth knowing before they surprise you:

- `Count` on either vocabulary counts the model's own table alone, so it
  under-counts what `Encode` can emit. Size an embedding table from the model,
  not from `Count`.
- An `lstrip`ped added token absorbs the whitespace on its left into the match,
  and [`BpeTokenizer.Decode`](../reference/embeddings/tokenization/bpetokenizer-decode.md) — the only decoder here, and the one whose byte-level
  round trip is otherwise exact — does not put it back: `'a <mask> b'` comes back
  as `'a<mask> b'`. HuggingFace loses it too, so this is parity rather than a
  defect — [decision 0022](../decisions/0022-added-token-matching-flags.md)
  records the measurement, and which of the five flags decides what.

## Embed a batch

Weights are **not** shipped: export an encoder (e.g. a sentence-transformers
model) to ONNX and pass its path, together with the tokenizer it was trained
with.

```csharp
using Lodestar.Embeddings.Onnx;
using Lodestar.Embeddings.Tokenization;

using var embedder = new OnnxTextEmbedder("model.onnx", wp);

float[][] vectors = embedder.EmbedBatch(texts, new EncodingOptions
{
    Template = SpecialTokenTemplate.Bert,          // [CLS] … [SEP]
    MaxLength = 256,                               // special tokens included, as in HuggingFace
    Truncation = TruncationStrategy.LongestFirst,
    BatchSize = 32,
});
```

**The library inserts the special tokens.** `SpecialTokenTemplate` carries them
as data — `Bert` is `[CLS] … [SEP]`, `Roberta` is `<s> … </s>`, `T5` appends
`</s>` and nothing else, and a model that wraps its input differently takes a
template you write out. The tokens are named, never numbered: the id comes from
the model's own vocabulary, so a vocabulary that places `[CLS]` anywhere works,
and one that lacks it fails at construction instead of embedding a plausible
wrong id.

It also builds the attention mask, which is the part a caller most often gets
wrong. Each sub-batch is padded to **its own longest sequence**, never to
`MaxLength` — padding every batch to 512 when the median length is 30 wastes
most of the compute — and the padded positions are masked to 0 so they cannot
reach the pooled vector. That last property is asserted directly: a text
embedded in a batch gets the same vector, bit for bit, as the same text embedded
alone.

`SortByLength` groups sequences of similar length into the same call so the long
ones stop dictating the width of every row they share it with. The caller's
order is restored before returning, so it is a performance switch and never an
observable one. `EmbedBatch` takes a `CancellationToken`, observed while
tokenizing and between sub-batches.

`MaxLength` left null asks the model for its declared maximum — which most
exports do not have, since `torch.onnx.export` with `dynamic_axes` writes a
symbolic sequence dimension. The real positional limit lives in the model's
`config.json`, not in the graph, so for a real encoder pass it explicitly.

The single-sequence entry point is still there for a caller who owns the
tokenization:

```csharp
using Lodestar.Embeddings.Onnx;

using var embedder = new OnnxTextEmbedder("model.onnx");
float[] single = embedder.Embed(ids, mask);   // mean pooling + L2 built in
```

`OnnxTextEmbedder` feeds `token_type_ids` only if the model declares it, performs
masked mean pooling and L2-normalizes. It takes the token-embeddings output —
the only output when the model has one, else the first of `last_hidden_state`,
`token_embeddings`, `sentence_embedding` and `output` that it declares, unless
you name one. It refuses an output whose rank is neither
`[batch, sequence, dim]` nor the `[batch, dim]` of a model that pools
internally.

## Index a corpus and query it

```csharp
using Lodestar.Embeddings.Search;

var index = new EmbeddingIndex(dimension: vector.Length);
foreach (float[] v in corpusVectors) index.Add(v);   // normalized on insertion

IReadOnlyList<SearchResult> hits = index.Search(queryVector, k: 5);
foreach (var h in hits) Console.WriteLine($"#{h.Index}  score={h.Score:F3}");
```

Embedding a corpus is the expensive half, and it only has to happen once. Save
the built index and reload it in the process that queries it:

```csharp
var index = new EmbeddingIndex(dimension: vector.Length);
foreach ((float[] v, string id) in corpusWithIds) index.Add(v, id);
index.Save("corpus.index.json");

// …later, in another process
EmbeddingIndex reloaded = EmbeddingIndex.Load("corpus.index.json");
SearchResult best = reloaded.Search(queryVector, k: 1)[0];
Console.WriteLine($"{reloaded.GetId(best.Index)}  score={best.Score:F3}");
```

The vectors are stored as raw IEEE-754 bits, so a reloaded index scores bit for
bit what the original scored — [`EmbeddingIndex.Load`](../reference/embeddings/search/embeddingindex-load.md)
has the bounds it applies on the way in. The normalization flag travels in the file
rather than being supplied again on load, because an index reloaded under the
other setting would rank a corpus wrongly without ever looking wrong. The reader
bounds every count it reads against `ArtifactLoadOptions` before that count sizes
a buffer — except the vector block, which `MaxTotalBytes` caps in bytes before
parsing begins. An element-count limit sized for a vocabulary is three orders of
magnitude away from what a corpus of embeddings needs, and the default one
refused a 384-dimensional index past 2 604 vectors.

### Compressing the artifact

The artifact is JSON with the vectors in base64, which spends eight bits to carry
six — so it lands about 1.33x the size of the raw block. Deflate takes that back
almost exactly. **The library does not do it for you**, and the recipe is one
wrapper on each side:

```csharp
using System.IO.Compression;
using Lodestar.Embeddings.Search;

var index = new EmbeddingIndex(dimension: vector.Length);
foreach ((float[] v, string id) in corpusWithIds) index.Add(v, id);

using (var file = File.Create("corpus.index.json.gz"))
using (var compressing = new GZipStream(file, CompressionLevel.Optimal))
{
    index.Save(compressing);
}

using var opened = File.OpenRead("corpus.index.json.gz");
using var decompressing = new GZipStream(opened, CompressionMode.Decompress);
EmbeddingIndex fromDisk = EmbeddingIndex.Load(decompressing);
```

Nothing in the library knows compression happened: a decompressing stream is
neither seekable nor of known length, so it takes the same growable read path any
network stream takes, and `ArtifactLoadOptions` still bounds what the artifact
expands to rather than what it occupies on disk.

**Weigh it before reaching for it.** Compression is the most expensive thing you
can do to this path — measured at **26.67x the save and 7.19x the load, to buy 26%
of the disk**, and the price grows with the artifact: at the benchmark corpus's 20 MB
it is 76.8x and 14.8x. The numbers and the machines are in
[the performance guide](performance.md#compressing-an-index-issue-378). That is worth it
for an index shipped over a network and a poor trade for one written once to a
local disk, which is why the default declines to make the choice for you.
`GZipStream` is the portable recipe; on .NET 10 `BrotliStream` is smaller and much
cheaper to write, and does not exist on `netstandard2.0`.

The search is an **exhaustive SIMD-vectorized** cosine (`System.Numerics.Vector`) —
the right default up to a few hundred thousand vectors. An approximate index
(HNSW) is only worth adding once a real need is demonstrated.
