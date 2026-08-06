# Semantic search with embeddings

`DataNet.Embeddings` covers the full chain: **tokenize → infer (ONNX) → pool →
index → query**. ONNX Runtime is isolated here, so the distance and
vectorization packages take no native dependency.

```bash
dotnet add package DataNet.Embeddings
```

## Sub-word tokenization

Two tokenizers, depending on the model — and in both cases the vocabulary is
**read from the file the model ships with**, never assembled by hand.

**WordPiece** (BERT), from a `vocab.txt` or a `tokenizer.json`:

```csharp
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tokenization;

WordPieceVocabulary vocab = VocabTxtLoader.Load("bert-base-uncased/vocab.txt", lowercase: true);
var wp = new WordPieceTokenizer(vocab);
TokenizationResult t = wp.Encode("playing");   // pieces: play ##ing
```

**SentencePiece** (ALBERT, T5, camemBERT, XLM-R) — unigram Viterbi segmentation,
from the trained `spiece.model`. The model's own `precompiled_charsmap` is
applied before segmentation, so a stock file — all four families ship `nmt_nfkc`
— tokenizes here as it does in Python:

```csharp
SentencePieceVocabulary vocab = SentencePieceModelLoader.Load("spiece.model");
var sp = new SentencePieceTokenizer(vocab);
TokenizationResult t = sp.Encode("the quick brown fox");
```

The loader is what makes the second example correct rather than merely short:
`spiece.model` records the *type* of every piece, so the tokenizer knows which
entries are control markers instead of inferring it from their ids. See
[loading vocabularies](#loading-vocabularies) for `tokenizer.json`, for the
limits applied to untrusted files, and for which models are refused outright.

> The tokenization must match the model's **exactly**, otherwise the embeddings
> are wrong (§5 of the brief). Both tokenizers are validated token-for-token
> against HuggingFace `tokenizers` / the `sentencepiece` library, and so are the
> three loaders.

## Loading vocabularies

Three formats, three loaders. Each has `Load(Stream)`, `Load(string path)` and an
async counterpart; a stream you pass in is never disposed for you.

| File | Loader | Produces |
| --- | --- | --- |
| `vocab.txt` (BERT) | `VocabTxtLoader.Load` | `WordPieceVocabulary` |
| `tokenizer.json` (HuggingFace) | `TokenizerJsonLoader.LoadWordPiece` / `.LoadUnigram` | `WordPieceVocabulary` / `SentencePieceVocabulary` |
| `spiece.model` (SentencePiece) | `SentencePieceModelLoader.Load` | `SentencePieceVocabulary` |

```csharp
WordPieceVocabulary wpVocab = TokenizerJsonLoader.LoadWordPiece("tokenizer.json");
SentencePieceVocabulary uniVocab = TokenizerJsonLoader.LoadUnigram("tokenizer.json");
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

DataNet's tokenizers implement one fixed pipeline each. A file describing a
different one is **rejected**, with a message naming what was found:

- a model trained with an algorithm other than **unigram** — a `spiece.model`
  whose `trainer_spec.model_type` is `BPE`, `WORD` or `CHAR` carries a piece
  table that unigram Viterbi decoding would consume and segment the wrong way;
- **`byte_fallback`**, in either format: Python resolves an uncovered character
  into `<0x..>` byte pieces where these tokenizers emit the unknown piece;
- a normalizer named in a `spiece.model` with no `precompiled_charsmap` to
  apply, or a character map that will not parse — the rules come from the
  compiled map, never from `normalizer_spec.name`;
- for `tokenizer.json`, a normalizer other than `Precompiled` on the Unigram
  path, or `Lowercase`/a plain `BertNormalizer` on the WordPiece one — `NFKC`
  asks for the runtime's Unicode tables where the model asked for a frozen map;
- a pre-tokenizer other than `Whitespace` (WordPiece) or `Metaspace` (Unigram),
  and a `Metaspace` whose `replacement`, `prepend_scheme` (or the older
  `add_prefix_space`) or `split` is away from the default;
- a `post_processor` — DataNet does not insert `[CLS]`/`[SEP]` for you;
- a `truncation` or `padding` section;
- an `added_tokens` entry that contradicts `model.vocab`, or that asks for
  `lstrip`, `rstrip` or `single_word` matching;
- a `spiece.model` with no `normalizer_spec` at all — treating "absent" as
  "identity" would make the normalizer check skippable by deleting a field;
- a special-token id (`unk_id`, `bos_id`, `eos_id`, `pad_id`) outside the
  vocabulary. `-1` is how the format spells "this model has none".

Entries in `added_tokens` that sit outside `model.vocab` are **folded into the
vocabulary** rather than dropped, so a token added with `Tokenizer.add_tokens`
stays reachable. A stock BERT file lists its special tokens in both tables at
the same ids, which folds to a no-op.

This is deliberate. The alternative is a vocabulary that loads cleanly and
produces embeddings for a model nobody trained, which is the failure this whole
guide warns about — and it would be silent.

## Run an ONNX model + pooling

Weights are **not** shipped: export an encoder (e.g. a sentence-transformers
model) to ONNX and pass its path.

<!-- docs-compile: skip - the two /* … */ placeholders are not C#; #60 replaces them with the batch API that owns the special tokens and the mask -->
```csharp
using DataNet.Embeddings.Onnx;

using var embedder = new OnnxTextEmbedder("model.onnx");
long[] ids = /* wp.Encode(text).Ids, with [CLS]/[SEP] if the model expects them */;
long[] mask = /* 1 per real token, 0 for padding */;
float[] vector = embedder.Embed(ids, mask);   // mean pooling + L2 built in
```

`OnnxTextEmbedder` feeds `token_type_ids` only if the model declares it, performs
masked mean pooling and L2-normalizes.

## Index a corpus and query it

```csharp
using DataNet.Embeddings.Search;

var index = new EmbeddingIndex(dimension: vector.Length);
foreach (float[] v in corpusVectors) index.Add(v);   // normalized on insertion

IReadOnlyList<SearchResult> hits = index.Search(queryVector, k: 5);
foreach (var h in hits) Console.WriteLine($"#{h.Index}  score={h.Score:F3}");
```

The search is an **exhaustive SIMD-vectorized** cosine (`System.Numerics.Vector`) —
the right default up to a few hundred thousand vectors. An approximate index
(HNSW) is only worth adding once a real need is demonstrated.
