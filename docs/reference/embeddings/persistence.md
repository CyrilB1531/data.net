# Loading vocabularies — `Lodestar.Embeddings.Persistence`

A tokenizer is only correct if its vocabulary is the model's own. `Lodestar.Embeddings.Persistence`
reads the four file formats models actually ship, and bounds what it will accept from them.

**Nothing here is assembled by hand.** The settings that change tokenization — whether the model
was trained lowercased, what marks a continuation piece, which pieces are control markers, how a
merge list is ordered — are read from the file wherever the file carries them, because a caller
guessing one produces embeddings that do not match the model and look fine.

## Which loader?

| The file the model ships | Loader | Produces |
| --- | --- | --- |
| `vocab.txt` (BERT) | [`VocabTxtLoader`](persistence/vocabtxtloader.md) | `WordPieceVocabulary` |
| `spiece.model` (SentencePiece) | [`SentencePieceModelLoader`](persistence/sentencepiecemodelloader.md) | `SentencePieceVocabulary` |
| `vocab.json` + `merges.txt` (GPT-2) | [`BpeFilesLoader`](persistence/bpefilesloader.md) | `BpeVocabulary` |
| `tokenizer.json` (HuggingFace) | [`TokenizerJsonLoader`](persistence/tokenizerjsonloader.md) | any of the three |

Every loader has the same three shapes: `Load(Stream)`, `Load(string path)` and an async
counterpart. **A stream you pass in is never disposed for you.**

## What the file carries, and what stays a parameter

The split is not arbitrary — it is whatever the format records.

`vocab.txt` is one token per line and nothing else, so
[`VocabTxtLoader.Load`](persistence/vocabtxtloader-load.md) takes `unkToken`,
`continuationPrefix` and `lowercase` as parameters: the file cannot tell you them, and getting
`lowercase` wrong silently changes every embedding.

`spiece.model` and `tokenizer.json` carry their settings, so the loaders read them instead of
asking. That is why [`SentencePieceModelLoader.Load`](persistence/sentencepiecemodelloader-load.md)
takes only bounds — the piece types, the scores and the normalizer map are all in the file.

## A file is untrusted until it has been bounded

A vocabulary is something you downloaded, and every count it declares would otherwise size a
buffer. [`ArtifactLoadOptions`](persistence/artifactloadoptions.md) is the ceiling on all five of
them, applied **while reading** rather than after. Exceeding one raises `InvalidDataException`
naming the limit and the value — never an `OutOfMemoryException`, which is the failure this type
exists to prevent.

This is a **different type** from `Lodestar.Text.Persistence.ArtifactLoadOptions`, which bounds a
saved vectorizer. The two are declared separately rather than shared;
[decision 0011](../../decisions/0011-persistence-format.md) has why, and the practical consequence
is that the defaults differ because what they bound differs.

## Refusing a model is a feature

Each tokenizer here implements one fixed pipeline, and a `tokenizer.json` describing another is
**refused by name** rather than loaded into an approximation of itself. Stock BERT is refused by
[`LoadWordPiece`](persistence/tokenizerjsonloader-loadwordpiece.md) — its route is
`VocabTxtLoader` — and Llama-2 and Mistral v0.1 are refused by
[`LoadBpe`](persistence/tokenizerjsonloader-loadbpe.md) for declaring `byte_fallback`.

A refusal is the correct outcome: the alternative is embeddings that do not match the model and
carry nothing to say so.

## Exchanging a float block with numpy

[`NpyFile`](persistence/npyfile.md) is the odd one out here: it reads and writes numpy's `.npy`,
which carries a float matrix and nothing else — no ids, no settings, no schema. It is **not** a
second artifact format, and [`EmbeddingIndex.Save`](search/embeddingindex-save.md) is unchanged.
It exists so vectors can come from numpy and go back to it.

It is bounded like everything else on this page, and it refuses the same class of thing the
loaders do — most pointedly `descr: '|O'`, numpy's object dtype, whose payload is a pickle.

## Types

| Type | What it is |
| --- | --- |
| [`ArtifactLoadOptions`](persistence/artifactloadoptions.md) | The five bounds every load here is held to. |
| [`BpeFilesLoader`](persistence/bpefilesloader.md) | The `vocab.json` + `merges.txt` pair GPT-2 ships. |
| [`NpyBlock`](persistence/npyblock.md) | A float block read from a `.npy`, with its shape. |
| [`NpyFile`](persistence/npyfile.md) | numpy's `.npy`, for exchanging a float matrix. |
| [`SentencePieceModelLoader`](persistence/sentencepiecemodelloader.md) | The trained `spiece.model`. |
| [`TokenizerJsonLoader`](persistence/tokenizerjsonloader.md) | A HuggingFace `tokenizer.json`, whichever model it declares. |
| [`VocabTxtLoader`](persistence/vocabtxtloader.md) | A BERT-style `vocab.txt`. |

## See also

- [Embeddings, end to end](../../guides/embeddings.md) — "Loading vocabularies", with the models
  that are refused.
- [Python → C# equivalence](../../equivalence.md) — the loader rows, with the quirks reproduced.
