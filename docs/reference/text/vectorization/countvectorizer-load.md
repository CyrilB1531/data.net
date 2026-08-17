# CountVectorizer.Load

Read a fitted vectorizer back.

<!-- docs-declaration -->

```csharp
public static CountVectorizer Load(Stream source, ArtifactLoadOptions options = null)
public static CountVectorizer Load(string path, ArtifactLoadOptions options = null)
```

**Parameters** — `source` is a readable stream, left open; `path` is a file to read.
`options` bounds what will be accepted — sizes, counts and depths — and defaults to
[`ArtifactLoadOptions`](../persistence/artifactloadoptions.md)'s own defaults.

**Returns** — `CountVectorizer`, fitted and ready to
[`Transform`](countvectorizer-transform.md).

**Exceptions** — `ArgumentNullException` for a null source. `InvalidDataException` when the
content is not a saved vectorizer, or when it exceeds a bound in `options`.

**Example** — restoring, and counting with the vocabulary that was saved.

```csharp
using Lodestar.Text.Vectorization;

var original = new CountVectorizer();
original.Fit(["the cat eats", "the dog eats"]);

using var buffer = new MemoryStream();
original.Save(buffer);
buffer.Position = 0;

CountVectorizer restored = CountVectorizer.Load(buffer);
IReadOnlyList<string> names = restored.GetFeatureNames();

string first = names[0];  // => cat
```

**Remarks** — `options` is the reason this is not a one-line deserialization. A saved vectorizer is
a file, a file can come from anywhere, and a vocabulary declaring a hundred million entries would
otherwise be allocated before anything noticed. The bounds are refused rather than truncated, so a
file that exceeds one is an error rather than a quietly smaller model.

The vocabulary comes back in the order it was saved, so a matrix produced after loading has the
same column meanings as one produced before.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CountVectorizer.Save`](countvectorizer-save.md),
[`CountVectorizer.LoadAsync`](countvectorizer-loadasync.md),
[`ArtifactLoadOptions`](../persistence/artifactloadoptions.md).
