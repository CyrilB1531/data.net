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

**Exceptions** — `ArgumentOutOfRangeException` when an id falls outside the vocabulary.
Decoding cannot silently skip one, since the caller would get back a shorter text than it
asked for with nothing said about it. Nothing else on this path throws: a byte sequence
that is not well-formed UTF-8 becomes U+FFFD, which is what
[decision 0023](../../../decisions/0023-byte-level-decode-substitutes.md) settled.

**Remarks** — byte-level BPE round-trips **exactly**, and that is the property that makes decoding
worth having: the vocabulary covers all 256 byte values through printable stand-ins, so emoji,
mixed scripts and even malformed UTF-8 come back as they went in.

**The SentencePiece-BPE lineage is the one exception.** Where the model declares the whitespace
escape — a `Metaspace` pre-tokenizer or a `Prepend` + `Replace` normalizer, which
[`TokenizerJsonLoader.LoadBpe`](../persistence/tokenizerjsonloader-loadbpe.md) reads — that escape
is an encode-side transform in this package, and a `Metaspace` `decoder` block is accepted without
being applied. So the text comes back with its replacement symbols in place of the spaces, and
`Decode(Encode(x))` is not `x`. [Decision 0062](../../../decisions/0062-the-two-metaspace-spellings-part-on-the-prepend-twice.md)
records it; undoing the escape belongs with the lot that reproduces the rest of that decoder.

`skipSpecialTokens` is what you want when showing a generated sequence to a person, and not what
you want when comparing against a reference that includes them.

Neither [`WordPieceTokenizer`](wordpiecetokenizer.md) nor
[`SentencePieceTokenizer`](sentencepiecetokenizer.md) offers this, which is why
[`ISubwordTokenizer`](isubwordtokenizer.md) does not declare it — a promise two of the three
cannot keep.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BpeTokenizer.Encode`](bpetokenizer-encode.md).
