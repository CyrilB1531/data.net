# CountVectorizer.LoadAsync

Read a fitted vectorizer back without blocking the caller.

<!-- docs-declaration -->

```csharp
public static Task<CountVectorizer> LoadAsync(Stream source, ArtifactLoadOptions options = null, CancellationToken cancellationToken = default)
```

**Parameters** — `source` is a readable stream, left open. `options` bounds what will be accepted.
`cancellationToken` cancels the read.

**Returns** — `Task<CountVectorizer>`, completing with a fitted vectorizer.

**Exceptions** — `ArgumentNullException` for a null source. `InvalidDataException` for content that
is not a saved vectorizer or that exceeds a bound. `OperationCanceledException` when cancelled.

**Example** — the asynchronous half of the round trip.

```csharp
using Lodestar.Text.Vectorization;

static async Task<string> FirstFeatureAsync()
{
    var original = new CountVectorizer();
    original.Fit(["the cat eats", "the dog eats"]);

    using var buffer = new MemoryStream();
    await original.SaveAsync(buffer);
    buffer.Position = 0;

    CountVectorizer restored = await CountVectorizer.LoadAsync(buffer);
    return restored.GetFeatureNames()[0];
}

string first = FirstFeatureAsync().GetAwaiter().GetResult();  // => cat
```

The `GetAwaiter().GetResult()` is only what lets a synchronous example drive an async one; in
async code the call is simply `await`.

**Remarks** — there is no `LoadAsync(string path)` overload, for the same reason there is no
`SaveAsync(string)`: opening the file with `useAsync: true` is the caller's decision, and doing it
here would hide whether the asynchrony reaches the disk.

The bounds in `options` are checked **as the content is read**, not after, so an oversized file is
refused before it is allocated rather than afterwards.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CountVectorizer.Load`](countvectorizer-load.md),
[`CountVectorizer.SaveAsync`](countvectorizer-saveasync.md),
[`ArtifactLoadOptions`](../persistence/artifactloadoptions.md).
