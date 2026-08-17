# CsrMatrix.ToDense

The same matrix with its zeros written out, for inspection.

<!-- docs-declaration -->

```csharp
public double[,] ToDense()
```

**Returns** — `double[,]` of `RowCount × ColumnCount`, every cell present, the stored values in
their places and zeros everywhere else.

**Example** — the cell that says `the` appears twice in the third document.

```csharp
using Lodestar.Text.Vectorization;

string[] docs = ["the cat eats", "the dog eats", "the cat and the dog"];
var cv = new CountVectorizer();
CsrMatrix counts = cv.FitTransform(docs);

// Features are sorted: and, cat, dog, eats, the — so "the" is column 4.
double[,] dense = counts.ToDense();
double theInThird = dense[2, 4];  // => 2
```

**Remarks** — **allocates `RowCount × ColumnCount` doubles**, which is exactly the array the sparse
layout exists to avoid. On the three-document corpus above that is fifteen cells; on a real corpus
it is the number that made sparsity necessary in the first place. Reach for it to look at a small
matrix, to hand a small one to something that wants a rectangular array, or in a test — not as a
step in a pipeline.

To read one cell without materialising the rest, walk
[`RowPointers`](csrmatrix.md) and `ColumnIndices` directly; to reduce the whole matrix against a
vector, [`Multiply`](csrmatrix-multiply.md) does it without densifying.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CsrMatrix`](csrmatrix.md), [`CsrMatrix.Multiply`](csrmatrix-multiply.md).
