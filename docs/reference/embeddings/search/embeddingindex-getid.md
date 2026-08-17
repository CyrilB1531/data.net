# EmbeddingIndex.GetId

The id stored at a position, or `null` when that item has none.

<!-- docs-declaration -->

```csharp
public string GetId(int index)
```

**Parameters** — `index` is a position in `[0, Count)` — in practice a
[`SearchResult.Index`](searchresult.md) handed back by [`Search`](embeddingindex-search.md).

**Returns** — `string`, the id given to [`Add`](embeddingindex-add.md), or `null` if that vector
was added without one.

**Exceptions** — `ArgumentOutOfRangeException` when `index` is negative or not less than `Count`.
The message names the valid range.

**Example** — one vector with an id, one without.

```csharp
using Lodestar.Embeddings.Search;

var index = new EmbeddingIndex(dimension: 2);
index.Add(new float[] { 1f, 0f }, "east");
index.Add(new float[] { 0f, 1f });

string named = index.GetId(0)!;  // => east
bool anonymous = index.GetId(1) is null;  // => True
```

**Remarks** — this is the step from a search hit to your own data, and it is a lookup rather than
a field on the result for a reason [`SearchResult`](searchresult.md) sets out: keeping references
out of the scored array is what makes scoring and sorting cheap.

`null` means two different things and the type cannot tell them apart: the vector was added
without an id, or it was added with an explicit `null`, which
[`Add`](embeddingindex-add.md) treats as the same thing. `HasIds` answers the coarser question of
whether the index carries ids at all.

Ids survive a [`Save`](embeddingindex-save.md) and [`Load`](embeddingindex-load.md) round trip,
which is what makes them worth storing: a reloaded index can name its documents without the
original corpus being present.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`EmbeddingIndex.Add`](embeddingindex-add.md),
[`EmbeddingIndex.Search`](embeddingindex-search.md), [`EmbeddingIndex`](embeddingindex.md).
