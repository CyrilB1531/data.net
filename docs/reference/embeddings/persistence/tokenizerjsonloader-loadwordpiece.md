# TokenizerJsonLoader.LoadWordPiece

Reads the WordPiece model a `tokenizer.json` declares.

<!-- docs-declaration -->

```csharp
public static WordPieceVocabulary LoadWordPiece(Stream source, ArtifactLoadOptions options = null)
public static WordPieceVocabulary LoadWordPiece(string path, ArtifactLoadOptions options = null)
```

**Parameters** — `source` is a readable stream, never disposed here; `path` is the file to read. `options` bounds what will be accepted and defaults to
[`ArtifactLoadOptions`](artifactloadoptions.md)'s own defaults.

**Returns** — `WordPieceVocabulary`, with the continuation prefix and the unknown piece read from the file.

**Exceptions** — `ArgumentNullException` for a null source or path. `InvalidDataException`
when the file declares a different model, declares a pipeline this package does not
reproduce, or exceeds a bound in `options` — the message names what was refused and why.

**Example** — a `tokenizer.json` whose pipeline already matches this package's.

<!-- docs-run: skip - the file is a model artifact, and model artifacts are never committed (CONTRIBUTING.md) -->

```csharp
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

WordPieceVocabulary vocab = TokenizerJsonLoader.LoadWordPiece("tokenizer.json");
```

**Remarks** — **A stock HuggingFace BERT `tokenizer.json` is refused here, and that is the correct outcome.**
Such a file declares a `BertPreTokenizer` and a full `BertNormalizer`, neither of which this
package reproduces; loading it anyway would produce embeddings that do not match the model and
carry nothing to say so. The route for stock BERT is
[`VocabTxtLoader.Load`](vocabtxtloader-load.md), whose format declares no pipeline to disagree
about.

What this entry point is for is a `tokenizer.json` whose pipeline **is** this package's — then the
settings `vocab.txt` would have left as parameters are read from the file instead.

Unlike [`VocabTxtLoader.Load`](vocabtxtloader-load.md), there is no `lowercase` parameter: the
file says.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`VocabTxtLoader.Load`](vocabtxtloader-load.md),
[`TokenizerJsonLoader.LoadWordPieceAsync`](tokenizerjsonloader-loadwordpieceasync.md),
[`TokenizerJsonLoader`](tokenizerjsonloader.md).
