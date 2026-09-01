# TfidfTransformer.FitTransform

Learn the document frequencies and weight the same matrix.

<!-- docs-declaration -->

```csharp

public CsrMatrix FitTransform(CsrMatrix counts)
```

**Parameters** — `counts` is the count matrix, both learned from and weighted.

**Returns** — [`CsrMatrix`](../../abstractions/sparse/csrmatrix.md), the same shape as `counts`, weighted and normalized by
[`TfidfOptions.Norm`](tfidfoptions.md).

**Exceptions** — `ArgumentNullException` when `counts` is null.

**Example** — the common case, where the counts and the corpus to weight are the same.

```csharp
using Lodestar.Abstractions;
using Lodestar.Text.Vectorization;

string[] docs = ["the cat eats", "the dog eats", "the cat and the dog"];
CsrMatrix counts = new CountVectorizer().FitTransform(docs);

CsrMatrix weighted = new TfidfTransformer().FitTransform(counts);

int rows = weighted.RowCount;      // => 3
int columns = weighted.ColumnCount; // => 5
```

**Remarks** — the shape is unchanged: weighting replaces values, it does not add or remove
columns. A count of zero stays absent, because a term the document does not hold has no weight
however rare it is.

The returned matrix is a **new** one; `counts` is left as it was, unlike
[`CsrMatrix.NormalizeRows`](../../abstractions/sparse/csrmatrix-normalizerows.md), which works in place.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TfidfTransformer.Fit`](tfidftransformer-fit.md),
[`TfidfVectorizer.FitTransform`](tfidfvectorizer-fittransform.md).
