# SentencePieceTokenizer.Encode

Tokens and ids for one string.

<!-- docs-declaration -->

```csharp
public TokenizationResult Encode(string text)
```

**Parameters** — `text` is the string to encode. No pre-tokenization happens.

**Returns** — [`TokenizationResult`](tokenizationresult.md), the pieces and their ids.

**Example** — the space is inside the token, not between them.

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

string second = encoded.Tokens[1];  // => ▁beta
int id = encoded.Ids[1];  // => 3
```

**Remarks** — both tokens carry `▁` because both words follow a boundary — the first at the start
of the stream, the second after a space. Reading a tokenization, a piece **without** `▁` is a
continuation of the previous one, which is how to spot where a word was split.

Encoding maximises the summed score over segmentations rather than taking the longest match, so it
can prefer two pieces to one where the two score better together.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SentencePiece`](sentencepiece.md), [`TokenizationResult`](tokenizationresult.md).
