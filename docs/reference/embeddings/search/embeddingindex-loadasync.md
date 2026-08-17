# EmbeddingIndex.LoadAsync

Reads an index back, asynchronously.

<!-- docs-declaration -->

```csharp
public static Task<EmbeddingIndex> LoadAsync(Stream source, ArtifactLoadOptions options = null, CancellationToken cancellationToken = default)
```

**Parameters** — `source` is a readable stream, never disposed by this method. `options` bounds
what will be accepted and defaults to
`Lodestar.Embeddings.Persistence.ArtifactLoadOptions`'s own defaults.
`cancellationToken` cancels the read.

**Returns** — `Task<EmbeddingIndex>`, completing with an index ready to
[`Search`](embeddingindex-search.md).

**Exceptions** — `InvalidDataException` for the same reasons as
[`Load`](embeddingindex-load.md). `OperationCanceledException` when `cancellationToken` is
signalled.

**Example** — the asynchronous round trip.

```csharp
using Lodestar.Embeddings.Search;

static async Task<int> ReloadAsync()
{
    var original = new EmbeddingIndex(dimension: 2);
    original.Add(new float[] { 1f, 0f });
    original.Add(new float[] { 0f, 1f });

    using var buffer = new MemoryStream();
    await original.SaveAsync(buffer);
    buffer.Position = 0;

    EmbeddingIndex reloaded = await EmbeddingIndex.LoadAsync(buffer);
    return reloaded.Count;
}

int count = ReloadAsync().GetAwaiter().GetResult();  // => 2
```

The `GetAwaiter().GetResult()` is only what lets a synchronous example drive an async one; in
async code the call is simply `await`.

**Remarks** — only the read is asynchronous; the bounds, the consistency checks and the
bit-exact restoration are identical to [`Load`](embeddingindex-load.md), and so is the fact that
the normalization flag comes from the file rather than from the caller.

There is no `string` overload here for the same reason
[`SaveAsync`](embeddingindex-saveasync.md) has none: opening the file is the synchronous part, and
a caller who wants it can open the stream and pass it.

**Bounds are applied while reading, not afterwards.** A file that would exceed `MaxTotalBytes` is
refused before its bytes are ever held, which is the whole point of bounding a load — checking
afterwards would mean having already allocated whatever the file asked for.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`EmbeddingIndex.Load`](embeddingindex-load.md),
[`EmbeddingIndex.SaveAsync`](embeddingindex-saveasync.md), [`EmbeddingIndex`](embeddingindex.md).
