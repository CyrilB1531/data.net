# SentencePieceVocabulary.GetHashCode

A hash consistent with that equality.

<!-- docs-declaration -->

```csharp
public int GetHashCode()
```

**Returns** — `int`, over the count and the special ids rather than every piece.

**Example** — equal vocabularies hash alike.

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
bool hashesAlike = vocabulary.GetHashCode() == same.GetHashCode();  // => True
```

**Remarks** — hashing thirty-two thousand pieces on every call is a cost that buys nothing, so the
hash reads the size and the four special ids. Two different vocabularies of the same size and
special ids collide, which is permitted:
[`Equals`](sentencepiecevocabulary-equals.md) is what decides, and it reads everything.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SentencePieceVocabulary.Equals`](sentencepiecevocabulary-equals.md).
