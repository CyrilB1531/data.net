# TfidfTransformer.Fit

Learn the document frequencies from a count matrix.

<!-- docs-declaration -->

```csharp

public TfidfTransformer Fit(CsrMatrix counts)
```

**Parameters** — `counts` is the count matrix to learn from: one row per document, one column per
term.

**Returns** — `TfidfTransformer`, the same instance, so a call can be chained.

**Exceptions** — `ArgumentNullException` when `counts` is null.

**Example** — fit on the training counts, weight a later matrix with them.

```csharp
using Lodestar.Abstractions;
using Lodestar.Text.Vectorization;

var cv = new CountVectorizer();
CsrMatrix training = cv.FitTransform(["the cat eats", "the dog eats"]);

var tfidf = new TfidfTransformer();
tfidf.Fit(training);

CsrMatrix later = tfidf.Transform(cv.Transform(["the cat"]));
int columns = later.ColumnCount;  // => 4
```

**Remarks** — what is learned is one IDF value per **column**, so a matrix transformed afterwards
must have the same number of columns and the same column meanings. That is why the count
vectorizer above is reused rather than refitted: a new one would sort a different vocabulary into
the same column indices and the weights would be applied to the wrong terms, silently.

After fitting, `Idf` holds those values in column order.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TfidfTransformer.Transform`](tfidftransformer-transform.md),
[`TfidfTransformer.FitTransform`](tfidftransformer-fittransform.md).
