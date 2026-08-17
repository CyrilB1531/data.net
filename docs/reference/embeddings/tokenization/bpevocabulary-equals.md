# BpeVocabulary.Equals

Value equality over the vocabulary, the merges and every flag.

<!-- docs-declaration -->

```csharp
public bool Equals(BpeVocabulary other)
```

**Parameters** — `other` is the model to compare against.

**Returns** — `bool`, true when the entries, the merge list **in order**, and all the flags agree.

**Example** — the same model built twice.

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

var same = new BpeVocabulary(vocab, merges)
{
    ByteLevel = true,
    PreTokenizerPattern = BpePatterns.Gpt2,
    PreSplit = null,
};

bool equal = model.Equals(same);  // => True
```

**Remarks** — the merges are compared **in order**, because their order is their meaning: the same
pairs ranked differently are a different tokenizer, and calling those equal would hide the
difference that matters most.

The dictionary is compared by content rather than by reference, which a `record`'s synthesised
equality would not do.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BpeVocabulary`](bpevocabulary.md), [`MergePair`](mergepair.md).
