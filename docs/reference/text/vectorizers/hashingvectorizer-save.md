# HashingVectorizer.Save

Write the options out, which is all there is to write.

<!-- docs-declaration -->

```csharp
public void Save(Stream destination)
public void Save(string path)
```

**Parameters** — `destination` is a writable stream, left open for the caller to dispose; `path`
is a file to create or overwrite.

**Exceptions** — `ArgumentNullException` for a null stream or path. `IOException` from the stream
or file system.

**Example** — a round trip that carries the settings and nothing else.

```csharp
using Lodestar.Text.Vectorization;

var hv = new HashingVectorizer(new HashingVectorizerOptions { NumFeatures = 16 });

using var buffer = new MemoryStream();
hv.Save(buffer);
buffer.Position = 0;

HashingVectorizer restored = HashingVectorizer.Load(buffer);
int columns = restored.NumFeatures;  // => 16
```

**Remarks** — there is no fit to save, so this writes the
[options](hashingvectorizeroptions.md) alone, and unlike its counterparts it **never throws for
being unfitted** — there is no such state.

Whether it is worth saving at all is a fair question, and the answer is that it keeps the three
vectorizers interchangeable: code that persists a fitted model works unchanged when the model is
this one. The file is small.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`HashingVectorizer.Load`](hashingvectorizer-load.md),
[`HashingVectorizer.SaveAsync`](hashingvectorizer-saveasync.md).
