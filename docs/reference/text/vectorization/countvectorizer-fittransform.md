# CountVectorizer.FitTransform

Learn the vocabulary and count the same corpus, in one pass.

<!-- docs-declaration -->

```csharp
public CsrMatrix FitTransform(IEnumerable<string> documents)
```

**Parameters** — `documents` is the corpus, both learned from and counted.

**Returns** — [`CsrMatrix`](csrmatrix.md), one row per document and one column per learned term.

**Exceptions** — `ArgumentNullException` when `documents` is null. `ArgumentException` when the
corpus leaves no terms at all.

**Example** — the whole corpus at once.

```csharp
using Lodestar.Text.Vectorization;

string[] docs = ["the cat eats", "the dog eats", "the cat and the dog"];

CsrMatrix counts = new CountVectorizer().FitTransform(docs);

int rows = counts.RowCount;         // => 3
int stored = counts.NonZeroCount;   // => 10
```

**Remarks** — equivalent to [`Fit`](countvectorizer-fit.md) then
[`Transform`](countvectorizer-transform.md) **on the same corpus**, and not equivalent to fitting
one corpus and transforming another. It exists because that is the common case and because doing
it in one pass avoids enumerating the corpus twice — which matters when the corpus is a lazy
sequence read from disk.

The fit is kept, so the vectorizer can go on to transform further corpora afterwards.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CountVectorizer.Fit`](countvectorizer-fit.md),
[`CountVectorizer.Transform`](countvectorizer-transform.md), [`CsrMatrix`](csrmatrix.md).
