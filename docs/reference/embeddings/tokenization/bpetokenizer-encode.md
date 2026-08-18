# BpeTokenizer.Encode

Tokens and ids for one string.

<!-- docs-declaration -->

```csharp
public TokenizationResult Encode(string text)
```

**Parameters** — `text` is the string to encode.

**Returns** — [`TokenizationResult`](tokenizationresult.md), the merged symbols and their ids.

**Example** — the merges applied in rank order.

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

string first = encoded.Tokens[0];  // => to
string second = encoded.Tokens[1];  // => ken
```

**Exceptions** — `ArgumentException` when a byte-level vocabulary is missing one of the
256 base alphabet tokens — a broken model rather than ordinary uncovered input — or, once a
normalizer is declared, when an unpaired surrogate falls in a gap, since
`string.Normalize` refuses that before the byte-level re-encoding is reached.
`EncoderFallbackException` when a byte-level model re-encodes text holding an unpaired
UTF-16 surrogate: byte-level BPE is lossless only over well-formed UTF-16, so it throws
rather than substituting. **The classic path never encodes to UTF-8 and so cannot raise
it** — measured, an unpaired surrogate through a classic model returns normally.

**Remarks** — `token` is in the vocabulary as a single entry, and the result is still two tokens.
That is not a bug: BPE reaches a symbol only by **merging**, and no rule joins `to` with `ken`.
A vocabulary entry with no path of merges to it is unreachable, which is a real property of hand-built
models and a good reason to check a tokenization rather than assume it.

The pre-tokenizer runs first and merges never cross its boundaries, so
[`BpePatterns`](bpepatterns.md) decides what the merge loop even sees.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BpeTokenizer.Decode`](bpetokenizer-decode.md), [`MergePair`](mergepair.md).
