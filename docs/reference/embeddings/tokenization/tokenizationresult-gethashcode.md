# TokenizationResult.GetHashCode

A hash consistent with the element-by-element equality.

<!-- docs-declaration -->

```csharp
public int GetHashCode()
```

**Returns** — `int`, derived from the ids in order.

**Example** — equal results hash alike, which is the contract.

```csharp
using Lodestar.Embeddings.Tokenization;

var vocab = new Dictionary<string, int>(StringComparer.Ordinal)
{
    ["[UNK]"] = 0, ["token"] = 1, ["##ize"] = 2, ["text"] = 3,
};
var tokenizer = new WordPieceTokenizer(
    vocab, unkToken: "[UNK]", continuationPrefix: "##", maxCharsPerWord: 100, lowercase: true);

TokenizationResult first = tokenizer.Encode("tokenize");
TokenizationResult second = tokenizer.Encode("tokenize");

bool consistent = first.Equals(second) && first.GetHashCode() == second.GetHashCode();  // => True
```

**Remarks** — the ids carry the hash and the tokens do not, because the two are redundant: a
token and its id determine each other within one vocabulary, and hashing both would cost twice for
nothing. Two results from **different** vocabularies could hash alike while holding different
tokens, which is a collision and permitted.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TokenizationResult.Equals`](tokenizationresult-equals.md).
