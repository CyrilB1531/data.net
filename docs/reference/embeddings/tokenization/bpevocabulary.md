# BpeVocabulary

A BPE model: the vocabulary, the ranked merges, and the flags that decide how they are applied.

<!-- docs-declaration -->

```csharp
public sealed record BpeVocabulary
```

**Properties** — `Vocab` maps token to id and `Merges` is the **ranked** [`MergePair`](mergepair.md)
list. `ByteLevel` selects byte-level spelling. `AddPrefixSpace` prepends a space to every text.
`IgnoreMerges` short-circuits to a whole-piece vocabulary lookup. `FuseUnk` collapses adjacent
unknown tokens into one. `EndOfWordSuffix` and `ContinuingSubwordPrefix` are the classic-BPE
markers. `UnkToken` is the fallback. `PreTokenizerPattern`, `NoPreTokenizer` and `PreSplit`
decide what the merge loop sees. `NormalizationForms` are applied first. `Count` is the
vocabulary size.

**Example** — a byte-level model with three merges.

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

int size = model.Count;  // => 11
int rules = model.Merges.Count;  // => 3
```

**Remarks** — fifteen properties because a `tokenizer.json` has that many knobs and getting any of
them wrong changes the ids. The ones that surprise:

- **`PreSplit = null` is not the same as `NoPreTokenizer = true`.** The first says "no Split step
  ahead of ByteLevel", which is stock GPT-2; the second says "no pre-tokenizer at all", so the
  merge loop sees the whole text including its spaces.
- **`IgnoreMerges`** makes a whole piece present in the vocabulary win over any merging. Llama-3
  sets it, and without it the same file tokenizes differently.
- **`AddPrefixSpace`** changes every first token of every text. It is a property of the model,
  not a preference.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BpeTokenizer`](bpetokenizer.md), [`MergePair`](mergepair.md),
[`BpeSplitStep`](bpesplitstep.md), [`BpePatterns`](bpepatterns.md).

## Members

| Member | What it does |
| --- | --- |
| [`BpeVocabulary.Equals`](bpevocabulary-equals.md) | Value equality over the vocabulary, the merges and every flag. |
| [`BpeVocabulary.GetHashCode`](bpevocabulary-gethashcode.md) | A hash consistent with it. |
