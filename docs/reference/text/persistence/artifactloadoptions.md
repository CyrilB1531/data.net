# ArtifactLoadOptions

The bounds a load is held to, so a file cannot ask for more memory than you meant to give it.

<!-- docs-declaration -->

```csharp
public sealed record ArtifactLoadOptions
```

**Properties** — `MaxVocabularySize` (default `1_000_000`) is how many vocabulary entries will be
accepted. `MaxTokenLength` (default `1024`) is the longest single token, in characters.
`MaxJsonDepth` (default `32`) is how deeply the document may nest. `MaxTotalBytes` (default 256
MiB) is how much will be read from the source in total. `MaxArrayLength` (default `1_000_000`) is
the longest single JSON array.

**Example** — a stricter set than the defaults, for a file from somewhere untrusted.

```csharp
using Lodestar.Text.Persistence;
using Lodestar.Text.Vectorization;

var strict = new ArtifactLoadOptions
{
    MaxVocabularySize = 50_000,
    MaxTotalBytes = 8L * 1024 * 1024,
};

var cv = new CountVectorizer();
cv.Fit(["the cat eats", "the dog eats"]);

using var buffer = new MemoryStream();
cv.Save(buffer);
buffer.Position = 0;

CountVectorizer restored = CountVectorizer.Load(buffer, strict);
int columns = restored.Transform(["the cat"]).ColumnCount;  // => 4
```

**Remarks** — the bounds are checked **as the content is read**, not after, so an oversized file is
refused before it is allocated rather than afterwards. That ordering is the whole point: a check
that runs once the array exists has already lost.

Every default is generous enough that a real model never meets one — a million vocabulary entries
is far past any corpus this package is likely to see — so tightening them is a decision about the
*source*, not about the model. Tighten when the file came from a user, a network, or a build you
do not control; leave them when it came from your own training run.

Exceeding a bound raises `InvalidDataException`, and the artifact is refused rather than
truncated. A model that quietly loaded smaller than it was saved would score differently and give
no sign, which is worse than a failure.

This type is declared separately from `Lodestar.Embeddings`'s namesake rather than shared, so that
neither package depends on the other for its loading contract;
[`decisions/0011`](../../../decisions/0011-persistence-format.md) has the reasoning, along with
the comparison to `pickle.load` that motivates bounding at all.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CountVectorizer.Load`](../vectorizers/countvectorizer-load.md),
[`TfidfVectorizer.Load`](../vectorizers/tfidfvectorizer-load.md),
[`HashingVectorizer.Load`](../vectorizers/hashingvectorizer-load.md), the
[vectorization guide](../../../guides/vectorization.md).
