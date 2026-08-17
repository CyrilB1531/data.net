# BpeTokenizer

Byte-level and classic BPE — GPT-2, Llama-3, Qwen2 — and the only tokenizer here that decodes.

<!-- docs-declaration -->

```csharp
public sealed class BpeTokenizer : ISubwordTokenizer
```

**Constructor** — takes a [`BpeVocabulary`](bpevocabulary.md).

**Example** — three merges turning five characters into two tokens, and back.

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

TokenizationResult encoded = tokenizer.Encode("token");
string roundTripped = tokenizer.Decode(encoded.Ids);

int pieces = encoded.Tokens.Count;  // => 2
```

**Remarks** — encoding starts from characters and applies the ranked merges in order:
`t` `o` `k` `e` `n` becomes `to` `k` `e` `n`, then `to` `ke` `n`, then `to` `ken`. The
result is two tokens, and no rule joins them because none is listed.

It is the only one of the three that can [`Decode`](bpetokenizer-decode.md), and byte-level is why:
the vocabulary covers all 256 byte values through printable stand-ins, so any input round-trips
exactly — emoji, mixed scripts and malformed UTF-8 alike. WordPiece and SentencePiece have thrown
information away by then.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BpeVocabulary`](bpevocabulary.md), [`MergePair`](mergepair.md),
[`ISubwordTokenizer`](isubwordtokenizer.md).

## Members

| Member | What it does |
| --- | --- |
| [`BpeTokenizer.Decode`](bpetokenizer-decode.md) | Ids back to text, exactly. |
| [`BpeTokenizer.Encode`](bpetokenizer-encode.md) | Tokens and ids for one string. |
| [`BpeTokenizer.TryGetId`](bpetokenizer-trygetid.md) | The id of a token, if the vocabulary holds it. |
