# TfidfVectorizer.LoadAsync

Read a fitted vectorizer back without blocking the caller.

<!-- docs-declaration -->

```csharp
public static Task<TfidfVectorizer> LoadAsync(Stream source, ArtifactLoadOptions options = null, CancellationToken cancellationToken = default)
```

**Parameters** — `source` is a readable stream, left open. `options` bounds what will be accepted.
`cancellationToken` cancels the read.

**Returns** — `Task<TfidfVectorizer>`, completing with a fitted vectorizer.

**Exceptions** — `ArgumentNullException` for a null source. `InvalidDataException` for content that
is not a saved vectorizer or that exceeds a bound. `OperationCanceledException` when cancelled.

**Example** — restoring, then weighting with the frequencies that were saved.

```csharp
using Lodestar.Text.Vectorization;

static async Task<int> RestoreAsync()
{
    var original = new TfidfVectorizer();
    original.Fit(["the cat eats", "the dog eats"]);

    using var buffer = new MemoryStream();
    await original.SaveAsync(buffer);
    buffer.Position = 0;

    TfidfVectorizer restored = await TfidfVectorizer.LoadAsync(buffer);
    return restored.Transform(["the cat"]).ColumnCount;
}

int columns = RestoreAsync().GetAwaiter().GetResult();  // => 4
```

The `GetAwaiter().GetResult()` is only what lets a synchronous example drive an async one; in
async code the call is simply `await`.

**Remarks** — the bounds in `options` are checked as the content is read rather than afterwards,
so an oversized file is refused before it is allocated.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TfidfVectorizer.Load`](tfidfvectorizer-load.md),
[`TfidfVectorizer.SaveAsync`](tfidfvectorizer-saveasync.md),
[`ArtifactLoadOptions`](../persistence/artifactloadoptions.md).
