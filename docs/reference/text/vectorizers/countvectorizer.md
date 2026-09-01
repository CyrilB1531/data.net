# CountVectorizer

Term counts over a vocabulary learned from the corpus — the equivalent of
`sklearn.feature_extraction.text.CountVectorizer`.

Fitting learns which terms exist and fixes their column order; transforming counts them. Column 7
means the same term in every row, and [`GetFeatureNames`](countvectorizer-getfeaturenames.md) can
say which.

<!-- docs-declaration -->

```csharp
public sealed class CountVectorizer
```

**Constructor** — `CountVectorizer(CountVectorizerOptions? options = null)`. The default options
are scikit-learn's defaults, so `new CountVectorizer()` is `CountVectorizer()`.

**Example** — three documents, and the vocabulary they produce.

```csharp
using Lodestar.Abstractions;
using Lodestar.Text.Vectorization;

string[] docs = ["the cat eats", "the dog eats", "the cat and the dog"];

var cv = new CountVectorizer();
CsrMatrix counts = cv.FitTransform(docs);

int features = counts.ColumnCount;  // => 5
```

**Remarks** — the vocabulary is **sorted**, as scikit-learn's is, so the column order depends only
on the terms and not on the order the documents arrived in. Two fits over the same corpus give the
same matrix, and a corpus shuffled before fitting gives the same matrix too.

What decides the counts is [`CountVectorizerOptions`](countvectorizeroptions.md) rather than
anything here — the token pattern that drops single-letter words, the stop words, the n-gram
range. This class only applies them.

To weight rare terms above common ones, [`TfidfVectorizer`](tfidfvectorizer.md) is this followed
by [`TfidfTransformer`](tfidftransformer.md). To skip the vocabulary entirely,
[`HashingVectorizer`](hashingvectorizer.md) trades naming for not having to hold one.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CountVectorizerOptions`](countvectorizeroptions.md), [`CsrMatrix`](../../abstractions/sparse/csrmatrix.md),
[`TfidfVectorizer`](tfidfvectorizer.md), the
[vectorization guide](../../../guides/vectorization.md).

## Members

| Member | What it does |
| --- | --- |
| [`CountVectorizer.Fit`](countvectorizer-fit.md) | Learn the vocabulary, and return the same instance. |
| [`CountVectorizer.FitTransform`](countvectorizer-fittransform.md) | Learn it and count the same corpus in one pass. |
| [`CountVectorizer.GetFeatureNames`](countvectorizer-getfeaturenames.md) | The term each column stands for, in column order. |
| [`CountVectorizer.Load`](countvectorizer-load.md) | Read a fitted vectorizer back. |
| [`CountVectorizer.LoadAsync`](countvectorizer-loadasync.md) | The same, without blocking. |
| [`CountVectorizer.Save`](countvectorizer-save.md) | Write a fitted vectorizer out. |
| [`CountVectorizer.SaveAsync`](countvectorizer-saveasync.md) | The same, without blocking. |
| [`CountVectorizer.Transform`](countvectorizer-transform.md) | Count a corpus against the vocabulary already learned. |
