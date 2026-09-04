# Mmr

Maximal Marginal Relevance: greedy selection that trades relevance to a query against redundancy
with what is already selected.

<!-- docs-declaration -->

```csharp
public static class Mmr
```

**Example** — the same four candidates picked two ways: pure relevance keeps query order, pure
diversity reaches for the one orthogonal candidate second.

```csharp
using Lodestar.Embeddings.Search;

float[] query = [1f, 0f, 0f];
float[][] candidates =
[
    [1.00f, 0.00f, 0.00f],
    [0.80f, 0.60f, 0.00f],
    [0.60f, 0.00f, 0.80f],
    [0.00f, 1.00f, 0.00f],
];

int[] relevanceOnly = Mmr.Select(query, candidates, count: 3, lambda: 1.0);
int[] diverse = Mmr.Select(query, candidates, count: 3, lambda: 0.0);

string relevanceOrder = string.Join(",", relevanceOnly);  // => 0,1,2
string diverseOrder = string.Join(",", diverse);           // => 0,3,2
```

**Remarks** — knows nothing about text. The candidates are vectors and the result is their
indices, so the same call serves keyword selection (see the
[keyword extraction guide](../../../guides/keyword-extraction.md)'s KeyBERT-style composition), passage
reranking, or any other list a caller wants spread out rather than clustered.

`lambda` trades the two off: `1` is pure relevance to the query, `0` is pure diversity from what is
already picked, and every value between blends the two scores. The **first** pick is always the
most relevant candidate regardless of `lambda`, because nothing is selected yet to be redundant
with.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Mmr.Select`](mmr-select.md), [`VectorMath`](vectormath.md),
[the search index](../search.md), [the keyword extraction guide](../../../guides/keyword-extraction.md).

## Members

| Member | What it does |
| --- | --- |
| [`Mmr.Select`](mmr-select.md) | Selects up to `count` candidates. |
