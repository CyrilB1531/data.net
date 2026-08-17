# HashingVectorizer.SaveAsync

Write the options out without blocking the caller.

<!-- docs-declaration -->

```csharp
public Task SaveAsync(Stream destination, CancellationToken cancellationToken = default)
```

**Parameters** — `destination` is a writable stream, left open. `cancellationToken` cancels the
write.

**Returns** — `Task`, completing when the options have been written.

**Exceptions** — `ArgumentNullException` for a null stream. `OperationCanceledException` when
cancelled.

**Example** — the round trip, asynchronously.

```csharp
using Lodestar.Text.Vectorization;

static async Task<int> RoundTripAsync()
{
    var hv = new HashingVectorizer(new HashingVectorizerOptions { NumFeatures = 16 });

    using var buffer = new MemoryStream();
    await hv.SaveAsync(buffer);
    buffer.Position = 0;

    HashingVectorizer restored = await HashingVectorizer.LoadAsync(buffer);
    return restored.NumFeatures;
}

int columns = RoundTripAsync().GetAwaiter().GetResult();  // => 16
```

The `GetAwaiter().GetResult()` is only what lets a synchronous example drive an async one; in
async code the call is simply `await`.

**Remarks** — what is written is a handful of settings, so the asynchronous overload buys little
here beyond keeping this type interchangeable with the other two. As with them, there is no
`string path` overload: opening the file with `useAsync: true` belongs to the caller.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`HashingVectorizer.Save`](hashingvectorizer-save.md),
[`HashingVectorizer.LoadAsync`](hashingvectorizer-loadasync.md).
