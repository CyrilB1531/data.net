# SentencePieceVocabulary.Equals

Value equality over the pieces, the types and the special ids.

<!-- docs-declaration -->

```csharp
public bool Equals(SentencePieceVocabulary other)
```

**Parameters** — `other` is the vocabulary to compare against.

**Returns** — `bool`, true when every piece, type and special id agrees.

**Example** — two vocabularies built from the same pieces.

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

var same = new SentencePieceVocabulary(pieces, types, UnkId: 0, BosId: 1, EosId: -1, PadId: -1);

bool equal = vocabulary.Equals(same);  // => True
```

**Remarks** — the two arrays are compared element by element. A `record`'s synthesised equality
would compare them by reference, so two vocabularies loaded from the same `spiece.model` would be
unequal — never the useful answer.

The special ids are part of the identity: two vocabularies with identical pieces and a different
`EosId` produce different model input, so calling them equal would hide a real difference.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SentencePieceVocabulary`](sentencepiecevocabulary.md).
