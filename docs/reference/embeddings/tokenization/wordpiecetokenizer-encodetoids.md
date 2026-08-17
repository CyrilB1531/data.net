# WordPieceTokenizer.EncodeToIds

Ids only, without building the token strings.

<!-- docs-declaration -->

```csharp
public IReadOnlyList<int> EncodeToIds(string text)
```

**Parameters** — `text` is the string to encode.

**Returns** — `IReadOnlyList<int>`, the same ids [`Encode`](wordpiecetokenizer-encode.md) would
give.

**Example** — the same ids, one allocation fewer.

```csharp
using Lodestar.Embeddings.Tokenization;

var vocab = new Dictionary<string, int>(StringComparer.Ordinal)
{
    ["[UNK]"] = 0, ["token"] = 1, ["##ize"] = 2, ["text"] = 3,
};
var tokenizer = new WordPieceTokenizer(
    vocab, unkToken: "[UNK]", continuationPrefix: "##", maxCharsPerWord: 100, lowercase: true);

IReadOnlyList<int> ids = tokenizer.EncodeToIds("tokenize text");

int count = ids.Count;  // => 3
int first = ids[0];  // => 1
```

**Remarks** — the token strings are what make a tokenization readable, and building them costs a
list of strings per call. When the ids go straight to a model and nobody will look at them, this
skips that — which is the hot path for encoding a corpus.

Reach for [`Encode`](wordpiecetokenizer-encode.md) while developing and this once it works. The
ids are identical; only the debuggability differs.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`WordPieceTokenizer.Encode`](wordpiecetokenizer-encode.md).
