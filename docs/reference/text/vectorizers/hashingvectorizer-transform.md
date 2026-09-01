# HashingVectorizer.Transform

Hash a corpus into the fixed columns.

<!-- docs-declaration -->

```csharp

public CsrMatrix Transform(IEnumerable<string> documents)
```

**Parameters** — `documents` is the corpus to vectorize. Nothing is learned from it.

**Returns** — [`CsrMatrix`](../../abstractions/sparse/csrmatrix.md), one row per document and exactly `NumFeatures` columns,
normalized by [`HashingVectorizerOptions.Norm`](hashingvectorizeroptions.md).

**Exceptions** — `ArgumentNullException` when `documents` is null.

**Example** — the width is the option, not the corpus.

```csharp
using Lodestar.Abstractions;
using Lodestar.Text.Vectorization;

var hv = new HashingVectorizer(new HashingVectorizerOptions { NumFeatures = 16 });

CsrMatrix first = hv.Transform(["the cat eats"]);
CsrMatrix second = hv.Transform(["an entirely different corpus about boats"]);

int width = first.ColumnCount;      // => 16
int alsoWidth = second.ColumnCount; // => 16
```

**Remarks** — no state carries between calls, so two corpora vectorized separately are directly
comparable — the same term lands in the same column both times, and on another machine too. That
is the property the count vectorizers cannot offer without saving and shipping a fitted model.

A row can hold **negative** values when `AlternateSign` is on, which is the default. That is not a
defect: it is what makes two colliding terms tend to cancel rather than sum, and it means a
`CsrMatrix` from here is the one place in this namespace where
[`RowL1Norm`](../../abstractions/sparse/csrmatrix-rowl1norm.md)'s absolute values matter.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`HashingVectorizer.FitTransform`](hashingvectorizer-fittransform.md),
[`HashingVectorizerOptions`](hashingvectorizeroptions.md).
