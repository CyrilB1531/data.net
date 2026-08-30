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

**The SentencePiece-BPE whitespace escape is read, in both of the spellings a file uses for it.**
A `Metaspace` pre-tokenizer whose `split` is off — Mistral v0.1's spelling — and a normalizer
`Sequence` of `Prepend` then `Replace` — Llama-2's. Both become one transform the returned
vocabulary carries and [`BpeTokenizer.Encode`](../tokenization/bpetokenizer-encode.md) applies:
every space becomes the replacement, and the replacement is prepended. The two differ only on the
prepend, and both differences are reproduced — a `Metaspace` block skips the prepend when the
escaped piece already begins with the replacement, and its `prepend_scheme` of `first` means the
opening piece rather than the whole text, where an added token counts as a piece. `prepend_scheme`
is read in all three of its values, with the pre-0.14 `add_prefix_space` standing in when it is
absent. `docs/decisions/0050-the-sentencepiece-bpe-lineage-stays-a-bpe-model.md` decided the shape
and `0062-the-two-metaspace-spellings-part-on-the-prepend-twice.md` bounds it.

Three shapes around it are **refused** by name rather than reduced: a `Metaspace` whose `split` is
on, since there is no pattern here for its own segmentation; a file writing *both* spellings, since
nothing measured says which one it means; and a normalizer `Sequence` that names `Prepend` or
`Replace` and is not exactly those two in that order.

The escape is an encode-side transform in this package. A `Metaspace` `decoder` block loads and is
**not applied**, so [`Decode`](../tokenization/bpetokenizer-decode.md) returns the escaped text,
replacement symbols and all, for a file declaring one.

**Llama-2 and Mistral v0.1 are still refused here by name**, now on `byte_fallback` alone. Both are
trained as SentencePiece BPE, and a real file of theirs declares `model.type == "BPE"`, so this is
the call that reaches it. The exception says what would go wrong: Python resolves an uncovered
character into `<0x..>` byte pieces where this tokenizer emits the unknown piece, so loading it
anyway would produce embeddings that do not match the model.

`docs/decisions/0017-bpe-parity-scope.md` has the parity scope — end to end for GPT-2 and the
classic lineage, split-pattern only for Llama-3 and Qwen2 — and a known split divergence above the
Basic Multilingual Plane.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BpeFilesLoader.Load`](bpefilesloader-load.md),
[`TokenizerJsonLoader.LoadBpeAsync`](tokenizerjsonloader-loadbpeasync.md),
[`TokenizerJsonLoader`](tokenizerjsonloader.md).
