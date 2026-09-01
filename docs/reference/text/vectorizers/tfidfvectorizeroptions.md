# TfidfVectorizerOptions

The counting half and the weighting half, as one object.

<!-- docs-declaration -->

```csharp
public sealed record TfidfVectorizerOptions
```

**Properties** — `Count` is a [`CountVectorizerOptions`](countvectorizeroptions.md), deciding what
counts as a term. `Tfidf` is a [`TfidfOptions`](tfidfoptions.md), deciding how a count becomes a
weight. Both default to their own defaults, so `new TfidfVectorizerOptions()` is scikit-learn's
`TfidfVectorizer()`.

**Example** — stop words removed before the weighting, which is the usual reason to reach for this.

```csharp
using Lodestar.Abstractions;
using Lodestar.Text.Vectorization;

var options = new TfidfVectorizerOptions
{
    Count = new CountVectorizerOptions { StopWords = StopWords.English },
    Tfidf = new TfidfOptions { SublinearTf = true },
};

CsrMatrix weighted = new TfidfVectorizer(options).FitTransform(["the cat eats", "a dog eats"]);
```

**Remarks** — the split into two records is the one place this surface reads differently from
scikit-learn, where `TfidfVectorizer` takes every keyword of `CountVectorizer` and every keyword of
`TfidfTransformer` in one flat constructor. Flattening them here would have meant one record with
thirteen properties and no way to hand the counting half to a
[`CountVectorizer`](countvectorizer.md) or the weighting half to a
[`TfidfTransformer`](tfidftransformer.md); keeping them apart means the same options object can be
used with any of the three.

The order the two halves apply in is fixed and worth stating: `Count` decides what a term *is*,
then `Tfidf` decides what it is *worth*. So a stop word removed by `Count` never reaches the
weighting, and a document frequency computed by the weighting is over the terms that survived.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TfidfVectorizer`](tfidfvectorizer.md),
[`CountVectorizerOptions`](countvectorizeroptions.md), [`TfidfOptions`](tfidfoptions.md), the
[Python equivalence table](../../../equivalence.md).
