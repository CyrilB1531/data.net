# BpeTokenizer.TryGetId

The id of a token, if the vocabulary holds it.

<!-- docs-declaration -->

```csharp
public bool TryGetId(string token, out int id)
```

**Parameters** — `token` is the exact vocabulary entry, in its byte-level spelling where the model
is byte-level. `id` receives the token's id, or `0` when the lookup fails.

**Returns** — `bool`, true when found; `id` is the id then and `0` otherwise.

**Example** — an entry that exists, and the byte-level spelling of a leading space.

```csharp
using Lodestar.Embeddings.Tokenization;

var vocab = new Dictionary<string, int>(StringComparer.Ordinal)
{
    ["Ġ"] = 0, ["t"] = 1, ["o"] = 2, ["k"] = 3, ["e"] = 4, ["n"] = 5,
    ["to"] = 6, ["ken"] = 7, ["token"] = 8, ["Ġtoken"] = 9, ["ke"] = 10,
};
var merges = new List<MergePair> { new("t", "o"), new("k", "e"), new("ke", "n") };
var model = new BpeVocabulary(vocab, merges)
{
    ByteLevel = true,
    PreTokenizerPattern = BpePatterns.Gpt2,
    PreSplit = null,
};
var tokenizer = new BpeTokenizer(model);

bool plain = tokenizer.TryGetId("token", out int id);  // => True
bool spaced = tokenizer.TryGetId("Ġtoken", out int spacedId);  // => True
bool bare = tokenizer.TryGetId(" token", out int missing);  // => False
```

**Remarks** — `" token"` with a real space is **not** found, while `"Ġtoken"` is. In a byte-level
model the space byte is represented by `Ġ` (U+0120), so that is how the entry is spelled and how it
must be looked up. Searching a vocabulary with a literal space is the most common reason a token
"is missing" when it is not.

`token` is found here although [`Encode`](bpetokenizer-encode.md) never produces it — a lookup
asks what the vocabulary holds, not what the merges can reach.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`ISubwordTokenizer.TryGetId`](isubwordtokenizer-trygetid.md),
[`BpeVocabulary`](bpevocabulary.md).
