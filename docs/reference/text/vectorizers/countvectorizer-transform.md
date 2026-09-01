# CountVectorizer.Transform

Count a corpus against the vocabulary already learned.

<!-- docs-declaration -->

```csharp

public CsrMatrix Transform(IEnumerable<string> documents)
```

**Parameters** — `documents` is the corpus to count. Its terms are looked up in the vocabulary
learned by [`Fit`](countvectorizer-fit.md); terms absent from it are dropped.

**Returns** — [`CsrMatrix`](../../abstractions/sparse/csrmatrix.md), one row per document and one column per **learned**
term, so its width is the fit's width whatever this corpus holds.

**Exceptions** — `InvalidOperationException` when nothing has been fitted yet.
`ArgumentNullException` when `documents` is null.

**Example** — a document holding an unseen term, and one holding none of the vocabulary.

```csharp
using Lodestar.Abstractions;
using Lodestar.Text.Vectorization;

var cv = new CountVectorizer();
cv.Fit(["the cat eats", "the dog eats"]);

CsrMatrix counts = cv.Transform(["the cat sleeps", "nothing here matches"]);

int width = counts.ColumnCount;   // => 4
int stored = counts.NonZeroCount; // => 2
```

**Remarks** — two stored cells: `the` and `cat` from the first document, nothing at all from the
second. A document that shares no term with the vocabulary produces an **empty row** rather than an
error — it is a legitimate answer, and one worth checking for downstream, because an empty row has
no norm and normalizing leaves it alone.

Transforming before fitting throws rather than fitting implicitly, because the alternative is a
vocabulary learned from whatever corpus happened to arrive first.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CountVectorizer.Fit`](countvectorizer-fit.md),
[`CountVectorizer.FitTransform`](countvectorizer-fittransform.md).
