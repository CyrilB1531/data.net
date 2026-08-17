# HashingVectorizer.Load

Read the options back.

<!-- docs-declaration -->

```csharp
public static HashingVectorizer Load(Stream source, ArtifactLoadOptions options = null)
public static HashingVectorizer Load(string path, ArtifactLoadOptions options = null)
```

**Parameters** — `source` is a readable stream, left open; `path` is a file to read. `options`
bounds what will be accepted.

**Returns** — `HashingVectorizer`, ready to
[`Transform`](hashingvectorizer-transform.md) immediately.

**Exceptions** — `ArgumentNullException` for a null source. `InvalidDataException` when the content
is not a saved vectorizer, or exceeds a bound in `options`.

**Example** — restored, and hashing identically to the original.

```csharp
using Lodestar.Text.Vectorization;

var original = new HashingVectorizer(new HashingVectorizerOptions { NumFeatures = 16 });

using var buffer = new MemoryStream();
original.Save(buffer);
buffer.Position = 0;

HashingVectorizer restored = HashingVectorizer.Load(buffer);
CsrMatrix before = original.Transform(["the cat eats"]);
CsrMatrix after = restored.Transform(["the cat eats"]);

bool identical = before.NonZeroCount == after.NonZeroCount
    && before.RowL2Norm(0) == after.RowL2Norm(0);  // => True
```

**Remarks** — a restored vectorizer produces the **same columns** as the original, because hashing
depends only on the term and the settings. That is the property that makes this type useful across
machines: shipping the options is enough, where the other two must ship a vocabulary.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`HashingVectorizer.Save`](hashingvectorizer-save.md),
[`ArtifactLoadOptions`](../persistence/artifactloadoptions.md).
