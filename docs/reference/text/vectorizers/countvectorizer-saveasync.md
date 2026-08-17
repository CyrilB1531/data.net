# CountVectorizer.SaveAsync

Write a fitted vectorizer out without blocking the caller.

<!-- docs-declaration -->

```csharp
public Task SaveAsync(Stream destination, CancellationToken cancellationToken = default)
```

**Parameters** — `destination` is a writable stream, left open. `cancellationToken` cancels the
write.

**Returns** — `Task`, completing when the vectorizer has been written.

**Exceptions** — `InvalidOperationException` when nothing has been fitted yet.
`ArgumentNullException` for a null stream. `OperationCanceledException` when cancelled.

**Example** — the same round trip as [`Save`](countvectorizer-save.md), asynchronously.

```csharp
using Lodestar.Text.Vectorization;

static async Task<int> RoundTripAsync()
{
    var cv = new CountVectorizer();
    cv.Fit(["the cat eats", "the dog eats"]);

    using var buffer = new MemoryStream();
    await cv.SaveAsync(buffer);
    buffer.Position = 0;

    CountVectorizer restored = await CountVectorizer.LoadAsync(buffer);
    return restored.Transform(["the cat"]).ColumnCount;
}

int columns = RoundTripAsync().GetAwaiter().GetResult();  // => 4
```

The `GetAwaiter().GetResult()` is only what lets a synchronous example drive an async one; in
async code the call is simply `await`.

**Remarks** — there is no `SaveAsync(string path)` overload to match
[`Save`](countvectorizer-save.md)'s: opening the file is the caller's, and a `FileStream` created
with `useAsync: true` is what makes the asynchrony reach the disk rather than stopping at a
buffer.

Worth using when the destination is a network stream or a large file, and not otherwise — a
vocabulary is small, and the synchronous overload avoids the machinery.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CountVectorizer.Save`](countvectorizer-save.md),
[`CountVectorizer.LoadAsync`](countvectorizer-loadasync.md).
