# EmbeddingIndex.Search

The `k` most similar vectors to a query, best first.

<!-- docs-declaration -->

```csharp
public IReadOnlyList<SearchResult> Search(ReadOnlySpan<float> query, int k)
```

**Parameters** — `query` is the query embedding and must have length `Dimension`. `k` is how many
hits to return and must be at least `1`.

**Returns** — `IReadOnlyList<SearchResult>`, sorted by score descending. It holds
`min(k, Count)` entries: asking for more than the index contains is **not** an error, it simply
returns everything.

**Exceptions** — `ArgumentException` when `query.Length` differs from `Dimension`.
`ArgumentOutOfRangeException` when `k` is less than `1`.

**Example** — the query is scaled and the score is still a cosine, and `k` past the end is
harmless.

```csharp
using Lodestar.Embeddings.Search;

var index = new EmbeddingIndex(dimension: 2);
index.Add(new float[] { 1f, 0f });
index.Add(new float[] { 0f, 1f });

IReadOnlyList<SearchResult> hits = index.Search(new float[] { 5f, 0f }, k: 10);
int returned = hits.Count;  // => 2
float top = hits[0].Score;  // => 1
float second = hits[1].Score;  // => 0
```

**Remarks** — the query is normalized on a normalizing index, so its length never affects the
ranking. `(5, 0)` and `(1, 0)` are the same query.

**Ties break on position, ascending.** Two vectors with an identical score come back in insertion
order, which makes the result reproducible across runs rather than dependent on the sort. That
matters more than it sounds: duplicate documents in a corpus produce exact ties routinely.

Every stored vector is scored on every call — that is what exhaustive means, and it is why the
result is exact with no recall parameter to tune. The cost is linear in `Count × Dimension`, with
a small constant from [`VectorMath.Dot`](vectormath-dot.md).

Concurrent calls are safe on an index nobody is adding to. Adding while searching is not.

The hits carry positions, not documents — [`GetId`](embeddingindex-getid.md) is the step from one
to the other, and [`SearchResult`](searchresult.md) explains why it is a separate step.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`EmbeddingIndex.Add`](embeddingindex-add.md), [`SearchResult`](searchresult.md),
[`EmbeddingIndex`](embeddingindex.md).
