# BpeFilesLoader

Reads the `vocab.json` + `merges.txt` pair GPT-2 ships, the layout that predates `tokenizer.json`.

<!-- docs-declaration -->

```csharp
public static class BpeFilesLoader
```

**Example** — two files, one vocabulary.

<!-- docs-run: skip - the file is a model artifact, and model artifacts are never committed (CONTRIBUTING.md) -->

```csharp
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

BpeVocabulary vocab = BpeFilesLoader.Load("gpt2/vocab.json", "gpt2/merges.txt");
var tokenizer = new BpeTokenizer(vocab);
```

**Remarks** — two files because BPE is two things: `vocab.json` maps a piece to an id, and
`merges.txt` gives the **ordered** merge list. Order is the algorithm — the lowest-ranked merge
applies first — so the pair has to come from the same checkpoint. Mixing a vocabulary from one
model with merges from another loads without complaint and tokenizes wrongly.

`byteLevel` defaults to `true`, which is GPT-2 and its descendants.
[`Load`](bpefilesloader-load.md) explains when it is not.

Neither file records whether the model is byte-level, which is why that stays a parameter here and
is read from the file by [`TokenizerJsonLoader.LoadBpe`](tokenizerjsonloader-loadbpe.md). Prefer
the `tokenizer.json` route when the checkpoint offers one.

Reference behaviour is `tokenizers.models.BPE.from_file(vocab, merges)`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TokenizerJsonLoader`](tokenizerjsonloader.md),
[`ArtifactLoadOptions`](artifactloadoptions.md), [the persistence index](../persistence.md).

## Members

| Member | What it does |
| --- | --- |
| [`BpeFilesLoader.Load`](bpefilesloader-load.md) | Reads a `vocab.json` and `merges.txt` pair. |
| [`BpeFilesLoader.LoadAsync`](bpefilesloader-loadasync.md) | The same, asynchronously. |
