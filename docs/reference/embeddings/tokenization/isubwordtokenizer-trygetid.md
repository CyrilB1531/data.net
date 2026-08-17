# ISubwordTokenizer.TryGetId

The id of a token, if the vocabulary holds it.

<!-- docs-declaration -->

```csharp
public bool TryGetId(string token, out int id)
```

**Parameters** — `token` is the exact token string, as the vocabulary spells it — including a
continuation prefix or a `▁`, where the family uses one. `id` receives the token's id, or `0`
when the lookup fails.

**Returns** — `bool`, true when the vocabulary holds it; `id` is the token's id then and `0`
otherwise.

**Example** — a token that exists and one that does not.

```csharp
using Lodestar.Embeddings.Tokenization;

var vocab = new Dictionary<string, int>(StringComparer.Ordinal)
{
    ["[UNK]"] = 0, ["token"] = 1, ["##ize"] = 2, ["text"] = 3,
};
ISubwordTokenizer tokenizer = new WordPieceTokenizer(
    vocab, unkToken: "[UNK]", continuationPrefix: "##", maxCharsPerWord: 100, lowercase: true);

bool known = tokenizer.TryGetId("text", out int id);  // => True
bool unknown = tokenizer.TryGetId("absent", out int missing);  // => False
```

**Remarks** — `missing` is `0` rather than `-1`, and `0` is a perfectly good id in most
vocabularies — usually the unknown token's. **Read the `bool`, never the `int` alone.**

It is a lookup, not an encode: `TryGetId("tokenize")` fails on a vocabulary that would happily
encode that word as `token` + `##ize`, because no single entry spells it. This is what
[`BatchEncoder`](batchencoder.md) uses to resolve a template's `[CLS]` against a vocabulary, which
is why a missing special token is caught at construction.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`ISubwordTokenizer.Encode`](isubwordtokenizer-encode.md),
[`SpecialTokenTemplate`](specialtokentemplate.md).
