# WordPieceTokenizer.TryGetId

The id of a token, if the vocabulary holds it.

<!-- docs-declaration -->

```csharp
public bool TryGetId(string token, out int id)
```

**Parameters** — `token` is the exact vocabulary entry, continuation prefix included where one
applies. `id` receives the token's id, or `0` when the lookup fails.

**Returns** — `bool`, true when found; `id` is the id then and `0` otherwise.

**Example** — a whole word and a continuation piece.

```csharp
using Lodestar.Embeddings.Tokenization;

var vocab = new Dictionary<string, int>(StringComparer.Ordinal)
{
    ["[UNK]"] = 0, ["token"] = 1, ["##ize"] = 2, ["text"] = 3,
};
var tokenizer = new WordPieceTokenizer(
    vocab, unkToken: "[UNK]", continuationPrefix: "##", maxCharsPerWord: 100, lowercase: true);

bool whole = tokenizer.TryGetId("text", out int textId);  // => True
bool piece = tokenizer.TryGetId("##ize", out int pieceId);  // => True
bool absent = tokenizer.TryGetId("tokenize", out int missing);  // => False
```

**Remarks** — `tokenize` is **not** found, though the tokenizer encodes it happily: this is a
vocabulary lookup, not an encode, and no single entry spells that word. Asking for the prefixed
form requires writing the prefix, because that is how the entry is spelled.

`missing` is `0`, which is a real id in most vocabularies. Read the `bool`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`ISubwordTokenizer.TryGetId`](isubwordtokenizer-trygetid.md),
[`WordPieceVocabulary`](wordpiecevocabulary.md).
