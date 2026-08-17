# TfidfTransformer

Counts in, TF-IDF weights out — the equivalent of
`sklearn.feature_extraction.text.TfidfTransformer`.

Takes a [`CsrMatrix`](csrmatrix.md) of counts from anywhere and reweights it, so a term appearing
in every document counts for little and one appearing in a few counts for a lot.

<!-- docs-declaration -->

```csharp
public sealed class TfidfTransformer
```

**Constructor** — `TfidfTransformer(TfidfOptions? options = null)`, whose defaults are
scikit-learn's.

**Properties** — `Idf` is the inverse document frequency learned per column, available after
fitting.

**Example** — counts from a [`CountVectorizer`](countvectorizer.md), weighted afterwards.

```csharp
using Lodestar.Text.Vectorization;

string[] docs = ["the cat eats", "the dog eats", "the cat and the dog"];
CsrMatrix counts = new CountVectorizer().FitTransform(docs);

CsrMatrix weighted = new TfidfTransformer().FitTransform(counts);

double rowLength = weighted.RowL2Norm(0);  // => 1
```

**Remarks** — this exists separately from [`TfidfVectorizer`](tfidfvectorizer.md) because counts
do not have to come from text. A matrix built by hand, loaded from a file, or produced by another
library can be weighted here, and that is the case the combined vectorizer cannot serve.

Where the counts *do* come from text, [`TfidfVectorizer`](tfidfvectorizer.md) is this and a
[`CountVectorizer`](countvectorizer.md) in one pass, and is the shorter path.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TfidfOptions`](tfidfoptions.md), [`TfidfVectorizer`](tfidfvectorizer.md),
[`CsrMatrix`](csrmatrix.md), the [Python equivalence table](../../../equivalence.md).

## Members

| Member | What it does |
| --- | --- |
| [`TfidfTransformer.Fit`](tfidftransformer-fit.md) | Learn the document frequencies from a count matrix. |
| [`TfidfTransformer.FitTransform`](tfidftransformer-fittransform.md) | Learn them and weight the same matrix. |
| [`TfidfTransformer.Transform`](tfidftransformer-transform.md) | Weight a matrix using frequencies already learned. |
