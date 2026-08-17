# TokenizerJsonLoader

Reads a HuggingFace `tokenizer.json`: the WordPiece, Unigram or BPE model it declares, with the
settings that change tokenization.

<!-- docs-declaration -->

```csharp
public static class TokenizerJsonLoader
```

**Example** — one file, three models, and you pick which one you expect.

<!-- docs-run: skip - the file is a model artifact, and model artifacts are never committed (CONTRIBUTING.md) -->

```csharp
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

WordPieceVocabulary wordPiece = TokenizerJsonLoader.LoadWordPiece("tokenizer.json");
SentencePieceVocabulary unigram = TokenizerJsonLoader.LoadUnigram("tokenizer.json");
BpeVocabulary bpe = TokenizerJsonLoader.LoadBpe("tokenizer.json");
```

**Remarks** — `tokenizer.json` is the modern format and carries the whole pipeline, not just the
vocabulary. That is why there are three entry points rather than one returning a common type:
**you say which model you expect**, and a file declaring another is refused rather than coerced.

This is the vocabulary side of `tokenizers.Tokenizer.from_file`. Each tokenizer in this package
implements one fixed pipeline, so a file describing a different one is **refused by name** — the
guide's "Models that are refused" lists them. Stock BERT is among them, and its route is
[`VocabTxtLoader`](vocabtxtloader.md).

An **unknown top-level property is accepted in silence**, which is deliberate: the format grows,
and refusing a file for carrying a key this loader has never heard of would break on every new
version of `tokenizers`. Properties that change tokenization are read; the rest are not this
loader's business.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`VocabTxtLoader`](vocabtxtloader.md), [`BpeFilesLoader`](bpefilesloader.md),
[the persistence index](../persistence.md), [the embeddings guide](../../../guides/embeddings.md).

## Members

| Member | What it does |
| --- | --- |
| [`TokenizerJsonLoader.LoadBpe`](tokenizerjsonloader-loadbpe.md) | The BPE model a `tokenizer.json` declares. |
| [`TokenizerJsonLoader.LoadBpeAsync`](tokenizerjsonloader-loadbpeasync.md) | The same, asynchronously. |
| [`TokenizerJsonLoader.LoadUnigram`](tokenizerjsonloader-loadunigram.md) | The Unigram model a `tokenizer.json` declares. |
| [`TokenizerJsonLoader.LoadUnigramAsync`](tokenizerjsonloader-loadunigramasync.md) | The same, asynchronously. |
| [`TokenizerJsonLoader.LoadWordPiece`](tokenizerjsonloader-loadwordpiece.md) | The WordPiece model a `tokenizer.json` declares. |
| [`TokenizerJsonLoader.LoadWordPieceAsync`](tokenizerjsonloader-loadwordpieceasync.md) | The same, asynchronously. |
