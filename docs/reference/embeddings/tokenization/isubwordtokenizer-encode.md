# ISubwordTokenizer.Encode

One string to tokens and ids.

<!-- docs-declaration -->

```csharp
public TokenizationResult Encode(string text)
```

**Parameters** — `text` is the string to encode. Empty is legal and encodes to nothing.

**Returns** — [`TokenizationResult`](tokenizationresult.md): the token strings and their ids, the
same length and in the same order.

**Example** — one word that splits, one that does not.

```csharp
using Lodestar.Embeddings.Tokenization;

var vocab = new Dictionary<string, int>(StringComparer.Ordinal)
{
    ["[UNK]"] = 0, ["token"] = 1, ["##ize"] = 2, ["text"] = 3,
};
ISubwordTokenizer tokenizer = new WordPieceTokenizer(
    vocab, unkToken: "[UNK]", continuationPrefix: "##", maxCharsPerWord: 100, lowercase: true);

TokenizationResult encoded = tokenizer.Encode("tokenize text");

string first = encoded.Tokens[0];  // => token
string second = encoded.Tokens[1];  // => ##ize
```

**Remarks** — no special tokens are added. Encoding produces what the *model's vocabulary* says
the text is, and wrapping that in `[CLS]`/`[SEP]` or their equivalents belongs to
[`BatchEncoder`](batchencoder.md), because which tokens wrap a sequence depends on the model
rather than on the text.

A word the vocabulary cannot cover becomes the unknown token — one of it, not one per character.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TokenizationResult`](tokenizationresult.md), [`BatchEncoder`](batchencoder.md).
