# HashingVectorizer.LoadAsync

Read the options back without blocking the caller.

<!-- docs-declaration -->

```csharp
public static Task<HashingVectorizer> LoadAsync(Stream source, ArtifactLoadOptions options = null, CancellationToken cancellationToken = default)
```

**Parameters** — `source` is a readable stream, left open. `options` bounds what will be accepted.
`cancellationToken` cancels the read.

**Returns** — `Task<HashingVectorizer>`, completing with a vectorizer ready to transform.

**Exceptions** — `ArgumentNullException` for a null source. `InvalidDataException` for content that
is not a saved vectorizer or that exceeds a bound. `OperationCanceledException` when cancelled.

**Example** — restored asynchronously, hashing as before.

```csharp
using Lodestar.Text.Vectorization;

static async Task<bool> RestoreAsync()
{
    var original = new HashingVectorizer(new HashingVectorizerOptions { NumFeatures = 16 });

    using var buffer = new MemoryStream();
    await original.SaveAsync(buffer);
    buffer.Position = 0;

    HashingVectorizer restored = await HashingVectorizer.LoadAsync(buffer);
    return restored.NumFeatures == original.NumFeatures;
}

bool identical = RestoreAsync().GetAwaiter().GetResult();  // => True
```

The `GetAwaiter().GetResult()` is only what lets a synchronous example drive an async one; in
async code the call is simply `await`.

**Remarks** — the bounds in `options` are checked as the content is read. They matter less here
than for the vectorizers that carry a vocabulary, since the content is a few settings, and they
are applied anyway so that one loading path behaves the same everywhere.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`HashingVectorizer.Load`](hashingvectorizer-load.md),
[`HashingVectorizer.SaveAsync`](hashingvectorizer-saveasync.md),
[`ArtifactLoadOptions`](../persistence/artifactloadoptions.md).
