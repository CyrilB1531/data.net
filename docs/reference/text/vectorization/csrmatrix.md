# CsrMatrix

The compressed-sparse-row matrix every vectorizer returns: one row per document, one column per
feature, and only the non-zero entries stored.

A corpus of ten thousand documents over fifty thousand terms has five hundred million cells and
perhaps a million non-zero ones. Storing the zeros is what this layout exists to avoid, and it is
the same layout `scipy.sparse.csr_matrix` uses, so a reader who knows one knows the other.

<!-- docs-declaration -->

```csharp
public sealed class CsrMatrix
```

**Properties** — `RowCount` and `ColumnCount` are the logical shape, zeros included.
`NonZeroCount` is how many cells are actually stored. `Values` holds those cells, `ColumnIndices`
the column each one sits in, and `RowPointers` where each row starts and ends: row `i` occupies
`Values[RowPointers[i]..RowPointers[i + 1]]`. `RowPointers` therefore has `RowCount + 1` entries,
and its last is `NonZeroCount`.

**Example** — three documents, five terms, and the three arrays that describe them.

```csharp
using Lodestar.Text.Vectorization;

string[] docs = ["the cat eats", "the dog eats", "the cat and the dog"];
CsrMatrix counts = new CountVectorizer().FitTransform(docs);

int rows = counts.RowCount;          // => 3
int columns = counts.ColumnCount;    // => 5
int stored = counts.NonZeroCount;    // => 10

// Row 2 runs from RowPointers[2] to RowPointers[3].
int start = counts.RowPointers[2];   // => 6
int end = counts.RowPointers[3];     // => 10
```

**Remarks** — fifteen cells, ten of them stored: the third document is the only one holding `and`,
and the first two hold neither `and` nor one of `cat`/`dog`.

The three arrays are exposed rather than hidden because reading them is often the point — feeding
another library, writing a file format, or checking what a vectorizer produced. They are the
matrix's **own** `double[]` and `int[]`, handed out without copying, so writing to one changes the
matrix. Treat them as read-only unless that is precisely what you mean.

Within a row, `ColumnIndices` is ascending. That is what makes a row comparable to another row in
one pass, and it is what `Multiply` relies on.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CountVectorizer`](countvectorizer.md), [`SparseNorm`](sparsenorm.md), the
[vectorization guide](../../../guides/vectorization.md), the
[Python equivalence table](../../../equivalence.md).

## Members

| Member | What it does |
| --- | --- |
| [`CsrMatrix.Multiply`](csrmatrix-multiply.md) | The matrix times a dense vector. |
| [`CsrMatrix.NormalizeRows`](csrmatrix-normalizerows.md) | Divide every row by its own norm, in place. |
| [`CsrMatrix.RowL1Norm`](csrmatrix-rowl1norm.md) | The sum of one row's absolute values. |
| [`CsrMatrix.RowL2Norm`](csrmatrix-rowl2norm.md) | The Euclidean length of one row. |
| [`CsrMatrix.ToDense`](csrmatrix-todense.md) | The same matrix with its zeros written out. |
