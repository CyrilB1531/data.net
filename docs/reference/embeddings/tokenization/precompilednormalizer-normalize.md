# PrecompiledNormalizer.Normalize

Apply the model's folding to one string.

<!-- docs-declaration -->

```csharp
public string Normalize(string text)
```

**Parameters** — `text` is the string to fold.

**Returns** — `string`, the text after the charsmap's replacements.

**Example** — full-width characters folded as the model expects.

<!-- docs-run: skip - a precompiled charsmap is a binary trie shipped inside a spiece.model, and model artifacts are never committed (CONTRIBUTING.md) -->

```csharp
using Lodestar.Embeddings.Tokenization;

// charsMap is the precompiled_charsmap blob from the model's own spiece.model.
byte[] charsMap = File.ReadAllBytes("spiece.model");

PrecompiledNormalizer normalizer = PrecompiledNormalizer.FromCharsMap(charsMap);

string folded = normalizer.Normalize("Ｈｅｌｌｏ");
```

**Exceptions** — `InvalidDataException` when the charsmap points at a replacement it does
not itself contain. That is a defect in the model file rather than in the input, and it
surfaces here because this is where the trie is walked.

**Remarks** — the replacements are the model's, not a standard's. Two models can fold the same
input differently and both be right, because each was trained on its own folding — which is why
this is a per-model artifact rather than a call to `string.Normalize`.

It runs **before** tokenization, so a piece in the vocabulary is spelled in normalized form, and
looking one up with unnormalized text can fail to match.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`PrecompiledNormalizer.FromCharsMap`](precompilednormalizer-fromcharsmap.md),
[`SentencePieceTokenizer`](sentencepiecetokenizer.md).
