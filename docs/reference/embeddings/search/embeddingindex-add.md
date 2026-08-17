# EmbeddingIndex.Add

Adds one vector to the index, optionally with an id to recall it by.

<!-- docs-declaration -->

```csharp
public void Add(ReadOnlySpan<float> vector)
public void Add(ReadOnlySpan<float> vector, string id)
```

**Parameters** — `vector` is the embedding, and its length must equal `Dimension`. `id` is
anything identifying the document — a primary key, a URL, a path — kept verbatim and never
interpreted; `null` is exactly equivalent to the single-argument overload.

**Returns** — nothing. The vector's position is the old `Count`, and the new `Count` is one
higher.

**Exceptions** — `ArgumentException` when `vector.Length` differs from `Dimension`. The message
names both lengths.

**Example** — two vectors, one with an id and one without.

```csharp
using Lodestar.Embeddings.Search;

var index = new EmbeddingIndex(dimension: 2);
index.Add(new float[] { 1f, 0f }, "east");
index.Add(new float[] { 0f, 1f });

int count = index.Count;  // => 2
bool anyIds = index.HasIds;  // => True
```

**Remarks** — a **copy** is stored, normalized on the way in when the index normalizes, so the
caller's array is neither retained nor modified. Reusing one buffer for every vector is therefore
safe and is the cheap way to load a corpus.

The id array is allocated on the first non-null id and never before, so an index of anonymous
vectors pays nothing for a feature it does not use. That is also why there are two overloads
rather than one optional parameter: adding a parameter to the existing method would change its
signature and break callers already compiled against it.

**A vector holding `NaN` or an infinity is accepted here and refused by
[`Save`](embeddingindex-save.md).** That asymmetry is deliberate. In memory a bad vector is a bad
score you can notice and fix; written to a file it becomes a permanent `NaN` that scores against
every future query, outliving the code that produced it. If you intend to persist an index, check
the vectors as you build it rather than at save time, when the corpus that produced them may be
gone.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`EmbeddingIndex.Search`](embeddingindex-search.md),
[`EmbeddingIndex.GetId`](embeddingindex-getid.md), [`EmbeddingIndex`](embeddingindex.md).
