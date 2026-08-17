# TfidfVectorizer.FitTransform

Learn the vocabulary and frequencies, and weight the same corpus.

<!-- docs-declaration -->

```csharp
public CsrMatrix FitTransform(IEnumerable<string> documents)
```

**Parameters** — `documents` is the corpus, both learned from and weighted.

**Returns** — [`CsrMatrix`](csrmatrix.md), one row per document, weighted and normalized by
[`TfidfOptions.Norm`](tfidfoptions.md).

**Exceptions** — `ArgumentNullException` when `documents` is null. A corpus that leaves no terms does **not**
throw: it yields a model of zero columns, which every later transform will produce empty
rows against.

**Example** — the whole corpus in one call, which is the usual way in.

```csharp
using Lodestar.Text.Vectorization;

string[] docs = ["the cat eats", "the dog eats", "the cat and the dog"];

CsrMatrix weighted = new TfidfVectorizer().FitTransform(docs);

int rows = weighted.RowCount;         // => 3
int columns = weighted.ColumnCount;   // => 5
```

**Remarks** — equivalent to [`Fit`](tfidfvectorizer-fit.md) then
[`Transform`](tfidfvectorizer-transform.md) on the same corpus, in one enumeration rather than
two. It is not equivalent to fitting one corpus and transforming another, and the difference is
not cosmetic here: the document frequencies would come from the wrong corpus.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TfidfVectorizer.Fit`](tfidfvectorizer-fit.md),
[`TfidfTransformer.FitTransform`](tfidftransformer-fittransform.md), [`CsrMatrix`](csrmatrix.md).
