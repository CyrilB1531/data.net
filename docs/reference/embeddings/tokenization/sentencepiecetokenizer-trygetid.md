# SentencePieceTokenizer.TryGetId

The id of a piece, if the vocabulary holds it.

<!-- docs-declaration -->

```csharp
public bool TryGetId(string token, out int id)
```

**Parameters** — `token` is the piece **as the vocabulary spells it**, `▁` included where the
piece begins a word. `id` receives the piece's id, or `0` when the lookup fails.

**Returns** — `bool`, true when found; `id` is the id then and `0` otherwise.

**Example** — the prefixed form is found and the bare one is not.

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

bool prefixed = tokenizer.TryGetId("▁alpha", out int withSpace);  // => True
bool bare = tokenizer.TryGetId("alpha", out int without);  // => False
```

**Remarks** — `alpha` is absent and `▁alpha` is present, which is the single most common surprise
in this namespace. The `▁` is a character of the token, so looking a word up without it asks for a
piece the vocabulary does not have.

It is U+2581 LOWER ONE EIGHTH BLOCK, not an underscore, and pasting the wrong one silently fails
to match.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`ISubwordTokenizer.TryGetId`](isubwordtokenizer-trygetid.md),
[`SentencePiece`](sentencepiece.md).
