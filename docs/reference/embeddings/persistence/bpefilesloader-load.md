# BpeFilesLoader.Load

Reads a `vocab.json` and `merges.txt` pair into a BPE vocabulary.

<!-- docs-declaration -->

```csharp
public static BpeVocabulary Load(Stream vocabJson, Stream merges, ArtifactLoadOptions options = null, bool byteLevel = true)
public static BpeVocabulary Load(string vocabJsonPath, string mergesPath, ArtifactLoadOptions options = null, bool byteLevel = true)
```

**Parameters** — `vocabJson` and `merges` are the two streams, never disposed here; `vocabJsonPath` and
`mergesPath` are the two files. `options` bounds what will be accepted and defaults to
[`ArtifactLoadOptions`](artifactloadoptions.md)'s own defaults.
`byteLevel` says whether the model is a byte-level BPE, which GPT-2 and its descendants are.

**Returns** — `BpeVocabulary`, with the merge list in the order the file gave it.

**Exceptions** — `ArgumentNullException` for a null source or path. `InvalidDataException`
when the content is not the format expected, declares a model this loader does not read, or
exceeds a bound in `options` — the message names both the limit and the value.

**Example** — the GPT-2 layout, both halves from the same checkpoint.

<!-- docs-run: skip - the file is a model artifact, and model artifacts are never committed (CONTRIBUTING.md) -->

```csharp
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

BpeVocabulary vocab = BpeFilesLoader.Load("gpt2/vocab.json", "gpt2/merges.txt");
```

**Remarks** — **The two files must come from the same checkpoint.** Nothing here can detect that they do not:
a vocabulary from one model and merges from another load without complaint and tokenize wrongly,
because the merge ranks address pieces that mean something else.

Merge **order is the algorithm** — the lowest-ranked applicable merge is applied first — so the
file's line order is significant and a sorted `merges.txt` is a different model.

`byteLevel` stays a parameter because neither file records it. That is the standing argument for
preferring [`TokenizerJsonLoader.LoadBpe`](tokenizerjsonloader-loadbpe.md) when the checkpoint
ships a `tokenizer.json`: there, the flag is read from the file.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BpeFilesLoader`](bpefilesloader.md), [`BpeFilesLoader.LoadAsync`](bpefilesloader-loadasync.md),
[the persistence index](../persistence.md).
