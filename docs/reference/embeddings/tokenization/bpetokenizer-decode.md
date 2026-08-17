# BpeTokenizer.Decode

Ids back to text, exactly.

<!-- docs-declaration -->

```csharp
public string Decode(IReadOnlyList<int> ids, bool skipSpecialTokens = false)
public string Decode(ReadOnlySpan<int> ids, bool skipSpecialTokens = false)
```

**Parameters** — `ids` are the ids to turn back into text. `skipSpecialTokens` drops control
tokens rather than rendering them.

**Returns** — `string`, the text those ids encode.

**Example** — a round trip.

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
string text = tokenizer.Decode(encoded.Ids);  // => token
```

**Remarks** — byte-level BPE round-trips **exactly**, and that is the property that makes decoding
worth having: the vocabulary covers all 256 byte values through printable stand-ins, so emoji,
mixed scripts and even malformed UTF-8 come back as they went in.

`skipSpecialTokens` is what you want when showing a generated sequence to a person, and not what
you want when comparing against a reference that includes them.

Neither [`WordPieceTokenizer`](wordpiecetokenizer.md) nor
[`SentencePieceTokenizer`](sentencepiecetokenizer.md) offers this, which is why
[`ISubwordTokenizer`](isubwordtokenizer.md) does not declare it — a promise two of the three
cannot keep.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BpeTokenizer.Encode`](bpetokenizer-encode.md).
