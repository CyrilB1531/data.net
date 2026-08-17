# TokenizationResult.Equals

Value equality over the tokens and the ids.

<!-- docs-declaration -->

```csharp
public bool Equals(TokenizationResult other)
```

**Parameters** — `other` is the result to compare against.

**Returns** — `bool`, true when both sequences hold the same values in the same order.

**Example** — encoding the same text twice gives equal results.

```csharp
using Lodestar.Embeddings.Tokenization;

var vocab = new Dictionary<string, int>(StringComparer.Ordinal)
{
    ["[UNK]"] = 0, ["token"] = 1, ["##ize"] = 2, ["text"] = 3,
};
var tokenizer = new WordPieceTokenizer(
    vocab, unkToken: "[UNK]", continuationPrefix: "##", maxCharsPerWord: 100, lowercase: true);

bool same = tokenizer.Encode("tokenize").Equals(tokenizer.Encode("tokenize"));  // => True
```

**Remarks** — a `record`'s synthesised equality would compare the two lists **by reference**, so
two encodings of the same text would be unequal. That is never the answer anyone wants from a
value type, which is why this is written by hand and compares element by element.

Useful mostly in tests, where "did this change" is the question and the ids alone are hard to read.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TokenizationResult`](tokenizationresult.md),
[`TokenizationResult.GetHashCode`](tokenizationresult-gethashcode.md).
