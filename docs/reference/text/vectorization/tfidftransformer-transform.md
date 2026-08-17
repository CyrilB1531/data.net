# TfidfTransformer.Transform

Weight a count matrix using document frequencies already learned.

<!-- docs-declaration -->

```csharp
public CsrMatrix Transform(CsrMatrix counts)
```

**Parameters** — `counts` is the matrix to weight. It must have the same number of columns as the
matrix that was fitted.

**Returns** — [`CsrMatrix`](csrmatrix.md), weighted and normalized, the same shape as `counts`.

**Exceptions** — `InvalidOperationException` when nothing has been fitted yet.
`ArgumentNullException` when `counts` is null. `ArgumentException` when the column count disagrees
with the fit.

**Example** — training frequencies applied to a later document.

```csharp
using Lodestar.Text.Vectorization;

var cv = new CountVectorizer();
CsrMatrix training = cv.FitTransform(["the cat eats", "the dog eats"]);

var tfidf = new TfidfTransformer();
tfidf.Fit(training);

CsrMatrix weighted = tfidf.Transform(cv.Transform(["the cat"]));
int width = weighted.ColumnCount;  // => 4
```

The row length is `1` only up to floating point — weighting then normalizing lands a
hair under it, which is why the assertion above is on the shape rather than the norm.

**Remarks** — the column count is checked and a mismatch throws, which is the one shape error this
type can catch. What it cannot catch is a matrix of the right width whose columns mean something
else — two vectorizers fitted on different corpora can easily agree in width and disagree in every
column. Reusing the vectorizer that produced the training counts is what avoids that, and there is
no way for this method to verify it.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TfidfTransformer.Fit`](tfidftransformer-fit.md),
[`TfidfTransformer.FitTransform`](tfidftransformer-fittransform.md).
