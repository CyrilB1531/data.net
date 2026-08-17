# BpeVocabulary.GetHashCode

A hash consistent with that equality.

<!-- docs-declaration -->

```csharp
public int GetHashCode()
```

**Returns** — `int`, over the flags and the two collections' sizes rather than their contents.

**Example** — equal models hash alike.

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
bool hashesAlike = model.GetHashCode() == same.GetHashCode();  // => True
```

**Remarks** — a real BPE model holds fifty thousand entries and as many merges, and hashing all of
them on every call would make the type unusable as a key. The size and the flags are enough to
separate the models anyone actually compares, and two that collide are separated by
[`Equals`](bpevocabulary-equals.md), which reads everything.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BpeVocabulary.Equals`](bpevocabulary-equals.md).
