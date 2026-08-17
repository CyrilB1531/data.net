# SentencePiece

One piece of a SentencePiece vocabulary: its text, its score, its id.

<!-- docs-declaration -->

```csharp
public readonly record struct SentencePiece
```

**Properties** — `Piece` is the token text, `Score` its log probability, `Id` its id.

**Example** — the four pieces of a tiny vocabulary.

```csharp
using Lodestar.Embeddings.Tokenization;

var piece = new SentencePiece("▁alpha", -1.5, 2);

string text = piece.Piece;  // => ▁alpha
double score = piece.Score;  // => -1.5
```

**Remarks** — the leading `▁` (U+2581, not an underscore) is **part of the token**, and it means
"a space came before this". That is what lets SentencePiece work with no pre-tokenizer: the word
boundary is inside the vocabulary rather than assumed by a regex, so a language written without
spaces tokenizes the same way as one written with them.

`Score` is what the unigram algorithm maximises: encoding picks the segmentation whose scores sum
highest, which is why a lower-scoring piece can still be chosen when it enables a better whole.
It is a log probability, so it is negative and closer to zero is more likely.

A `readonly record struct`, so it is copied rather than referenced and compares by value.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SentencePieceVocabulary`](sentencepiecevocabulary.md),
[`SentencePieceType`](sentencepiecetype.md).
