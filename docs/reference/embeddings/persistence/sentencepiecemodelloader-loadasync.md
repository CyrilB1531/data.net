# SentencePieceModelLoader.LoadAsync

Reads a `spiece.model`, asynchronously.

<!-- docs-declaration -->

```csharp
public static Task<SentencePieceVocabulary> LoadAsync(Stream source, ArtifactLoadOptions options = null, CancellationToken cancellationToken = default)
```

**Parameters** — `source` is a readable stream, never disposed here; `path` is the file to read. `options` bounds what will be accepted and defaults to
[`ArtifactLoadOptions`](artifactloadoptions.md)'s own defaults. `cancellationToken` cancels the read.

**Returns** — `Task<SentencePieceVocabulary>`, completing with the loaded vocabulary.

**Exceptions** — `ArgumentNullException` for a null source or path. `InvalidDataException`
when the content is not the format expected, declares a model this loader does not read, or
exceeds a bound in `options` — the message names both the limit and the value. `OperationCanceledException` when `cancellationToken` is signalled.

**Example** — the same load, awaited.

<!-- docs-run: skip - the file is a model artifact, and model artifacts are never committed (CONTRIBUTING.md) -->

```csharp
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

static async Task<SentencePieceVocabulary> LoadAsync() =>
    await SentencePieceModelLoader.LoadAsync(File.OpenRead("spiece.model"));

SentencePieceVocabulary vocab = LoadAsync().GetAwaiter().GetResult();
```

**Remarks** — Same protobuf, same reads and same refusals as [`Load`](sentencepiecemodelloader-load.md); only
the I/O is asynchronous. There is no `string` overload, because opening the file is the
synchronous part and a caller who wants one can open it and hand over the stream.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SentencePieceModelLoader.Load`](sentencepiecemodelloader-load.md),
[`SentencePieceModelLoader`](sentencepiecemodelloader.md),
[the persistence index](../persistence.md).
