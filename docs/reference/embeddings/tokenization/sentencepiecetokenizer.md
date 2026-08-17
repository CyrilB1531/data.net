# SentencePieceTokenizer

Unigram encoding over a SentencePiece vocabulary — T5, ALBERT, XLM-R, camemBERT.

<!-- docs-declaration -->

```csharp
public sealed class SentencePieceTokenizer : ISubwordTokenizer
```

**Constructor** — takes a [`SentencePieceVocabulary`](sentencepiecevocabulary.md).

**Example** — two words, each one piece, each carrying its space.

```csharp
using Lodestar.Embeddings.Tokenization;

SentencePiece[] pieces =
[
    new SentencePiece("<unk>", 0.0, 0),
    new SentencePiece("<s>", 0.0, 1),
    new SentencePiece("▁alpha", -1.5, 2),
    new SentencePiece("▁beta", -2.5, 3),
];
SentencePieceType[] types =
[
    SentencePieceType.Unknown,
    SentencePieceType.Control,
    SentencePieceType.Normal,
    SentencePieceType.Normal,
];
var vocabulary = new SentencePieceVocabulary(pieces, types, UnkId: 0, BosId: 1, EosId: -1, PadId: -1);

var tokenizer = new SentencePieceTokenizer(vocabulary);
TokenizationResult encoded = tokenizer.Encode("alpha beta");

int count = encoded.Tokens.Count;  // => 2
string first = encoded.Tokens[0];  // => ▁alpha
```

**Remarks** — no pre-tokenizer runs. The text is a stream, the space is encoded as `▁` inside the
pieces, and the segmentation is whichever one maximises the sum of the scores. That is the whole
difference from WordPiece, which splits on whitespace first and then matches greedily inside each
word.

The practical consequence: leading spaces matter. `"alpha"` and `" alpha"` can tokenize
differently, because one begins a word and the other continues the stream — and a model trained on
sentences expects the `▁`.

Where the vocabulary carries a [`PrecompiledNormalizer`](precompilednormalizer.md), it runs first.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SentencePieceVocabulary`](sentencepiecevocabulary.md),
[`ISubwordTokenizer`](isubwordtokenizer.md).

## Members

| Member | What it does |
| --- | --- |
| [`SentencePieceTokenizer.Encode`](sentencepiecetokenizer-encode.md) | Tokens and ids for one string. |
| [`SentencePieceTokenizer.TryGetId`](sentencepiecetokenizer-trygetid.md) | The id of a piece, if the vocabulary holds it. |
