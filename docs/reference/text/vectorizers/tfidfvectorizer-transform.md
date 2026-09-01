# TfidfVectorizer.Transform

Weight a corpus against the vocabulary and frequencies already learned.

<!-- docs-declaration -->

```csharp

public CsrMatrix Transform(IEnumerable<string> documents)
```

**Parameters** — `documents` is the corpus to weight. Terms absent from the learned vocabulary are
dropped.

**Returns** — [`CsrMatrix`](../../abstractions/sparse/csrmatrix.md), as wide as the fit, weighted and normalized.

**Exceptions** — `InvalidOperationException` when nothing has been fitted yet.
`ArgumentNullException` when `documents` is null.

**Example** — the fit's width, whatever this corpus holds.

```csharp
using Lodestar.Abstractions;
using Lodestar.Text.Vectorization;

var tv = new TfidfVectorizer();
tv.Fit(["the cat eats", "the dog eats", "the cat and the dog"]);

CsrMatrix weighted = tv.Transform(["the cat", "nothing here matches"]);

int width = weighted.ColumnCount;  // => 5
double empty = weighted.RowL2Norm(1);  // => 0
```

**Remarks** — the second document shares no term with the vocabulary, so its row is empty and its
norm is `0` rather than `1`. Normalization leaves an all-zero row alone rather than dividing by
zero, so an unmatched document stays visible as a zero vector instead of becoming a `NaN` one —
worth checking for, because a zero vector has cosine similarity `0` with everything and will
quietly rank last rather than erroring.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TfidfVectorizer.Fit`](tfidfvectorizer-fit.md),
[`CsrMatrix.NormalizeRows`](../../abstractions/sparse/csrmatrix-normalizerows.md).
