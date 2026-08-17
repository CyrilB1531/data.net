# SentencePieceVocabulary

The pieces, their types, and the four special ids.

<!-- docs-declaration -->

```csharp
public sealed record SentencePieceVocabulary
```

**Properties** — `Pieces` are the [`SentencePiece`](sentencepiece.md) entries and `Types` their
[`SentencePieceType`](sentencepiecetype.md), the same length and index-aligned. `UnkId`, `BosId`,
`EosId` and `PadId` are the four special ids, **`-1` when the model declares none**. `Count` is
how many pieces. `Normalizer` is the [`PrecompiledNormalizer`](precompilednormalizer.md) the
model shipped, or `null`.

**Example** — a four-piece vocabulary with no end or pad token.

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

int count = vocabulary.Count;  // => 4
int noEos = vocabulary.EosId;  // => -1
```

**Remarks** — `-1` rather than `null` for an absent special id, because the ids are `int` and
every real id is non-negative. **Check for it**: passing `-1` to a model as a token id is a
silent corruption rather than an error.

`Types` being a parallel array rather than a field of `SentencePiece` mirrors the `spiece.model`
file, which stores them that way, and keeps the piece a small value type.

A `Normalizer` of `null` means the text reaches the tokenizer unchanged; a stock T5 or XLM-R ships
one, and a model built by hand usually does not.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SentencePieceTokenizer`](sentencepiecetokenizer.md),
[`SentencePieceType`](sentencepiecetype.md).

## Members

| Member | What it does |
| --- | --- |
| [`SentencePieceVocabulary.Equals`](sentencepiecevocabulary-equals.md) | Value equality over pieces, types and ids. |
| [`SentencePieceVocabulary.GetHashCode`](sentencepiecevocabulary-gethashcode.md) | A hash consistent with it. |
| [`SentencePieceVocabulary.IsMatchable`](sentencepiecevocabulary-ismatchable.md) | Whether the encoder may produce a piece. |
