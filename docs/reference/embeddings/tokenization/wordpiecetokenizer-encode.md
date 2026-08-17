# WordPieceTokenizer.Encode

Tokens and ids for one string.

<!-- docs-declaration -->

```csharp
public TokenizationResult Encode(string text)
```

**Parameters** — `text` is the string to encode.

**Returns** — [`TokenizationResult`](tokenizationresult.md), tokens and ids of the same length.

**Example** — a word outside the vocabulary becomes one unknown token.

```csharp
using Lodestar.Embeddings.Tokenization;

var vocab = new Dictionary<string, int>(StringComparer.Ordinal)
{
    ["[UNK]"] = 0, ["token"] = 1, ["##ize"] = 2, ["text"] = 3,
};
var tokenizer = new WordPieceTokenizer(
    vocab, unkToken: "[UNK]", continuationPrefix: "##", maxCharsPerWord: 100, lowercase: true);

TokenizationResult known = tokenizer.Encode("tokenize");
TokenizationResult unknown = tokenizer.Encode("zzz");

int pieces = known.Tokens.Count;  // => 2
string missing = unknown.Tokens[0];  // => [UNK]
```

**Remarks** — `zzz` produces a **single** `[UNK]`, not three. That is WordPiece's rule and it
matters when reading a tokenization: a burst of unknown tokens means several unmatched words, never
one long one.

Lowercasing, when enabled, happens before matching, so the vocabulary only needs lowercase entries.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`WordPieceTokenizer.EncodeToIds`](wordpiecetokenizer-encodetoids.md),
[`TokenizationResult`](tokenizationresult.md).
