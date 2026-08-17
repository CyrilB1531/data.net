# TfidfVectorizer.Fit

Learn the vocabulary and the document frequencies from a corpus.

<!-- docs-declaration -->

```csharp
public TfidfVectorizer Fit(IEnumerable<string> documents)
```

**Parameters** — `documents` is the corpus to learn from, enumerated once.

**Returns** — `TfidfVectorizer`, the same instance, so a call can be chained.

**Exceptions** — `ArgumentNullException` when `documents` is null. `ArgumentException` when the
corpus leaves no terms at all.

**Example** — fit on training documents, weight a later one with those frequencies.

```csharp
using Lodestar.Text.Vectorization;

var tv = new TfidfVectorizer();
tv.Fit(["the cat eats", "the dog eats"]);

CsrMatrix weighted = tv.Transform(["the cat sleeps"]);
int columns = weighted.ColumnCount;  // => 4
```

**Remarks** — two things are learned here where [`CountVectorizer.Fit`](countvectorizer-fit.md)
learns one: the vocabulary, and the document frequency of each term in it. Both come from this
corpus, which is what makes fitting on training data the correct order — a term's rarity is a
property of the corpus it was measured on, and measuring it on the test set leaks information
that will not exist at prediction time.

`sleeps` was never seen and is dropped, exactly as it would be by a count vectorizer.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TfidfVectorizer.Transform`](tfidfvectorizer-transform.md),
[`TfidfVectorizer.FitTransform`](tfidfvectorizer-fittransform.md).
