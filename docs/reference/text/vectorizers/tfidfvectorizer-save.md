# TfidfVectorizer.Save

Write a fitted vectorizer out, vocabulary and document frequencies together.

<!-- docs-declaration -->

```csharp
public void Save(Stream destination)
public void Save(string path)
```

**Parameters** — `destination` is a writable stream, left open for the caller to dispose; `path`
is a file to create or overwrite.

**Exceptions** — `InvalidOperationException` when nothing has been fitted yet.
`ArgumentNullException` for a null stream or path. `IOException` from the stream or file system.

**Example** — saving and restoring, with the weights intact.

```csharp
using Lodestar.Text.Vectorization;

var tv = new TfidfVectorizer();
tv.Fit(["the cat eats", "the dog eats"]);

using var buffer = new MemoryStream();
tv.Save(buffer);
buffer.Position = 0;

TfidfVectorizer restored = TfidfVectorizer.Load(buffer);
string first = restored.GetFeatureNames()[0];  // => cat
```

**Remarks** — what is written is both halves of the fit: the vocabulary **and** the document
frequencies. That second half is the reason saving matters more here than for
[`CountVectorizer`](countvectorizer.md) — frequencies are a property of the training corpus, and
a corpus that is gone cannot be measured again.

The stream overload leaves `destination` open, so a vectorizer can be one entry in a larger
archive.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TfidfVectorizer.Load`](tfidfvectorizer-load.md),
[`TfidfVectorizer.SaveAsync`](tfidfvectorizer-saveasync.md).
