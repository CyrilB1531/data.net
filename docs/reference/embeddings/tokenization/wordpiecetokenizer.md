# WordPieceTokenizer

Longest-match sub-word encoding with a continuation prefix — BERT's tokenizer.

<!-- docs-declaration -->

```csharp
public sealed class WordPieceTokenizer : ISubwordTokenizer
```

**Constructor** — takes a vocabulary, an unknown token, a continuation prefix, a per-word character
cap and a lowercase flag; or a [`WordPieceVocabulary`](wordpiecevocabulary.md) that carries all of
them.

**Example** — one word that splits into two pieces, one that does not.

```csharp
using Lodestar.Embeddings.Tokenization;

var vocab = new Dictionary<string, int>(StringComparer.Ordinal)
{
    ["[UNK]"] = 0, ["token"] = 1, ["##ize"] = 2, ["text"] = 3,
};
var tokenizer = new WordPieceTokenizer(
    vocab, unkToken: "[UNK]", continuationPrefix: "##", maxCharsPerWord: 100, lowercase: true);

TokenizationResult encoded = tokenizer.Encode("tokenize text");

string continuation = encoded.Tokens[1];  // => ##ize
int count = encoded.Ids.Count;  // => 3
```

**Remarks** — the algorithm is greedy longest-match **within a word**: take the longest vocabulary
entry that prefixes what is left, mark every piece after the first with the continuation prefix,
repeat. `tokenize` becomes `token` + `##ize` because `token` is the longest entry that starts it.

The important failure is all-or-nothing: a word no sequence of pieces can cover becomes **one**
unknown token, not a run of partial ones. Half a word is not a useful signal, and BERT agrees.

`maxCharsPerWord` is a guard rather than a tuning knob — a word longer than it becomes the unknown
token without being attempted, which keeps a pathological input from costing quadratic time.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`WordPieceVocabulary`](wordpiecevocabulary.md),
[`ISubwordTokenizer`](isubwordtokenizer.md), the [embeddings guide](../../../guides/embeddings.md).

## Members

| Member | What it does |
| --- | --- |
| [`WordPieceTokenizer.Encode`](wordpiecetokenizer-encode.md) | Tokens and ids for one string. |
| [`WordPieceTokenizer.EncodeToIds`](wordpiecetokenizer-encodetoids.md) | Ids only, without building the token strings. |
| [`WordPieceTokenizer.TryGetId`](wordpiecetokenizer-trygetid.md) | The id of a token, if the vocabulary holds it. |
