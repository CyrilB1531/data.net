# The shared sparse primitive — `Lodestar.Abstractions`

Text vectorization produces a matrix that is almost entirely zeros: a thousand documents over a
twenty-thousand-word vocabulary is twenty million cells of which perhaps forty thousand are not
zero. [`CsrMatrix`](sparse/csrmatrix.md) stores the forty thousand.

It lives in a package of its own because more than one package needs it and they do not need each
other. `Lodestar.Text`'s vectorizers produce one; a decomposition consumes one; neither should
oblige a caller to take the other's distances, stemmers, tokenizers and JSON.
[Decision 0071](../../decisions/0071-csrmatrix-moves-to-an-abstractions-package.md) records that
move and what it cost.

The package is deliberately small and has no dependencies. It holds one class and one enum, no I/O,
and nothing to configure.

## Compressed sparse row, in one paragraph

Three arrays. `Values` holds the non-zero cells, row by row. `ColumnIndices` holds the column each
one sits in, ascending within a row. `RowPointers` has `RowCount + 1` entries and delimits the
rows: row `i` occupies the values from `RowPointers[i]` up to `RowPointers[i + 1]`. Reading a row
is a slice; reading a column is not, which is what makes the two products below the operations
worth having.

| Member | What it does |
| --- | --- |
| [`CsrMatrix`](sparse/csrmatrix.md) | The matrix: three arrays, and what the layout guarantees. |
| [`CsrMatrix.Multiply`](sparse/csrmatrix-multiply.md) | The matrix times a dense vector, or times a dense block. |
| [`CsrMatrix.TransposeMultiply`](sparse/csrmatrix-transposemultiply.md) | The transposed matrix times a dense block, without building the transpose. |
| [`CsrMatrix.NormalizeRows`](sparse/csrmatrix-normalizerows.md) | Divide every row by its own norm, in place. |
| [`CsrMatrix.RowL1Norm`](sparse/csrmatrix-rowl1norm.md) | The sum of one row's absolute values. |
| [`CsrMatrix.RowL2Norm`](sparse/csrmatrix-rowl2norm.md) | The Euclidean length of one row. |
| [`CsrMatrix.ToDense`](sparse/csrmatrix-todense.md) | The same matrix with its zeros written out. |
| [`SparseNorm`](sparse/sparsenorm.md) | Which norm `CsrMatrix.NormalizeRows` divides each row by. |
