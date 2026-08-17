# MergePair

One BPE merge rule: the two symbols it joins.

<!-- docs-declaration -->

```csharp
public readonly record struct MergePair
```

**Properties** — `Left` and `Right` are the two symbols. Applying the rule replaces an adjacent
`Left`, `Right` with their concatenation.

**Example** — the three rules that build `token` from characters.

```csharp
using Lodestar.Embeddings.Tokenization;

var merges = new List<MergePair> { new("t", "o"), new("k", "e"), new("ke", "n") };

string left = merges[2].Left;  // => ke
int count = merges.Count;  // => 3
```

**Remarks** — **order is the rule**. A BPE merge list is ranked: the pair listed first is applied
first, everywhere it occurs, before the next is considered. That is why `ke` + `n` can appear after
`k` + `e` — the second rule creates the symbol the third consumes — and why shuffling a
`merges.txt` produces a tokenizer that is not merely different but wrong.

The rank is the position in the list, so the list is the model. Two identical pairs at different
ranks are a real thing that occurs in shipped files, and which occurrence wins is a divergence
recorded in `docs/decisions/`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BpeVocabulary`](bpevocabulary.md), [`BpeTokenizer`](bpetokenizer.md).
