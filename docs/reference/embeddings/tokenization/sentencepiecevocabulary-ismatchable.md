# SentencePieceVocabulary.IsMatchable

Whether the encoder may produce a piece from text.

<!-- docs-declaration -->

```csharp
public bool IsMatchable(int id)
```

**Parameters** — `id` is the piece's id.

**Returns** — `bool`, true when text may encode to it.

**Example** — a control token is not matchable; ordinary text is.

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

bool control = vocabulary.IsMatchable(0);  // => False
bool normal = vocabulary.IsMatchable(2);  // => True
```

**Exceptions** — `ArgumentOutOfRangeException` when `id` falls outside
[`Types`](sentencepiecevocabulary.md). Measured on a two-piece vocabulary: `5` and `-1`
both throw, `1` returns. The record is a data carrier and does not itself require
`Pieces` and `Types` to agree in length —
[`SentencePieceTokenizer`](sentencepiecetokenizer.md) is what refuses a vocabulary where
they do not, because reporting it here as a raw index failure would name neither the
argument nor the reason.

**Remarks** — id `0` is the unknown piece and id `1` is `<s>`; neither may be produced by
matching text. That is a **security property** as much as a correctness one: if the literal string
`<s>` encoded to the control token, any input could forge a sequence boundary and change what the
model thinks it was given.

The rule follows [`SentencePieceType`](sentencepiecetype.md) — `Normal`, `UserDefined` and `Byte`
are matchable, `Unknown`, `Control` and `Unused` are not — and the encoder consults this rather
than the type directly.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SentencePieceType`](sentencepiecetype.md),
[`SentencePieceVocabulary`](sentencepiecevocabulary.md).
