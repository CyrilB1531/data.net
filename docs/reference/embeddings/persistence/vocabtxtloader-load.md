# VocabTxtLoader.Load

Reads a `vocab.txt` into a WordPiece vocabulary.

<!-- docs-declaration -->

```csharp
public static WordPieceVocabulary Load(Stream source, ArtifactLoadOptions options = null, string unkToken = "[UNK]", string continuationPrefix = "##", bool lowercase = false)
public static WordPieceVocabulary Load(string path, ArtifactLoadOptions options = null, string unkToken = "[UNK]", string continuationPrefix = "##", bool lowercase = false)
```

**Parameters** — `source` is a readable stream, never disposed here; `path` is the file to read. `options` bounds what will be accepted and defaults to
[`ArtifactLoadOptions`](artifactloadoptions.md)'s own defaults.
`unkToken` is the piece an unknown word maps to, `continuationPrefix` marks a word-internal
piece, and `lowercase` says whether the model was trained on lowercased text.

**Returns** — `WordPieceVocabulary`, ids assigned by line number: the first line is id `0`.

**Exceptions** — `ArgumentNullException` for a null source or path. `InvalidDataException`
when the content is not the format expected, declares a model this loader does not read, or
exceeds a bound in `options` — the message names both the limit and the value.

**Example** — an uncased BERT checkpoint, which is the case `lowercase` exists for.

<!-- docs-run: skip - the file is a model artifact, and model artifacts are never committed (CONTRIBUTING.md) -->

```csharp
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

WordPieceVocabulary vocab = VocabTxtLoader.Load("bert-base-uncased/vocab.txt", lowercase: true);
```

**Remarks** — **`lowercase` is the parameter to get right**, and the file cannot tell you its value. Loading an
uncased checkpoint without it leaves every capitalised word mapping to the unknown piece, which
does not throw, does not look wrong, and produces embeddings that are quietly meaningless. The
name of the checkpoint is usually the only evidence — `bert-base-uncased` against
`bert-base-cased`.

The id **is** the line number, so the file's order is the vocabulary's order and a reordered
`vocab.txt` is a different vocabulary. Two quirks of the Python loop this matches are reproduced
deliberately rather than corrected; `docs/equivalence.md`'s loader row names them.

The defaults `[UNK]` and `##` are BERT's. A model using other markers has to say so here, because
`vocab.txt` records neither.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`VocabTxtLoader`](vocabtxtloader.md), [`VocabTxtLoader.LoadAsync`](vocabtxtloader-loadasync.md),
[the persistence index](../persistence.md).
