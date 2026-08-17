# TfidfVectorizer.Load

Read a fitted vectorizer back.

<!-- docs-declaration -->

```csharp
public static TfidfVectorizer Load(Stream source, ArtifactLoadOptions options = null)
public static TfidfVectorizer Load(string path, ArtifactLoadOptions options = null)
```

**Parameters** — `source` is a readable stream, left open; `path` is a file to read. `options`
bounds what will be accepted and defaults to
[`ArtifactLoadOptions`](../persistence/artifactloadoptions.md)'s own defaults.

**Returns** — `TfidfVectorizer`, fitted and ready to
[`Transform`](tfidfvectorizer-transform.md).

**Exceptions** — `ArgumentNullException` for a null source. `InvalidDataException` when the content
is not a saved vectorizer, or exceeds a bound in `options`.

**Example** — the vocabulary comes back in the order it was saved.

```csharp
using Lodestar.Text.Vectorization;

var original = new TfidfVectorizer();
original.Fit(["the cat eats", "the dog eats"]);

using var buffer = new MemoryStream();
original.Save(buffer);
buffer.Position = 0;

TfidfVectorizer restored = TfidfVectorizer.Load(buffer);
string first = restored.GetFeatureNames()[0];  // => cat
```

**Remarks** — `options` is what stands between a file and an allocation: a saved vectorizer
declaring a hundred million vocabulary entries would otherwise be believed. Bounds are refused
rather than truncated, so an oversized file is an error rather than a quietly smaller model.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TfidfVectorizer.Save`](tfidfvectorizer-save.md),
[`ArtifactLoadOptions`](../persistence/artifactloadoptions.md).
