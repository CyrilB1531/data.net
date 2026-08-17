# PrecompiledNormalizer.FromCharsMap

Build one from a model's compiled trie.

<!-- docs-declaration -->

```csharp
public static PrecompiledNormalizer FromCharsMap(byte[] charsMap)
```

**Parameters** — `charsMap` is the `precompiled_charsmap` blob from a `spiece.model` or a
`tokenizer.json` normalizer.

**Returns** — `PrecompiledNormalizer` ready to [`Normalize`](precompilednormalizer-normalize.md).

**Exceptions** — `InvalidDataException` when the blob is not a usable charsmap: too short to carry
its own trie size, or carrying an empty trie.

**Example** — the refusals, which are what a caller can actually reach without a model file.

```csharp
using Lodestar.Embeddings.Tokenization;

string tooShort = "";
try { PrecompiledNormalizer.FromCharsMap([]); }
catch (InvalidDataException e) { tooShort = e.Message; }

string emptyTrie = "";
try { PrecompiledNormalizer.FromCharsMap([0, 0, 0, 0]); }
catch (InvalidDataException e) { emptyTrie = e.Message; }

bool bothRefused = tooShort.Length > 0 && emptyTrie.Length > 0;  // => True
```

**Remarks** — a charsmap cannot be synthesised, which is why the example above shows what happens
when you try. Four zero bytes are a well-formed *header* declaring a trie of nothing, and that is
refused separately from a blob too short to have a header at all — two different ways a file can
be wrong, and the messages say which.

Refusing rather than accepting an empty trie matters: a normalizer that silently does nothing
would leave text unfolded and the model would see input it was not trained on, with no error
anywhere.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`PrecompiledNormalizer`](precompilednormalizer.md),
[`PrecompiledNormalizer.Normalize`](precompilednormalizer-normalize.md).
