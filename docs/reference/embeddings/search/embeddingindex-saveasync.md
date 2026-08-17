# EmbeddingIndex.SaveAsync

Writes the index out, asynchronously.

<!-- docs-declaration -->

```csharp
public Task SaveAsync(Stream destination, CancellationToken cancellationToken = default)
```

**Parameters** — `destination` is a writable stream, never disposed by this method.
`cancellationToken` cancels the write.

**Returns** — `Task`, completing when the index has been written and flushed.

**Exceptions** — `InvalidDataException` when any stored vector holds a non-finite component.
`OperationCanceledException` when `cancellationToken` is signalled.

**Example** — the asynchronous half of a round trip.

```csharp
using Lodestar.Embeddings.Search;

static async Task<string> RoundTripAsync()
{
    var index = new EmbeddingIndex(dimension: 2);
    index.Add(new float[] { 1f, 0f }, "east");

    using var buffer = new MemoryStream();
    await index.SaveAsync(buffer);
    buffer.Position = 0;

    EmbeddingIndex restored = await EmbeddingIndex.LoadAsync(buffer);
    return restored.GetId(0)!;
}

string label = RoundTripAsync().GetAwaiter().GetResult();  // => east
```

The `GetAwaiter().GetResult()` is only what lets a synchronous example drive an async one; in
async code the call is simply `await`.

**Remarks** — the same format and the same refusals as [`Save`](embeddingindex-save.md); only the
I/O is asynchronous. There is no `string` overload, because opening the file is the synchronous
part and a caller who wants one can open it and hand over the stream.

Cancellation stops the write; it does not undo it. The stream belongs to the caller and is never
disposed here, so whatever had already been written stays written. Save to a temporary file and
move it into place if a half-written artifact could be mistaken for a good one.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`EmbeddingIndex.Save`](embeddingindex-save.md),
[`EmbeddingIndex.LoadAsync`](embeddingindex-loadasync.md), [`EmbeddingIndex`](embeddingindex.md).
