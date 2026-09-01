# TfidfVectorizer

[`CountVectorizer`](countvectorizer.md) and [`TfidfTransformer`](tfidftransformer.md) in one pass
— the equivalent of `sklearn.feature_extraction.text.TfidfVectorizer`.

Counts the terms, then weights each count by how rare the term is across the corpus, so that
words appearing everywhere stop dominating the vectors.

<!-- docs-declaration -->

```csharp
public sealed class TfidfVectorizer
```

**Constructor** — `TfidfVectorizer(TfidfVectorizerOptions? options = null)`, whose two halves
default to scikit-learn's defaults.

**Properties** — `Idf` is the inverse document frequency per column, available after fitting.

**Example** — the same corpus as [`CountVectorizer`](countvectorizer.md), weighted.

```csharp
using Lodestar.Abstractions;
using Lodestar.Text.Vectorization;

string[] docs = ["the cat eats", "the dog eats", "the cat and the dog"];

CsrMatrix weighted = new TfidfVectorizer().FitTransform(docs);

// Rows come out L2-normalized, so every row's length is 1.
double rowLength = weighted.RowL2Norm(0);  // => 1
```

**Remarks** — this is the vectorizer to reach for by default. Raw counts make long documents look
important and common words look meaningful; TF-IDF fixes both, and the L2 normalization that
follows is what makes two documents of different lengths comparable.

It is exactly the two other types composed, and the composition is the only difference. Where
counts already exist, [`TfidfTransformer`](tfidftransformer.md) weights them without re-reading
text. Where the vocabulary must not be held in memory,
[`HashingVectorizer`](hashingvectorizer.md) gives that up instead.

`the` appears in all three documents above and still has a non-zero weight, which surprises
readers: with `SmoothIdf` on, the IDF of a ubiquitous term is `log(1) + 1`, not `0`. See
[`TfidfOptions`](tfidfoptions.md).

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TfidfVectorizerOptions`](tfidfvectorizeroptions.md),
[`TfidfTransformer`](tfidftransformer.md), [`CountVectorizer`](countvectorizer.md), the
[vectorization guide](../../../guides/vectorization.md).

## Members

| Member | What it does |
| --- | --- |
| [`TfidfVectorizer.Fit`](tfidfvectorizer-fit.md) | Learn the vocabulary and the document frequencies. |
| [`TfidfVectorizer.FitTransform`](tfidfvectorizer-fittransform.md) | Learn them and weight the same corpus. |
| [`TfidfVectorizer.GetFeatureNames`](tfidfvectorizer-getfeaturenames.md) | The term each column stands for. |
| [`TfidfVectorizer.Load`](tfidfvectorizer-load.md) | Read a fitted vectorizer back. |
| [`TfidfVectorizer.LoadAsync`](tfidfvectorizer-loadasync.md) | The same, without blocking. |
| [`TfidfVectorizer.Save`](tfidfvectorizer-save.md) | Write a fitted vectorizer out. |
| [`TfidfVectorizer.SaveAsync`](tfidfvectorizer-saveasync.md) | The same, without blocking. |
| [`TfidfVectorizer.Transform`](tfidfvectorizer-transform.md) | Weight a corpus against what was learned. |
