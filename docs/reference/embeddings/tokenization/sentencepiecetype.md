# SentencePieceType

What a piece is for — normal text, a control token, or nothing.

<!-- docs-declaration -->

```csharp
public enum SentencePieceType { Normal, Unknown, Control, UserDefined, Unused, Byte }
```

**Members** — `Normal` is ordinary text and the only kind the encoder matches freely. `Unknown` is
the fallback piece. `Control` is a token like `<s>` that the model inserts and the encoder must
never match from text. `UserDefined` is a token added after training, matched literally. `Unused`
is a placeholder occupying an id and nothing else. `Byte` is one of the 256 byte fallbacks, used
when a character has no piece at all.

**Example** — the type decides whether the encoder may produce a piece.

```csharp
using Lodestar.Embeddings.Tokenization;

SentencePieceType control = SentencePieceType.Control;
bool isControl = control == SentencePieceType.Control;  // => True
```

**Remarks** — the distinction that matters is **matchable or not**. A `Control` piece exists so the
model can be given `<s>`, and letting text containing the literal string `<s>` encode to it would
let any input forge a control token. `Unused` is the same in a different way: an id kept so the
vocabulary size stays fixed, matching nothing.

[`SentencePieceVocabulary.IsMatchable`](sentencepiecevocabulary-ismatchable.md) is that question
asked directly, and is what the encoder consults.

`Byte` is what makes a SentencePiece model with byte fallback total: no input is unencodable,
because every byte has a piece.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SentencePieceVocabulary`](sentencepiecevocabulary.md),
[`SentencePiece`](sentencepiece.md).
