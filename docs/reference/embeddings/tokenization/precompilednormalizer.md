# PrecompiledNormalizer

SentencePiece's charsmap normalization: the character folding a model was trained with.

<!-- docs-declaration -->

```csharp
public sealed class PrecompiledNormalizer
```

**Properties** — `CharsMapLength` is the size in bytes of the compiled trie it was built from.

**Example** — the normalizer a model shipped, applied before tokenizing.

<!-- docs-run: skip - a precompiled charsmap is a binary trie shipped inside a spiece.model, and model artifacts are never committed (CONTRIBUTING.md) -->

```csharp
using Lodestar.Embeddings.Tokenization;

// charsMap is the precompiled_charsmap blob from the model's own spiece.model.
byte[] charsMap = File.ReadAllBytes("spiece.model");

PrecompiledNormalizer normalizer = PrecompiledNormalizer.FromCharsMap(charsMap);

string folded = normalizer.Normalize("Ｈｅｌｌｏ");
```

**Remarks** — this is not Unicode normalization by a named form. It is a **trie compiled into the
model file**, mapping character sequences to replacements, and it encodes decisions the model's
authors made — full-width to half-width, some accents stripped, some kept. Substituting NFKC for it
gives text the model was not trained on.

A vocabulary's [`Normalizer`](sentencepiecevocabulary.md) is `null` when the model ships no
charsmap, and then the text reaches the tokenizer unchanged. A stock T5, ALBERT, XLM-R or camemBERT
ships one; a model built by hand usually does not.

**Every fence on these pages is `docs-run: skip`** for the same reason the ONNX pages are: the
input is a binary artifact and none is committed. They are still compiled, so a renamed member
still fails CI.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SentencePieceVocabulary`](sentencepiecevocabulary.md),
[`SentencePieceTokenizer`](sentencepiecetokenizer.md).

## Members

| Member | What it does |
| --- | --- |
| [`PrecompiledNormalizer.Equals`](precompilednormalizer-equals.md) | Value equality over the charsmap. |
| [`PrecompiledNormalizer.FromCharsMap`](precompilednormalizer-fromcharsmap.md) | Build one from a model's compiled trie. |
| [`PrecompiledNormalizer.GetHashCode`](precompilednormalizer-gethashcode.md) | A hash consistent with it. |
| [`PrecompiledNormalizer.Normalize`](precompilednormalizer-normalize.md) | Apply the folding to one string. |
