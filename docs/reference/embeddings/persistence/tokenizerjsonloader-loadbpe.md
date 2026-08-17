# TokenizerJsonLoader.LoadBpe

Reads the BPE model a `tokenizer.json` declares.

<!-- docs-declaration -->

```csharp
public static BpeVocabulary LoadBpe(Stream source, ArtifactLoadOptions options = null)
public static BpeVocabulary LoadBpe(string path, ArtifactLoadOptions options = null)
```

**Parameters** — `source` is a readable stream, never disposed here; `path` is the file to read. `options` bounds what will be accepted and defaults to
[`ArtifactLoadOptions`](artifactloadoptions.md)'s own defaults.

**Returns** — `BpeVocabulary`, with the merge list, the split pattern and the byte-level flag all read from
the file.

**Exceptions** — `ArgumentNullException` for a null source or path. `InvalidDataException`
when the file declares a different model, declares a pipeline this package does not
reproduce, or exceeds a bound in `options` — the message names what was refused and why.

**Example** — the preferred BPE route, and the refusal worth knowing about.

<!-- docs-run: skip - the file is a model artifact, and model artifacts are never committed (CONTRIBUTING.md) -->

```csharp
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

BpeVocabulary vocab = TokenizerJsonLoader.LoadBpe("gpt2/tokenizer.json");
```

**Remarks** — **Prefer this over [`BpeFilesLoader.Load`](bpefilesloader-load.md)** whenever the checkpoint
ships a `tokenizer.json`. It reads `ignore_merges`, the split pattern together with its `behavior`
and `invert` flag, and the byte-level flag straight from the file — every one of which is a
parameter the older two-file route asks a caller to get right.

**Llama-2 and Mistral v0.1 are refused here by name.** They are trained as SentencePiece BPE with
a `Metaspace` pre-tokenizer and `byte_fallback`, a third pipeline distinct from both lineages this
package implements. A real file of theirs declares `model.type == "BPE"`, so this is the call that
reaches `byte_fallback`, and the exception says what would go wrong: Python resolves an uncovered
character into `<0x..>` byte pieces where this tokenizer emits the unknown piece, so loading it
anyway would produce embeddings that do not match the model.

`docs/decisions/0017-bpe-parity-scope.md` has the parity scope — end to end for GPT-2 and the
classic lineage, split-pattern only for Llama-3 and Qwen2 — and a known split divergence above the
Basic Multilingual Plane.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BpeFilesLoader.Load`](bpefilesloader-load.md),
[`TokenizerJsonLoader.LoadBpeAsync`](tokenizerjsonloader-loadbpeasync.md),
[`TokenizerJsonLoader`](tokenizerjsonloader.md).
