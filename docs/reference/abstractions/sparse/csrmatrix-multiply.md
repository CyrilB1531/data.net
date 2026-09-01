# CsrMatrix.Multiply

The matrix times a dense vector, or times a dense block, in one pass over the stored cells.

<!-- docs-declaration -->

```csharp
public double[] Multiply(ReadOnlySpan<double> vector)
public double[] Multiply(ReadOnlySpan<double> block, int columnCount)
```

**Parameters** — `vector` is the dense vector to multiply by, and must hold exactly `ColumnCount`
values. The second overload takes `block`, a row-major dense operand of `ColumnCount` rows and
`columnCount` columns, so its length is the product of the two.

**Returns** — `double[]`. From the first overload, `RowCount` entries, each the dot product of that
row with `vector`. From the second, `RowCount × columnCount` entries, row-major.

**Exceptions** — `ArgumentException` when `vector` is not `ColumnCount` long, or when `block` is
not `ColumnCount` rows of `columnCount`. `ArgumentOutOfRangeException` when `columnCount` is not
positive, or when the product would not fit in a single array.

**Example** — multiplying by all ones totals each row, which is that document's term count.

```csharp
using Lodestar.Abstractions;
using Lodestar.Text.Vectorization;

string[] docs = ["the cat eats", "the dog eats", "the cat and the dog"];
CsrMatrix counts = new CountVectorizer().FitTransform(docs);

double[] totals = counts.Multiply([1, 1, 1, 1, 1]);

double first = totals[0];   // => 3
double third = totals[2];   // => 5
```

**Remarks** — the cost is `NonZeroCount`, not `RowCount × ColumnCount`, which is the whole reason
to keep the matrix sparse. The third document totals `5` rather than `4` because `the` appears
twice in it and the count is a count.

This is the operation behind scoring a corpus against a linear model: the weights are the vector,
and each row's dot product is that document's score.

**The block overload is not a loop over the vector one.** It makes one pass over the non-zeros
rather than `columnCount` passes: each column index is read once and the inner loop walks
contiguous memory on both sides. That is the operation a randomized SVD's power iteration spends
its time in — `A Ω` for a thin dense `Ω` — and
[`TransposeMultiply`](csrmatrix-transposemultiply.md) is its other half.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CsrMatrix.TransposeMultiply`](csrmatrix-transposemultiply.md), [`CsrMatrix`](csrmatrix.md), [`CsrMatrix.ToDense`](csrmatrix-todense.md).
