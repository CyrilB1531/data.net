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
ships a `tokenizer.json`. It reads `ignore_merges`, `byte_fallback`, the split pattern together
with its `behavior` and `invert` flag, and the byte-level flag straight from the file — every one
of which is a parameter the older two-file route asks a caller to get right.

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
**not applied** for a file that does not declare `byte_fallback`, so
[`Decode`](../tokenization/bpetokenizer-decode.md) returns the escaped text, replacement symbols
and all, for such a file.

**Llama-2 and Mistral v0.1 both load.** Both are trained as SentencePiece BPE with
`byte_fallback: true`, and a real file of theirs declares `model.type == "BPE"`, so this is the
call that reaches them. `byte_fallback` is read: an uncovered symbol resolves into one `<0xXX>`
piece per UTF-8 byte, uppercase hexadecimal, rather than the unknown token — the expansion runs
before the merges, on the decorated symbol, so a byte piece merges like any other and a
`continuing_subword_prefix` or `end_of_word_suffix` on it is itself encoded as bytes. What is
still **refused**, by name, is a vocabulary that declares `byte_fallback` without carrying all 256
`<0xXX>` pieces — `tokenizers` degrades such a file silently, to the unknown token or, with none
declared, by dropping the symbol and letting its neighbours merge across the hole, and this
package refuses rather than reproduce either. For such a file the `decoder` block is also read
strictly: a bare `{"type": "ByteFallback"}` or a `Sequence` of exactly `[Replace, ByteFallback,
Fuse, Strip]` in that order — Llama-2's own chain — undoes the byte pieces and, for the four-step
form, the whitespace escape with them; any other shape is refused by name.
[Decision 0063](../../decisions/0063-byte-fallback-requires-the-whole-alphabet-and-its-decoder-is-read-strictly-too.md)
has the measurements and the refusal.

`docs/decisions/0017-bpe-parity-scope.md` has the parity scope — end to end for GPT-2 and the
classic lineage, split-pattern only for Llama-3 and Qwen2 — and a known split divergence above the
Basic Multilingual Plane.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BpeFilesLoader.Load`](bpefilesloader-load.md),
[`TokenizerJsonLoader.LoadBpeAsync`](tokenizerjsonloader-loadbpeasync.md),
[`TokenizerJsonLoader`](tokenizerjsonloader.md).
