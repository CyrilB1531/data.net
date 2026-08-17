# BpeFilesLoader.LoadAsync

Reads a `vocab.json` and `merges.txt` pair, asynchronously.

<!-- docs-declaration -->

```csharp
public static Task<BpeVocabulary> LoadAsync(Stream vocabJson, Stream merges, ArtifactLoadOptions options = null, bool byteLevel = true, CancellationToken cancellationToken = default)
```

**Parameters** — `vocabJson` and `merges` are the two streams, never disposed here; `vocabJsonPath` and
`mergesPath` are the two files. `options` bounds what will be accepted and defaults to
[`ArtifactLoadOptions`](artifactloadoptions.md)'s own defaults.
`byteLevel` says whether the model is a byte-level BPE, which GPT-2 and its descendants are.
`cancellationToken` cancels the read.

**Returns** — `Task<BpeVocabulary>`, completing with the loaded vocabulary.

**Exceptions** — `ArgumentNullException` for a null source or path. `InvalidDataException`
when the content is not the format expected, declares a model this loader does not read, or
exceeds a bound in `options` — the message names both the limit and the value. `OperationCanceledException` when `cancellationToken` is signalled.

**Example** — both streams, awaited together.

<!-- docs-run: skip - the file is a model artifact, and model artifacts are never committed (CONTRIBUTING.md) -->

```csharp
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

static async Task<BpeVocabulary> LoadAsync() =>
    await BpeFilesLoader.LoadAsync(File.OpenRead("vocab.json"), File.OpenRead("merges.txt"));

BpeVocabulary vocab = LoadAsync().GetAwaiter().GetResult();
```

**Remarks** — Same pair, same ordering rules and same refusals as [`Load`](bpefilesloader-load.md); only the
I/O is asynchronous. The two streams are both read and neither is disposed, so the caller closes
them.

The same-checkpoint requirement from [`Load`](bpefilesloader-load.md) applies unchanged, and is no
easier to notice here.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BpeFilesLoader.Load`](bpefilesloader-load.md), [`BpeFilesLoader`](bpefilesloader.md),
[the persistence index](../persistence.md).
