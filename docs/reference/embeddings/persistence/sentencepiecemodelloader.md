# SentencePieceModelLoader

Reads a SentencePiece `spiece.model` — the trained unigram vocabulary, its scores, its piece types
and the model's special-token ids.

<!-- docs-declaration -->

```csharp
public static class SentencePieceModelLoader
```

**Example** — T5, ALBERT, camemBERT and XLM-R all ship this file.

<!-- docs-run: skip - the file is a model artifact, and model artifacts are never committed (CONTRIBUTING.md) -->

```csharp
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

SentencePieceVocabulary vocab = SentencePieceModelLoader.Load("spiece.model");
var tokenizer = new SentencePieceTokenizer(vocab);
```

**Remarks** — `spiece.model` is a protobuf, and it carries far more than a word list. It records
the **type** of every piece, so the tokenizer knows which entries are control markers instead of
guessing from their ids, and it carries the scores unigram Viterbi segmentation needs.

It also carries the **normalizer**, as a compiled character map. That map is read from the file and
never assumed to be `identity` — a stock model ships `nmt_nfkc`, and applying it is what makes
tokenization here match Python on the same text.

Because all of that is in the file, [`Load`](sentencepiecemodelloader-load.md) takes only bounds.
There is nothing left for a caller to get wrong.

Reference behaviour is `sentencepiece.SentencePieceProcessor(model_file=…)`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TokenizerJsonLoader`](tokenizerjsonloader.md),
[`ArtifactLoadOptions`](artifactloadoptions.md), [the persistence index](../persistence.md).

## Members

| Member | What it does |
| --- | --- |
| [`SentencePieceModelLoader.Load`](sentencepiecemodelloader-load.md) | Reads a `spiece.model`. |
| [`SentencePieceModelLoader.LoadAsync`](sentencepiecemodelloader-loadasync.md) | The same, asynchronously. |
