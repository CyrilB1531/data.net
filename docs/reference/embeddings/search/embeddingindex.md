# EmbeddingIndex

An exhaustive cosine-similarity index: add vectors, query for the nearest, save and reload.

<!-- docs-declaration -->

```csharp
public sealed class EmbeddingIndex
```

**Example** — build, query, and read the hit back.

```csharp
using Lodestar.Embeddings.Search;

var index = new EmbeddingIndex(dimension: 2);
index.Add(new float[] { 1f, 0f }, "east");
index.Add(new float[] { 0f, 1f }, "north");

int size = index.Count;  // => 2
SearchResult best = index.Search(new float[] { 3f, 0f }, k: 1)[0];
string label = index.GetId(best.Index)!;  // => east
```

**Remarks** — the constructor is `EmbeddingIndex(int dimension, bool normalize = true)`.
`dimension` is the length every vector must have and must be at least `1`; `normalize` L2-
normalizes vectors on insertion and queries on search, which is what makes a dot product a cosine.
Leave it on unless the vectors are already unit length or you deliberately want a raw dot product.

Three properties describe an index without touching its contents:

| Property | What it is |
| --- | --- |
| `Count` | How many vectors have been added. |
| `Dimension` | The length every vector must have — what the constructor was given. |
| `HasIds` | Whether any vector in this index carries an id. |

Vectors are stored contiguously in one `float[]` that grows by doubling, so an index of *n*
vectors of dimension *d* is one allocation of about *n·d* floats rather than *n* small ones. That
layout is what the SIMD scan in [`VectorMath.Dot`](vectormath-dot.md) needs.

**Adding is not thread-safe; searching is.** Concurrent [`Search`](embeddingindex-search.md) calls
are fine on an index nobody is writing to — build it on one thread, then query it from many. There
is no internal lock, because paying for one on every query to protect a phase that is over would
be the wrong trade.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SearchResult`](searchresult.md), [`VectorMath`](vectormath.md),
[the search index](../search.md), [the embeddings guide](../../../guides/embeddings.md).

## Members

| Member | What it does |
| --- | --- |
| [`EmbeddingIndex.Add`](embeddingindex-add.md) | Adds one vector, optionally with an id. |
| [`EmbeddingIndex.GetId`](embeddingindex-getid.md) | The id stored at a position, or `null`. |
| [`EmbeddingIndex.Load`](embeddingindex-load.md) | Reads a saved index back. |
| [`EmbeddingIndex.LoadAsync`](embeddingindex-loadasync.md) | Reads a saved index back, asynchronously. |
| [`EmbeddingIndex.Save`](embeddingindex-save.md) | Writes the index out. |
| [`EmbeddingIndex.SaveAsync`](embeddingindex-saveasync.md) | Writes the index out, asynchronously. |
| [`EmbeddingIndex.Search`](embeddingindex-search.md) | The `k` most similar vectors to a query. |
