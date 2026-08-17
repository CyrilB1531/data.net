# HashingVectorizer

Counts into a fixed number of columns, learning nothing — the equivalent of
`sklearn.feature_extraction.text.HashingVectorizer`.

Each term is hashed to a column. There is no vocabulary, so there is no `Fit`, no memory that
grows with the corpus, and **no `GetFeatureNames`**: nothing was kept that could name a column.

<!-- docs-declaration -->

```csharp
public sealed class HashingVectorizer
```

**Constructor** — `HashingVectorizer(HashingVectorizerOptions? options = null)`, whose defaults
are scikit-learn's.

**Properties** — `NumFeatures` is how many columns the matrix has.

**Example** — no fitting, and a width chosen rather than discovered.

```csharp
using Lodestar.Text.Vectorization;

var hv = new HashingVectorizer(new HashingVectorizerOptions { NumFeatures = 16 });
CsrMatrix hashed = hv.Transform(["the cat eats", "the dog eats", "the cat and the dog"]);

int columns = hashed.ColumnCount;  // => 16
double rowLength = hashed.RowL2Norm(0);  // => 1
```

**Remarks** — the trade is stateless-ness for names and for collisions. Choose this when the
corpus is a stream too large to pass over twice, when documents arrive one at a time and the
vocabulary would grow without bound, or when several machines must produce compatible vectors
without sharing a fitted model — hashing is deterministic, so they will.

Do not choose it when you will need to explain a vector. Column 9 means "whatever hashed to 9",
possibly two unrelated terms at once, and there is no way back.

`AlternateSign` is what keeps collisions from simply accumulating; see
[`HashingVectorizerOptions`](hashingvectorizeroptions.md).

**Applies to** — net10.0, netstandard2.0.

**See also** — [`HashingVectorizerOptions`](hashingvectorizeroptions.md),
[`CountVectorizer`](countvectorizer.md), [`CsrMatrix`](csrmatrix.md), the
[vectorization guide](../../../guides/vectorization.md).

## Members

| Member | What it does |
| --- | --- |
| [`HashingVectorizer.FitTransform`](hashingvectorizer-fittransform.md) | The same as `Transform`; there is nothing to fit. |
| [`HashingVectorizer.Load`](hashingvectorizer-load.md) | Read the options back. |
| [`HashingVectorizer.LoadAsync`](hashingvectorizer-loadasync.md) | The same, without blocking. |
| [`HashingVectorizer.Save`](hashingvectorizer-save.md) | Write the options out. |
| [`HashingVectorizer.SaveAsync`](hashingvectorizer-saveasync.md) | The same, without blocking. |
| [`HashingVectorizer.Transform`](hashingvectorizer-transform.md) | Hash a corpus into the fixed columns. |
