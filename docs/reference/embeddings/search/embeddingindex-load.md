# EmbeddingIndex.Load

Reads an index back, ready to search without embedding the corpus again.

<!-- docs-declaration -->

```csharp
public static EmbeddingIndex Load(Stream source, ArtifactLoadOptions options = null)
public static EmbeddingIndex Load(string path, ArtifactLoadOptions options = null)
```

**Parameters** — `source` is a readable stream, left open for the caller to dispose; `path` is the
file to read. `options` bounds what will be accepted and defaults to
[`ArtifactLoadOptions`](../persistence/artifactloadoptions.md)'s own defaults.

**Returns** — `EmbeddingIndex`, with the same `Dimension`, `Count`, ids and normalization setting
it was saved with.

**Exceptions** — `InvalidDataException` when the content is not an embedding index, is of an
unsupported version, is internally inconsistent, holds a non-finite value, or exceeds a bound in
`options`.

**Example** — a saved index reloaded and queried.

```csharp
using Lodestar.Embeddings.Search;

var original = new EmbeddingIndex(dimension: 2);
original.Add(new float[] { 1f, 0f }, "east");
original.Add(new float[] { 0f, 1f }, "north");

using var buffer = new MemoryStream();
original.Save(buffer);
buffer.Position = 0;

EmbeddingIndex reloaded = EmbeddingIndex.Load(buffer);
int size = reloaded.Count;  // => 2
string top = reloaded.GetId(reloaded.Search(new float[] { 1f, 0f }, k: 1)[0].Index)!;  // => east
```

**Remarks** — vectors are restored **exactly as stored** and never replayed through
[`Add`](embeddingindex-add.md). Re-normalizing an already normalized vector would move its bits,
and a reloaded index would then score slightly differently from the one that was saved.

**The normalization flag travels in the file and cannot be supplied here.** That is why neither
overload takes one. An index built with normalization on and reloaded with it off would rank a
corpus wrongly while looking entirely healthy, which is the class of bug a file format should make
impossible rather than document.

`options` is what stands between a file and an allocation. Counts are bounded before they size
anything, and the vector block is capped in **bytes** by `MaxTotalBytes` before parsing begins —
an element-count limit sized for a vocabulary is orders of magnitude away from what a corpus of
embeddings needs. A file that exceeds a bound is refused, never truncated: a quietly smaller index
is a wrong answer.

Internal consistency is checked too. A file whose `count` and `dimension` do not account for the
number of values in its vector block is refused, as is one whose id array is a different length
from its count.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`EmbeddingIndex.Save`](embeddingindex-save.md),
[`EmbeddingIndex.LoadAsync`](embeddingindex-loadasync.md), [`EmbeddingIndex`](embeddingindex.md).
