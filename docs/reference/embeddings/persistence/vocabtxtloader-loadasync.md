# VocabTxtLoader.LoadAsync

Reads a `vocab.txt` into a WordPiece vocabulary, asynchronously.

<!-- docs-declaration -->

```csharp
public static Task<WordPieceVocabulary> LoadAsync(Stream source, ArtifactLoadOptions options = null, string unkToken = "[UNK]", string continuationPrefix = "##", bool lowercase = false, CancellationToken cancellationToken = default)
```

**Parameters** — `source` is a readable stream, never disposed here; `path` is the file to read. `options` bounds what will be accepted and defaults to
[`ArtifactLoadOptions`](artifactloadoptions.md)'s own defaults.
`unkToken` is the piece an unknown word maps to, `continuationPrefix` marks a word-internal
piece, and `lowercase` says whether the model was trained on lowercased text.
`cancellationToken` cancels the read.

**Returns** — `Task<WordPieceVocabulary>`, completing with the loaded vocabulary.

**Exceptions** — `ArgumentNullException` for a null source or path. `InvalidDataException`
when the content is not the format expected, declares a model this loader does not read, or
exceeds a bound in `options` — the message names both the limit and the value. `OperationCanceledException` when `cancellationToken` is signalled.

**Example** — the same load, awaited.

<!-- docs-run: skip - the file is a model artifact, and model artifacts are never committed (CONTRIBUTING.md) -->

```csharp
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

static async Task<WordPieceVocabulary> LoadAsync() =>
    await VocabTxtLoader.LoadAsync(File.OpenRead("vocab.txt"), lowercase: true);

WordPieceVocabulary vocab = LoadAsync().GetAwaiter().GetResult();
```

**Remarks** — Same format, same parameters and same refusals as [`Load`](vocabtxtloader-load.md); only the I/O
is asynchronous, and there is no `string` overload because opening the file is the synchronous
part.

`lowercase` matters exactly as much here — see [`Load`](vocabtxtloader-load.md) for why getting it
wrong is silent.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`VocabTxtLoader.Load`](vocabtxtloader-load.md), [`VocabTxtLoader`](vocabtxtloader.md),
[the persistence index](../persistence.md).
