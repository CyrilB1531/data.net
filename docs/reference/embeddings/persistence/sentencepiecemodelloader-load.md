# SentencePieceModelLoader.Load

Reads a `spiece.model` into a SentencePiece vocabulary.

<!-- docs-declaration -->

```csharp
public static SentencePieceVocabulary Load(Stream source, ArtifactLoadOptions options = null)
public static SentencePieceVocabulary Load(string path, ArtifactLoadOptions options = null)
```

**Parameters** — `source` is a readable stream, never disposed here; `path` is the file to read. `options` bounds what will be accepted and defaults to
[`ArtifactLoadOptions`](artifactloadoptions.md)'s own defaults.

**Returns** — `SentencePieceVocabulary`, carrying the pieces, their scores, their types and the model's
special-token ids.

**Exceptions** — `ArgumentNullException` for a null source or path. `InvalidDataException`
when the content is not the format expected, declares a model this loader does not read, or
exceeds a bound in `options` — the message names both the limit and the value.

**Example** — everything this tokenizer needs is in the one file.

<!-- docs-run: skip - the file is a model artifact, and model artifacts are never committed (CONTRIBUTING.md) -->

```csharp
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

SentencePieceVocabulary vocab = SentencePieceModelLoader.Load("spiece.model");
```

**Remarks** — **No settings parameters, because the file has them all.** That is the difference from
[`VocabTxtLoader.Load`](vocabtxtloader-load.md), and it is why this is the safer of the two routes
where a model offers both: there is nothing left to get wrong.

The piece **types** are read rather than inferred. Control markers are marked as such in the
protobuf, so the tokenizer does not have to guess from an id range — a guess that breaks on any
model that lays its specials out differently.

The **normalizer** comes from the file too, as a compiled character map, and is never assumed to
be `identity`. All four of the common families ship `nmt_nfkc`, and applying the model's own map
is what makes the segmentation match Python's on the same string.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SentencePieceModelLoader`](sentencepiecemodelloader.md),
[`SentencePieceModelLoader.LoadAsync`](sentencepiecemodelloader-loadasync.md),
[the persistence index](../persistence.md).
