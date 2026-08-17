# EmbeddingIndex.Save

Writes the index — configuration, ids and the vector block — out as UTF-8 JSON.

<!-- docs-declaration -->

```csharp
public void Save(Stream destination)
public void Save(string path)
```

**Parameters** — `destination` is a writable stream, flushed but never disposed: the caller owns
it. `path` is a file to create or overwrite, written UTF-8 without a byte-order mark.

**Returns** — nothing.

**Exceptions** — `InvalidDataException` when any stored vector holds a non-finite component; the
message names the item and the component. `IOException` from the stream or file system.

**Example** — a round trip through memory, ids and all.

```csharp
using Lodestar.Embeddings.Search;

var index = new EmbeddingIndex(dimension: 2);
index.Add(new float[] { 1f, 0f }, "east");

using var buffer = new MemoryStream();
index.Save(buffer);
buffer.Position = 0;

EmbeddingIndex restored = EmbeddingIndex.Load(buffer);
string label = restored.GetId(0)!;  // => east
```

**Remarks** — vectors are written as **base64 raw little-endian bits**, not as decimal text, so a
reload scores bit for bit what the original scored. A decimal round trip that lost the last bit of
a float would change scores by an amount too small to notice and too large to be nothing.

**A non-finite component is refused here though [`Add`](embeddingindex-add.md) accepted it.** A
`NaN` in memory is a wrong score you can still find; in a file it becomes a wrong score that
outlives the code that made it, scoring `NaN` against every query the reloaded index is ever
given.

The `path` overload **checks for that before it opens the file**. Truncating a good artifact and
then refusing to write is the failure mode that would cost you the index you already had, so the
check comes first and a refused save leaves the existing file untouched.

The stream overload leaves `destination` open, so an index can be one entry inside a larger
archive.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`EmbeddingIndex.Load`](embeddingindex-load.md),
[`EmbeddingIndex.SaveAsync`](embeddingindex-saveasync.md), [`EmbeddingIndex`](embeddingindex.md).
