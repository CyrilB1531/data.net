# CsrMatrix.TransposeMultiply

The transposed matrix times a dense block, without ever building the transpose.

<!-- docs-declaration -->

```csharp
public double[] TransposeMultiply(ReadOnlySpan<double> block, int columnCount)
```

**Parameters** — `block` is the dense right operand, row-major, with `RowCount` rows and
`columnCount` columns, so its length is the product of the two. `columnCount` is how many columns
it holds.

**Returns** — `double[]` of `ColumnCount × columnCount` entries, row-major: one row per column of
the matrix.

**Exceptions** — `ArgumentException` when `block` is not `RowCount` rows of `columnCount`.
`ArgumentOutOfRangeException` when `columnCount` is not positive, or when the product would not fit
in a single array.

**Example** — `[[1, 0, 2], [0, 3, 0]]` against a two-row, two-column block. The matrix has three
columns, so the answer has three rows; the middle one is the only one the second row of the matrix
reaches.

```csharp
using Lodestar.Abstractions;

CsrMatrix matrix = new(2, 3, [1.0, 2.0, 3.0], [0, 2, 1], [0, 2, 3]);

double[] product = matrix.TransposeMultiply([1.0, 0.5, 2.0, 1.5], columnCount: 2);

double firstRow = product[0];    // => 1
double middleRow = product[2];   // => 6
double lastRow = product[4];     // => 2
```

**Remarks** — materialising the transpose would cost a second matrix of the same size and a pass to
build it. Scattering into the result instead reads each non-zero exactly once, which is the same
arithmetic for none of the memory: for every stored cell, the row it sits in selects a row of
`block` and its column index selects the row of the result to add into.

Together with [`Multiply`](csrmatrix-multiply.md)'s block overload this is what a randomized SVD's
power iteration alternates between — `A Ω`, then `Aᵀ Q` — and the pair is the reason both exist
rather than a single vector product.

The result is **not** the transpose of `Multiply`'s. `Multiply` produces one row per row of the
matrix; this produces one per column.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CsrMatrix.Multiply`](csrmatrix-multiply.md), [`CsrMatrix`](csrmatrix.md), the
[Python equivalence table](../../../equivalence.md).
