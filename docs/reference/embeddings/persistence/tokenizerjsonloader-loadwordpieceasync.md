# TokenizerJsonLoader.LoadWordPieceAsync

Reads the WordPiece model a `tokenizer.json` declares, asynchronously.

<!-- docs-declaration -->

```csharp
public static Task<WordPieceVocabulary> LoadWordPieceAsync(Stream source, ArtifactLoadOptions options = null, CancellationToken cancellationToken = default)
```

**Parameters** — `source` is a readable stream, never disposed here; `path` is the file to read. `options` bounds what will be accepted and defaults to
[`ArtifactLoadOptions`](artifactloadoptions.md)'s own defaults. `cancellationToken` cancels the read.

**Returns** — `Task<WordPieceVocabulary>`, completing with the loaded vocabulary.

**Exceptions** — `ArgumentNullException` for a null source or path. `InvalidDataException`
when the file declares a different model, declares a pipeline this package does not
reproduce, or exceeds a bound in `options` — the message names what was refused and why. `OperationCanceledException` when `cancellationToken` is signalled.

**Example** — the same load, awaited.

<!-- docs-run: skip - the file is a model artifact, and model artifacts are never committed (CONTRIBUTING.md) -->

```csharp
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

static async Task<WordPieceVocabulary> LoadAsync() =>
    await TokenizerJsonLoader.LoadWordPieceAsync(File.OpenRead("tokenizer.json"));

WordPieceVocabulary vocab = LoadAsync().GetAwaiter().GetResult();
```

**Remarks** — Same file, same reads and the same refusals as
[`LoadWordPiece`](tokenizerjsonloader-loadwordpiece.md) — stock BERT included; only the I/O is
asynchronous.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TokenizerJsonLoader.LoadWordPiece`](tokenizerjsonloader-loadwordpiece.md),
[`TokenizerJsonLoader`](tokenizerjsonloader.md), [the persistence index](../persistence.md).
