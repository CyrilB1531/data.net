# TokenizerJsonLoader.LoadUnigram

Reads the Unigram model a `tokenizer.json` declares.

<!-- docs-declaration -->

```csharp
public static SentencePieceVocabulary LoadUnigram(Stream source, ArtifactLoadOptions options = null)
public static SentencePieceVocabulary LoadUnigram(string path, ArtifactLoadOptions options = null)
```

**Parameters** — `source` is a readable stream, never disposed here; `path` is the file to read. `options` bounds what will be accepted and defaults to
[`ArtifactLoadOptions`](artifactloadoptions.md)'s own defaults.

**Returns** — `SentencePieceVocabulary`, the same type [`SentencePieceModelLoader.Load`](sentencepiecemodelloader-load.md)
produces.

**Exceptions** — `ArgumentNullException` for a null source or path. `InvalidDataException`
when the file declares a different model, declares a pipeline this package does not
reproduce, or exceeds a bound in `options` — the message names what was refused and why.

**Example** — the `tokenizer.json` route to a Unigram model.

<!-- docs-run: skip - the file is a model artifact, and model artifacts are never committed (CONTRIBUTING.md) -->

```csharp
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

SentencePieceVocabulary vocab = TokenizerJsonLoader.LoadUnigram("tokenizer.json");
```

**Remarks** — This is the alternative to [`SentencePieceModelLoader.Load`](sentencepiecemodelloader-load.md)
for a checkpoint that ships `tokenizer.json` rather than `spiece.model`. Both produce the same
type, so the tokenizer built afterwards does not care which was used.

**It refuses a file that declares a `BPE` model**, including one that also declares
`byte_fallback` — this call is for the Unigram pipeline, and `byte_fallback` is a `BPE`-model
setting this pipeline does not reproduce, so a plain BPE checkpoint and one with `byte_fallback`
are both refused for declaring the wrong model kind. A Llama-2 or Mistral v0.1 `tokenizer.json` is
the second shape: the message names `byte_fallback` directly rather than only "wrong model type",
and points at [`LoadBpe`](tokenizerjsonloader-loadbpe.md) — which is the call that loads such a
file today, `byte_fallback` and all
([decision 0063](../../../decisions/0063-byte-fallback-requires-the-whole-alphabet-and-its-decoder-is-read-strictly-too.md)).
The routing survives; only the reason changed.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SentencePieceModelLoader.Load`](sentencepiecemodelloader-load.md),
[`TokenizerJsonLoader.LoadBpe`](tokenizerjsonloader-loadbpe.md),
[`TokenizerJsonLoader`](tokenizerjsonloader.md).
