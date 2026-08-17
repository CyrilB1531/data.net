# SearchResult

One search hit: where the vector sits in the index, and how well it scored.

<!-- docs-declaration -->

```csharp
public readonly record struct SearchResult(int Index, float Score)
```

**Example** — reading a hit apart.

```csharp
using Lodestar.Embeddings.Search;

var index = new EmbeddingIndex(dimension: 2);
index.Add(new float[] { 1f, 0f }, "first");
index.Add(new float[] { 0f, 1f }, "second");

SearchResult hit = index.Search(new float[] { 1f, 0f }, k: 1)[0];
int where = hit.Index;  // => 0
string id = index.GetId(hit.Index)!;  // => first
```

**Remarks** — two fields and nothing else.

`Index` is a position in `[0, Count)`, in insertion order — the *n*-th vector added is index
*n − 1*. It is what [`EmbeddingIndex.GetId`](embeddingindex-getid.md) takes, and what you carry
back to whatever holds your documents.

`Score` is the similarity, which for a normalizing index is a cosine in `[-1, 1]`: `1` is the same
direction, `0` perpendicular. For an index built with `normalize: false` it is a raw dot product
and has no bounded range.

**The document is deliberately not in here.** This struct is 8 bytes with no references, so
[`Search`](embeddingindex-search.md) can score into a `SearchResult[Count]` and sort it without
the garbage collector ever scanning that array or the sort moving references through it. Putting
a `string` id in it would cost that on every query, to save one lookup on the handful of results
actually returned.

Being a `record struct`, it compares by value and deconstructs — `var (position, score) = hit;`
works.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`EmbeddingIndex.Search`](embeddingindex-search.md),
[`EmbeddingIndex.GetId`](embeddingindex-getid.md), [the search index](../search.md).
